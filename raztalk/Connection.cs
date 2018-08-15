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
using System.Collections.Generic;
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
        static private Dictionary<string, Connection> m_connections = new Dictionary<string, Connection>();
        private Timer m_timer;

        public User User { get; private set; }
        public Channel Channel { get; private set; }
        public string Password { get; private set; }
        public string Token { get; private set; }

        private Connection(User user, Channel channel, string password)
        {
            User = user;
            Channel = channel;
            Password = password;
            Token = Guid.NewGuid().ToString();

            m_connections.Add(Token, this);
            StartKeepAliveTimer();

            SendInfo(User.Name + " is connecting...");
            UpdateUsers();
        }

        private void StartKeepAliveTimer()
        {
            m_timer = new Timer(5000); // wait for max 5 seconds until signalR is connected
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

        private void KeepAliveExpired(object sender, ElapsedEventArgs e)
        {
            KillKeepAliveTimer();
            Close();
        }

        public void SendMessage(string text)
        {
            Message message = new Message(User, text);
            Channel.AddMessage(message);
            GlobalHost.ConnectionManager.GetHubContext<ChannelHub>().Clients.Group(Channel.Name).Send(message.User.Name, message.Text, message.TimestampStr);
        }

        public void SendInfo(string info)
        {
            Message message = new Message(User.System, info);
            Channel.AddMessage(message);
            GlobalHost.ConnectionManager.GetHubContext<ChannelHub>().Clients.Group(Channel.Name).SendInfo(message.Text, message.TimestampStr);
        }

        private void UpdateUsers()
        {
            string userlist = Channel.Users.AsString();
            GlobalHost.ConnectionManager.GetHubContext<ChannelHub>().Clients.Group(Channel.Name).UpdateUsers(userlist);
        }

        public void Close()
        {
            if (Channel != null)
            {
                Channel.Logout(User);
                SendInfo(User.Name + " left");
                UpdateUsers();
            }

            m_connections.Remove(Token);
            User = null;
            Channel = null;
            Password = null;
            Token = string.Empty;
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

        static public Connection Get(string token, bool join = false)
        {
            if (token == null)
                return null;

            Connection connection;
            if (m_connections.TryGetValue(token, out connection))
            {
                if (join)
                {
                    connection.KillKeepAliveTimer();
                    connection.SendInfo(connection.User.Name + " joined");
                }
                return connection;
            }

            return null;
        }
    }
}
