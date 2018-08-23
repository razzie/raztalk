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

using Microsoft.AspNet.SignalR;
using raztalk.Modules;
using System;
using System.Collections.Concurrent;
using System.Security.Principal;
using System.Timers;

namespace raztalk
{
    public class Identity : IIdentity
    {
        public string Name { get; set; }
        public string AuthenticationType { get; set; }
        public bool IsAuthenticated { get; set; }
    }

    public class Connection : IPrincipal
    {
        static private ConcurrentDictionary<string, Connection> m_connections = new ConcurrentDictionary<string, Connection>();
        static public TimeSpan KeepAliveTimeout { get; } = TimeSpan.FromSeconds(15);

        private Timer m_timer;
        private CommandParser m_cmdparser = new CommandParser();

        public User User { get; private set; }
        public Channel Channel { get; private set; }
        public string Password { get; private set; }
        public string Token { get; private set; }
        private string ConnectionId { get; set; }

        private Connection(User user, Channel channel, string password)
        {
            User = user;
            Channel = channel;
            Password = password;
            Token = Guid.NewGuid().ToString();

            if (!m_connections.TryAdd(Token, this))
                throw new Exception("Internal connection error");

            StartKeepAliveTimer();

            SendInfo(User.Name + " is connecting...");

            m_cmdparser.Exceptions += (o, e) => SendInfo(e.Message);
            m_cmdparser.Add<uint>("!keepalive {0}m", CmdKeepAliveChannelMinutes);
            m_cmdparser.Add<uint>("!keepalive {0}h", CmdKeepAliveChannelHours);
        }

        ~Connection()
        {
            Close();
        }

        private void StartKeepAliveTimer()
        {
            m_timer = new Timer(KeepAliveTimeout.TotalMilliseconds); // wait for max N seconds until signalR is connected
            m_timer.Elapsed += KeepAliveExpired;
            m_timer.AutoReset = false;
            m_timer.Enabled = true;
        }

        private void KillKeepAliveTimer()
        {
            if (m_timer != null)
            {
                m_timer.Elapsed -= KeepAliveExpired;
                m_timer.Dispose();
                m_timer = null;
            }
        }

        private void CmdKeepAliveChannelMinutes(uint m)
        {
            Channel.KeepAliveTimeout = TimeSpan.FromMinutes(m);
            SendInfo(string.Format("Keep alive timeout for this channel is {0} minute(s)", m));
        }

        private void CmdKeepAliveChannelHours(uint h)
        {
            Channel.KeepAliveTimeout = TimeSpan.FromHours(h);
            SendInfo(string.Format("Keep alive timeout for this channel is {0} hour(s)", h));
        }

        private void KeepAliveExpired(object sender, ElapsedEventArgs e)
        {
            KillKeepAliveTimer();
            Close();
        }

        static private IHubContext Hub
        {
            get
            {
                return GlobalHost.ConnectionManager.GetHubContext<ChannelHub>();
            }
        }

        public void SendMessage(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            Message message = new Message(User, text);

            Hub.Clients.Group(Channel.Name).Send(message.User.Name, message.Text, message.TimestampMs);
            Channel.AddMessage(message);

            if (text.StartsWith("!"))
                m_cmdparser.Exec(text);
        }

        public void SendInfo(string info)
        {
            if (string.IsNullOrEmpty(info))
                return;

            Message message = new Message(User.System, info);
            Hub.Clients.Group(Channel.Name).Send(string.Empty, message.Text, message.TimestampMs);
            Channel.AddMessage(message);
        }

        private void UpdateUsers()
        {
            string userlist = Channel.Users.AsString();
            Hub.Clients.Group(Channel.Name).UpdateUsers(userlist);
        }

        public void Close()
        {
            if (Channel != null)
            {
                Channel.Logout(User);
                SendInfo(User.Name + " left");
                UpdateUsers();
            }

            Connection tmp_connection;
            m_connections.TryRemove(Token, out tmp_connection);

            User = null;
            Channel = null;
            Password = null;
            Token = string.Empty;

            m_cmdparser?.Dispose();
            m_cmdparser = null;
        }

        public IIdentity Identity
        {
            get { return new Identity { Name = Token, AuthenticationType = string.Empty, IsAuthenticated = true }; }
        }

        public bool IsInRole(string role)
        {
            throw new NotImplementedException();
        }

        static public Connection Open(string username, string channelname, string channelpw)
        {
            var user = new User(username);
            var channel = Channel.Login(user, channelname, channelpw);

            return new Connection(user, channel, channelpw);
        }

        static public Connection Get(string token)
        {
            if (token == null)
                return null;

            Connection connection = null;
            m_connections.TryGetValue(token, out connection);
            return connection;
        }

        static public Connection Join(string connectionId, string token)
        {
            if (token == null)
                return null;

            Connection connection;
            if (m_connections.TryGetValue(token, out connection))
            {
                connection.KillKeepAliveTimer();
                connection.SendInfo(connection.User.Name + " joined");
                Hub.Groups.Add(connectionId, connection.Channel.Name);
                connection.UpdateUsers();
                connection.ConnectionId = connectionId;
                return connection;
            }

            return null;
        }

        static public void Close(string connectionId)
        {
            foreach (var conn in m_connections)
            {
                if (conn.Value.ConnectionId != null && conn.Value.ConnectionId.Equals(connectionId))
                {
                    Connection tmp_connection;
                    m_connections.TryRemove(conn.Key, out tmp_connection);
                    return;
                }
            }
        }
    }
}
