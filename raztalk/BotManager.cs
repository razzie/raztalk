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
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.Remoting;

namespace raztalk
{
    public class BotManager : IDisposable
    {
        static private SandboxDomain Domain { get; } = new SandboxDomain("bots/");

        private ConcurrentDictionary<string, Bot> m_bots = new ConcurrentDictionary<string, Bot>();

        static public string Available
        {
            get
            {
                return string.Join(", ", Domain.Classes.Select(c => c.TypeNme).ToArray());
            }
        }

        public string Current
        {
            get
            {
                return string.Join(", ", m_bots.Keys);
            }
        }

        public Bot Add(string bot)
        {
            var newbot = Domain.Create(bot) as Bot;
            if (newbot != null && m_bots.TryAdd(bot, newbot))
            {
                Domain.Unloaded += (sender, domain) => Remove(bot);
                return newbot;
            }

            return null;
        }

        public Bot Get(string bot)
        {
            Bot tmp_bot = null;
            m_bots.TryGetValue(bot, out tmp_bot);
            return tmp_bot;
        }

        public bool Remove(string bot)
        {
            Bot tmp_bot;
            if (m_bots.TryRemove(bot, out tmp_bot))
            {
                tmp_bot.Dispose();
                return true;
            }

            return false;
        }

        public void ConsumeMessage(Message message)
        {
            foreach (var bot in m_bots.ToArray())
            {
                if (message.User.IsBot || message.SystemMessage) continue;

                try
                {
                    bot.Value.ConsumeMessageAsync(message.User.Name, message.Text, message.Timestamp);
                }
                catch (RemotingException)
                {
                    Bot tmp_bot;
                    m_bots.TryRemove(bot.Key, out tmp_bot);
                }
            }
        }

        public void Dispose()
        {
            foreach (var bot in m_bots.Values)
            {
                try
                {
                    bot.Dispose();
                }
                catch (RemotingException)
                {
                }
            }
            m_bots.Clear();
        }

        static public void Load()
        {
            Domain.Load();
        }

        static public void Unload()
        {
            Domain.Unload();
        }
    }
}
