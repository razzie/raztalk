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
using System.Threading.Tasks;

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

        public void ConsumeMessageAsync(string user, string message, DateTime timestamp)
        {
            Task.Factory.StartNew(() => ConsumeMessage(user, message, timestamp));
        }

        protected virtual void ConsumeMessage(string user, string message, DateTime timestamp)
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
