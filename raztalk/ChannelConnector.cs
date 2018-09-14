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

using raztalk.bot;

namespace raztalk
{
    public class ChannelConnector : bot.ChannelConnector
    {
        private Channel m_channel;

        public override event MessageEvent Message;

        public ChannelConnector(Channel channel)
        {
            m_channel = channel;
            m_channel.Message += OnChannelMessage;
        }

        private void OnChannelMessage(object sender, Message e)
        {
            Message?.Invoke(e.User.Name, e.Text, e.Timestamp);
        }

        ~ChannelConnector()
        {
            Dispose();
        }

        public override void Send(Bot bot, string text)
        {
            m_channel.Send(bot.UserData as User, text);
        }

        public override void Dispose()
        {
            Message = null;
            m_channel = null;
        }
    }
}
