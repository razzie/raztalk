using raztalk.bot;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace raztalk
{
    public class BotManager : IDisposable
    {
        static private DllDomainManager Domain { get; } = new DllDomainManager("bots/");

        private ConcurrentDictionary<string, Bot> m_bots = new ConcurrentDictionary<string, Bot>();

        static public string Available
        {
            get
            {
                return string.Join(", ", Domain.Classes.Select(t => t.Name).ToArray());
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
            foreach (var bot in m_bots.Values)
            {
                bot.ConsumeMessage(message.User.Name, message.Text, message.Timestamp);
            }
        }

        public void Dispose()
        {
            foreach (var bot in m_bots.Values)
            {
                bot.Dispose();
            }
            m_bots.Clear();
        }

        static public void Reload()
        {
            Domain.Reload();
        }
    }
}
