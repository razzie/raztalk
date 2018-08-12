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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace raztalk.Modules
{
    public class ChannelHub : Hub
    {
        static private Dictionary<string, Connection> m_connections = new Dictionary<string, Connection>();

        public bool Login(string token)
        {
            Connection connection = Connection.Get(token);
            if (connection != null)
            {
                m_connections.Add(Context.ConnectionId, connection);
                Groups.Add(Context.ConnectionId, connection.Channel.Name);
                Clients.Group(connection.Channel.Name).SendInfo(connection.User.Name + " connected");
                return true;
            }

            return false;
        }

        public void Send(string text)
        {
            Connection connection;
            if (m_connections.TryGetValue(Context.ConnectionId, out connection))
            {
                Message message = new Message(connection.User, text);
                connection.Channel.AddMessage(message);

                Clients.Group(connection.Channel.Name).Send(connection.User.Name, message.Text, message.TimestampStr);
            }
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            Connection connection;
            if (m_connections.TryGetValue(Context.ConnectionId, out connection))
            {
                Clients.Group(connection.Channel.Name).SendInfo(connection.User.Name + " disconnected");
                m_connections.Remove(Context.ConnectionId);

                //connection.Close();
                TimeoutReference.Add(connection, 10000);
            }

            return base.OnDisconnected(stopCalled);
        }
    }
}
