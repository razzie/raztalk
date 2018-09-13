/*
Copyright (C) Gábor "Razzie" Görzsöny
Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE
*/

using raztools;
using System;
using System.Collections.Concurrent;
using System.Security.Principal;

namespace raztalk
{
    public class Identity : IIdentity
    {
        public string Name { get; set; }
        public string AuthenticationType { get; set; }
        public bool IsAuthenticated { get; set; }
    }

    public enum ConnectionState
    {
        AccessGranted,
        Connecting,
        Estabilished,
        Closed
    }

    public class Connection : IPrincipal
    {
        static private ConcurrentDictionary<string, Connection> m_connections = new ConcurrentDictionary<string, Connection>();
        static public TimeSpan KeepAliveTimeout { get; } = TimeSpan.FromSeconds(15);

        private Timeout m_timeout = new Timeout();

        public User User { get; private set; }
        public Channel Channel { get; private set; }
        public string Password { get; private set; }
        public string Token { get; private set; }
        public ConnectionState State { get; private set; }
        private string ConnectionId { get; set; }

        private Connection(User user, Channel channel, string password, TimeSpan keepalive)
        {
            User = user;
            Channel = channel;
            Password = password;
            Token = Guid.NewGuid().ToString();
            State = ConnectionState.AccessGranted;

            if (!m_connections.TryAdd(Token, this))
                throw new Exception("Internal connection error");

            m_timeout.Expired += (o, e) => Close();
            m_timeout.Start(keepalive);
        }

        private Connection(User user, Channel channel, string password) : this(user, channel, password, KeepAliveTimeout)
        {
        }

        ~Connection()
        {
            Close();
        }

        public void SendMessage(string text)
        {
            Channel.Send(User, text);
        }

        public void SendInfo(string text)
        {
            Channel.Send(text);
        }

        public bool Join(string connectionId)
        {
            if (State == ConnectionState.Connecting)
            {
                m_timeout.Stop();
                State = ConnectionState.Estabilished;
                SendInfo(User.Name + " joined");
                ConnectionId = connectionId;
                User.ConnectionId = connectionId;
                return true;
            }

            return false;
        }

        public void Close()
        {
            if (Channel != null)
            {
                Channel.Logout(User);

                if (State != ConnectionState.AccessGranted)
                    SendInfo(User.Name + " left");
            }

            m_connections.TryRemove(Token, out Connection tmp_connection);

            User = null;
            Channel = null;
            Password = null;
            Token = string.Empty;
            State = ConnectionState.Closed;
        }

        public IIdentity Identity
        {
            get { return new Identity { Name = Token, AuthenticationType = string.Empty, IsAuthenticated = true }; }
        }

        public bool IsInRole(string role)
        {
            throw new NotImplementedException();
        }

        static public Connection Login(string username, string channelname, string channelpw)
        {
            var user = new User(username);
            var channel = Channel.Login(user, channelname, channelpw);

            return new Connection(user, channel, channelpw);
        }

        static public Connection Invite(string username, string channelname, string channelpw, TimeSpan keepalive)
        {
            var user = new User(username);
            var channel = Channel.Invite(user, channelname, channelpw);

            return new Connection(user, channel, channelpw, keepalive);
        }

        static public Connection Get(string token)
        {
            if (token == null)
                return null;

            if (m_connections.TryGetValue(token, out Connection connection))
            {
                if (connection.State == ConnectionState.AccessGranted)
                {
                    connection.State = ConnectionState.Connecting;
                    connection.SendInfo(connection.User.Name + " is connecting...");
                    return connection;
                }
                else if (connection.State == ConnectionState.Connecting)
                {
                    return connection;
                }
                else
                {
                    connection.SendInfo("Someone is using already existing access token for user: " + connection.User.Name);
                }
            }

            return null;
        }
    }
}
