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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Timers;

namespace raztalk
{
    public class Channel
    {
        static private ConcurrentDictionary<string, Channel> m_channels = new ConcurrentDictionary<string, Channel>();

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
        private ConcurrentQueue<Message> m_messages = new ConcurrentQueue<Message>();
        private Timer m_timer;

        private Channel(string channelname, string channelpw, User creator)
        {
            Name = channelname;
            Password = channelpw;
            MaxHistory = 20;
            m_users.Add(creator);

            if (!m_channels.TryAdd(Name.ToLower(), this))
                throw new Exception("Internal channel error");
        }

        public string Name { get; private set; }
        public string Password { get; private set; }
        public IEnumerable<User> Users { get { return m_users; } }
        public IEnumerable<Message> Messages { get { return m_messages; } }
        public uint MaxHistory { get; private set; }
        public TimeSpan KeepAliveTimeout { get; private set; } = TimeSpan.FromMinutes(5);

        public void AddMessage(Message message)
        {
            m_messages.Enqueue(message);

            if (m_messages.Count > MaxHistory)
            {
                Message tmp_message;
                m_messages.TryDequeue(out tmp_message);
            }
        }

        private bool Login(User user, string password)
        {
            lock (m_users)
            {
                foreach (var u in m_users)
                {
                    if (u.Name.ToLower().Equals(user.Name.ToLower()))
                        throw new Exception("User already in channel");
                }

                if (Password.Equals(password))
                {
                    KillTimeout();
                    m_users.Add(user);
                    return true;
                }
            }

            return false;
        }

        public void Logout(User user)
        {
            lock (m_users)
            {
                m_users.Remove(user);

                if (m_users.Count == 0)
                    StartTimeout();
            }
        }

        private void StartTimeout()
        {
            if (m_timer != null)
                KillTimeout();

            m_timer = new Timer(KeepAliveTimeout.TotalMilliseconds);
            m_timer.Elapsed += TimeoutExpired;
            m_timer.AutoReset = false;
            m_timer.Enabled = true;
        }

        private void KillTimeout()
        {
            if (m_timer != null)
            {
                m_timer.Elapsed -= TimeoutExpired;
                m_timer.Dispose();
                m_timer = null;
            }
        }

        private void TimeoutExpired(object sender, ElapsedEventArgs e)
        {
            KillTimeout();

            lock (m_users)
            {
                if (m_users.Count == 0)
                {
                    Channel tmp_channel;
                    m_channels.TryRemove(Name, out tmp_channel);
                }
            }
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
