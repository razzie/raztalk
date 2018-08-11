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

using System;
using System.Collections.Generic;

namespace raztalk
{
    public class Connection
    {
        static private Dictionary<string, WeakReference<Connection>> m_connections = new Dictionary<string, WeakReference<Connection>>();

        private Connection(User user, Channel channel)
        {
            User = user;
            Channel = channel;
            Token = Guid.NewGuid().ToString();
            m_connections.Add(Token, new WeakReference<Connection>(this));

            // hold a reference to this for 10 seconds, should be enough till SignalR connects
            TimeoutReference.Add(this, 10000);
        }

        ~Connection()
        {
            Close();
        }

        public void Close()
        {
            m_connections.Remove(Token);
            Channel?.Logout(User);
            User = null;
            Channel = null;
            Token = string.Empty;
        }

        public User User { get; private set; }
        public Channel Channel { get; private set; }
        public string Token { get; private set; }

        static public Connection Open(string username, string channelname, string channelpw)
        {
            User user = new User(username);
            Channel channel = Channel.Login(user, channelname, channelpw);

            if (channel != null)
                return new Connection(user, channel);
            else
                return null;
        }

        static public Connection Get(string token)
        {
            if (token == null)
                return null;

            WeakReference<Connection> weakconn = null;
            Connection connection = null;

            m_connections.TryGetValue(token, out weakconn);
            weakconn?.TryGetTarget(out connection);
            return connection;
        }
    }
}
