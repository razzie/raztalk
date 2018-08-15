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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace raztalk.Modules
{
    public class ChannelHub : Hub
    {
        static private Dictionary<string, Connection> m_connections = new Dictionary<string, Connection>();

        private Connection Connection
        {
            get
            {
                Connection connection;
                if (!m_connections.TryGetValue(Context.ConnectionId, out connection))
                    Clients.Caller.RequestLogin();

                return connection;
            }
        }

        public bool Login(string username, string channelname, string channelpw)
        {
            try
            {
                var connection = Connection.Open(username, channelname, channelpw);
                return Join(connection.Token);
            }
            catch (Exception)
            {
            }
            
            return false;
        }

        public bool Join(string token)
        {
            Connection connection = Connection.Join(token, Context.ConnectionId);
            if (connection != null)
            {
                m_connections.Add(Context.ConnectionId, connection);
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
            Connection?.Close();
            m_connections.Remove(Context.ConnectionId);

            return base.OnDisconnected(stopCalled);
        }

        public override Task OnReconnected()
        {
            Clients.Caller.RequestLogin();

            return base.OnReconnected();
        }
    }
}
