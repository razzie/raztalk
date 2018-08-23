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
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace raztalk
{
    public class CommandParser : IDisposable
    {
        private Dictionary<string, Delegate> m_commands = new Dictionary<string, Delegate>();

        public event EventHandler<Exception> Exceptions;

        public void Add<T>(string template, Action<T> method)
        {
            Add(template, (Delegate)method);
        }

        public void Add<T1, T2>(string template, Action<T1, T2> method)
        {
            Add(template, (Delegate)method);
        }

        public void Add<T1, T2, T3>(string template, Action<T1, T2, T3> method)
        {
            Add(template, (Delegate)method);
        }

        public void Add<T1, T2, T3, T4>(string template, Action<T1, T2, T3, T4> method)
        {
            Add(template, (Delegate)method);
        }

        public void Add(string template, Delegate method)
        {
            m_commands.Add(template, method);
        }

        public void Exec(string cmdline)
        {
            foreach (var cmd in m_commands)
            {
                try
                {
                    var args = ReverseStringFormat(cmd.Key, cmdline);
                    if (args == null)
                        continue;

                    Invoke(cmd.Value, args.ToArray());
                }
                catch (Exception e)
                {
                    Exceptions?.Invoke(this, e);
                }
            }
        }

        private List<string> ReverseStringFormat(string template, string str)
        {
            template = Regex.Replace(template, @"[\\\^\$\.\|\?\*\+\(\)]", m => "\\" + m.Value);
            string pattern = "^" + Regex.Replace(template, @"\{[0-9]+\}", "(.*?)") + "$";

            Regex regex = new Regex(pattern);
            Match match = regex.Match(str);

            if (!match.Success)
                return null;

            List<string> ret = new List<string>();

            for (int i = 1; i < match.Groups.Count; i++)
                ret.Add(match.Groups[i].Value);

            return ret;
        }

        static private void Invoke(Delegate method, string[] args)
        {
            var parameters = method.Method.GetParameters();
            var converted_args = new List<object>();
            
            for (int i = 0; i < parameters.Length; ++i)
            {
                var argtype = parameters[i].ParameterType;
                var arg = TypeDescriptor.GetConverter(argtype).ConvertFromString(args[i]);
                converted_args.Add(arg);
            }

            method.DynamicInvoke(converted_args.ToArray());
        }

        public void Dispose()
        {
            m_commands.Clear();
            Exceptions = null;
        }
    }
}
