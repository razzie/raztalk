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
using System.Globalization;

namespace raztalk
{
    public class Message
    {
        public Message(User user, string text)
        {
            User = user;
            Text = text;
            Timestamp = DateTime.Now;
        }

        public Message(User user, string text, DateTime timestamp)
        {
            User = user;
            Text = text;
            Timestamp = timestamp;
        }

        public User User { get; private set; }
        public string Text { get; private set; }
        public DateTime Timestamp { get; private set; }
        public string TimestampStr { get { return Timestamp.ToString(TimestampFormat, CultureInfo.InvariantCulture); } }
        public long TimestampMs { get { return (long)(Timestamp - Epoch).TotalMilliseconds; } }
        public bool SystemMessage { get { return User == User.System; } }

        static public DateTime Epoch { get; } = new DateTime(1970, 1, 1);
        static public string TimestampFormat { get; } = "yyyy/MM/dd hh:mm:ss";
    }
}
