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

namespace raztalk.bot
{
    public abstract class Bot : MarshalByRefObject, IDisposable
    {
        protected Dictionary<string, string> Config { get; } = new Dictionary<string, string>();
        protected ChannelConnector Connector { get; private set; }
        public object UserData { get; set; }

        public Bot(ChannelConnector connector)
        {
            Connector = connector;
        }

        public string this[string arg]
        {
            get { return Config[arg]; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    Config.Remove(arg);
                    OnConfigRemoved(arg);
                }
                else
                {
                    Config[arg] = value;
                    OnConfigChanged(arg, value);
                }
            }
        }

        protected virtual void OnConfigChanged(string arg, string value)
        {
        }

        protected virtual void OnConfigRemoved(string arg)
        {
        }

        protected void Send(string message)
        {
            Connector?.Send(this, message);
        }

        public virtual void Dispose()
        {
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
