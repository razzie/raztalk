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

using System.Collections.Generic;
using System.Timers;

namespace raztalk
{
    class TimeoutReference
    {
        static private List<TimeoutReference> m_references = new List<TimeoutReference>();
        private Timer m_timer;
        private object m_reference;

        private TimeoutReference(object obj, int milliseconds)
        {
            m_reference = obj;
            m_timer = new Timer(milliseconds);
            m_timer.Elapsed += KillReference;
            m_timer.AutoReset = false;
            m_timer.Enabled = true;
        }

        private void KillReference(object sender, ElapsedEventArgs e)
        {
            m_timer.Elapsed -= KillReference;
            m_timer.Dispose();
            m_timer = null;
            m_reference = null;
            m_references.Remove(this);
        }

        static public void Add(object obj, int milliseconds)
        {
            m_references.Add(new TimeoutReference(obj, milliseconds));
        }
    }
}
