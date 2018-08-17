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
using System.Text.RegularExpressions;

namespace raztalk
{
    public class Channel
    {
        static private Dictionary<string, Channel> m_channels = new Dictionary<string, Channel>();

        static public int ChannelCount { get { return m_channels.Count; } }
        static public int UserCount
        {
            get
            {
                int users = 0;
                foreach (var channel in m_channels.Values)
                    users += channel.m_users.Count;
                return users;
            }
        }

        private List<User> m_users = new List<User>();
        private List<Message> m_messages = new List<Message>();

        private Channel(string channelname, string channelpw, User creator)
        {
            Name = channelname;
            Password = channelpw;
            MaxHistory = 20;
            m_users.Add(creator);

            m_channels.Add(Name.ToLower(), this);
        }

        public string Name { get; private set; }
        public string Password { get; private set; }
        public IEnumerable<User> Users { get { return m_users; } }
        public IEnumerable<Message> Messages { get { return m_messages; } }
        public uint MaxHistory { get; private set; }

        public void AddMessage(Message message)
        {
            m_messages.Add(message);

            if (m_messages.Count > MaxHistory)
                m_messages.RemoveAt(0);
        }

        private bool Login(User user, string password)
        {
            foreach (var u in m_users)
            {
                if (u.Name.ToLower().Equals(user.Name.ToLower()))
                    throw new Exception("User already in channel");
            }

            if (Password.Equals(password))
            {
                m_users.Add(user);
                return true;
            }

            return false;
        }

        public void Logout(User user)
        {
            m_users.Remove(user);

            if (m_users.Count == 0)
                m_channels.Remove(Name);
        }

        static public Channel Login(User user, string channelname, string channelpw)
        {
            if (string.IsNullOrEmpty(channelname) || !Regex.IsMatch(channelname, "^[a-zA-Z0-9_.-]*$"))
                throw new Exception("Invalid username!");

            if (channelname.Length > 64)
                throw new Exception("Channel name too long");

            if (channelpw == null)
                channelpw = string.Empty;

            Channel channel;
            if (m_channels.TryGetValue(channelname.ToLower(), out channel))
            {
                if (channel.Login(user, channelpw))
                    return channel;
                else
                    throw new Exception("Authentication to channel failed!");
            }
            else
            {
                return new Channel(channelname, channelpw, user);
            }
        }
    }
}
