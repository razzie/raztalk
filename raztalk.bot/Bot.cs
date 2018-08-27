using System;
using System.Collections.Generic;

namespace raztalk.bot
{
    public abstract class Bot : MarshalByRefObject, IDisposable
    {
        private Dictionary<string, string> m_args = new Dictionary<string, string>();

        public delegate void NewMessageEvent(Bot bot, string message);
        public delegate void ArgChangedEvent(Bot bot, string arg, string value);

        public event NewMessageEvent NewMessage;
        public event ArgChangedEvent ArgChanged;

        public object UserData { get; set; }

        public string this[string arg]
        {
            get { return m_args[arg]; }
            set
            {
                m_args[arg] = value;
                ArgChanged?.Invoke(this, arg, value);
            }
        }

        public virtual void ConsumeMessage(string user, string message, DateTime timestamp)
        {
        }

        public virtual void Dispose()
        {
            m_args.Clear();
            NewMessage = null;
            ArgChanged = null;
        }
        
        protected void FireNewMessage(string message)
        {
            NewMessage?.Invoke(this, message);
        }
    }
}
