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
using System.Linq;
using System.Text.RegularExpressions;

namespace raztalk
{
    [Serializable]
    public class User
    {
        private User()
        {
            Name = string.Empty;
        }

        public User(string username)
        {
            if (string.IsNullOrEmpty(username) || !Regex.IsMatch(username, "^[a-zA-Z0-9_.-]*$"))
                throw new Exception("Invalid username!");

            if (username.Length > 64)
                throw new Exception("Username too long");

            Name = username;
        }

        static public User BotUser(string botname)
        {
            return new User()
            {
                Name = "[" + botname + "]"
            };
        }

        public string Name { get; private set; }

        static public User System { get; set; } = new User();
    }

    public static class UsersExtension
    {
        static public string AsString(this IEnumerable<User> users)
        {
            return string.Join(", ", users.Select(x => x.Name).ToArray());
        }
    }
}
