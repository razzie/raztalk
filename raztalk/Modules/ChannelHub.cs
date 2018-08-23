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
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Tasks;

namespace raztalk.Modules
{
    public class ChannelHub : Hub
    {
        static private ConcurrentDictionary<string, Connection> m_connections = new ConcurrentDictionary<string, Connection>();

        private Connection Connection
        {
            get
            {
                Connection connection = null;
                if (!m_connections.TryGetValue(Context.ConnectionId, out connection))
                    Clients.Caller.RequestLogin();

                return connection;
            }
        }

        public bool Login(string username, string channelname, string channelpw, long last_timestamp)
        {
            Connection connection;
            if (m_connections.TryRemove(Context.ConnectionId, out connection))
            {
                connection.Close();
            }

            connection = Connection.Open(username, channelname, channelpw);
            var joined = Join(connection.Token);

            if (joined)
            {
                var ts = new DateTime(last_timestamp * TimeSpan.TicksPerMillisecond);
                foreach (var msg in connection.Channel.Messages)
                {
                    if (msg.Timestamp > ts)
                        Clients.Caller.Send(msg.User.Name, msg.Text, msg.TimestampMs);
                }
            }
            
            return false;
        }

        public bool Join(string token)
        {
            Connection connection = Connection.Join(Context.ConnectionId, token);
            if (connection != null && m_connections.TryAdd(Context.ConnectionId, connection))
            {
                foreach (var msg in connection.Channel.Messages)
                    Clients.Caller.Send(msg.User.Name, msg.Text, msg.TimestampMs);

                return true;
            }

            return false;
        }

        public void Send(string text)
        {
             Connection?.SendMessage(text);
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            Connection.Close(Context.ConnectionId);

            Connection connection;
            if (m_connections.TryRemove(Context.ConnectionId, out connection))
            {
                connection.Close();
            }

            return base.OnDisconnected(stopCalled);
        }

        public override Task OnReconnected()
        {
            Clients.Caller.RequestLogin();

            return base.OnReconnected();
        }
    }
}
