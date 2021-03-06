﻿/*
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

using Microsoft.AspNet.SignalR;
using raztalk.bot;
using raztalk.Modules;
using raztools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Timers;

namespace raztalk
{
    public class Channel : MarshalByRefObject, IDisposable
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

        static private IHubContext Hub
        {
            get { return GlobalHost.ConnectionManager.GetHubContext<ChannelHub>(); }
        }

        private dynamic HubGroup
        {
            get { return Hub.Clients.Group(Name); }
        }

        public event EventHandler<Message> Message;

        private List<User> m_users = new List<User>();
        private ConcurrentQueue<Message> m_messages = new ConcurrentQueue<Message>();
        private CommandParser m_cmdparser = new CommandParser();
        private Timeout m_timeout = new Timeout();
        private BotManager m_botmgr;

        private Channel(string channelname, string channelpw, User creator)
        {
            Name = channelname;
            Password = channelpw;
            m_users.Add(creator);

            if (!m_channels.TryAdd(Name.ToLower(), this))
                throw new Exception("Internal channel error");

            m_timeout.Expired += TimeoutExpired;

            m_botmgr = new BotManager(this);

            InitCommands();
        }

        public string Name { get; private set; }
        public string Password { get; private set; }
        public IEnumerable<User> Users { get { return m_users; } }
        public IEnumerable<Message> Messages { get { return m_messages; } }
        public uint MaxHistory { get; private set; } = 100;
        public TimeSpan KeepAliveTimeout { get; private set; } = TimeSpan.FromMinutes(5);
        public bool InviteOnly { get; private set; }

        public void Send(string text)
        {
            Send(null, text);
        }

        public void Send(User user, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            Message message = new Message(user, text);

            m_messages.Enqueue(message);
            HubGroup.Send(message.User.Name, message.Text, message.TimestampMs);

            if (m_messages.Count > MaxHistory)
                m_messages.TryDequeue(out Message tmp_message);

            if (text.StartsWith("!"))
                m_cmdparser.Exec(text);
            else
                Message?.Invoke(this, message);
        }

        private void InitCommands()
        {
            m_cmdparser.Exceptions += CommandParserExceptions;

            m_cmdparser.Add("!help", () => Send(string.Join("\n", m_cmdparser.Commands)));

            m_cmdparser.Add("!ping", () => Send("pong!"));

            m_cmdparser.Add("!users", () => Send("Current users: " + Users.AsString()));

            m_cmdparser.Add<uint>("!keepalive {0}m", m => {
                KeepAliveTimeout = TimeSpan.FromMinutes(m);
                Send(string.Format("Keep alive timeout for this channel is {0} minute(s)", m));
            });

            m_cmdparser.Add<uint>("!keepalive {0}h", h => {
                KeepAliveTimeout = TimeSpan.FromHours(h);
                Send(string.Format("Keep alive timeout for this channel is {0} hour(s)", h));
            });

            m_cmdparser.Add<string, uint>("!invite {0} {0}m", (user, keepalive) =>
            {
                var connection = Connection.Invite(user, Name, Password, TimeSpan.FromMinutes(keepalive));
                if (connection != null)
                    Send(user + " invited (<a href=\"/view-channel/" + connection.Token + "\">copy this link</a>)");
            });

            m_cmdparser.Add<string, uint>("!invite {0} {0}h", (user, keepalive) =>
            {
                var connection = Connection.Invite(user, Name, Password, TimeSpan.FromHours(keepalive));
                if (connection != null)
                    Send(user + " invited (<a href=\"/view-channel/" + connection.Token + "\">copy this link</a>)");
            });

            m_cmdparser.Add<bool>("!invite-only {0}", enabled =>
            {
                InviteOnly = enabled;
                Send("Invite only mode " + (enabled ? "enabled" : "disabled"));
            });

            m_cmdparser.Add("!bots", () =>
            {
                Send("Bots available: " + m_botmgr.Available);
                Send("Bots enabled: " + m_botmgr.Current);
            });

            m_cmdparser.Add<string>("!add-bot {0}", bot =>
            {
                var newbot = m_botmgr.Add(bot);
                if (newbot != null)
                {
                    newbot.UserData = User.BotUser(bot);
                    Send("Bot added");
                }
            });

            m_cmdparser.Add<string>("!remove-bot {0}", bot =>
            {
                if (m_botmgr.Remove(bot))
                {
                    Send("Bot removed");
                }
            });

            m_cmdparser.Add<string, string, string>("!bot {0} {1}:{2}", (bot, arg, value) =>
            {
                Bot tmp_bot = m_botmgr.Get(bot);
                if (tmp_bot != null)
                {
                    tmp_bot[arg] = value;
                    Send("Done");
                }
            });

            m_cmdparser.Add<string, string>("!bot {0} {1}?", (bot, arg) =>
            {
                Bot tmp_bot = m_botmgr.Get(bot);
                if (tmp_bot != null)
                {
                    Send(tmp_bot[arg]);
                }
            });
        }

        private void CommandParserExceptions(object sender, Exception e)
        {
            Send(e.Message);
            Send(e.StackTrace);
        }

        private bool Login(User user, string password, bool invited)
        {
            if (InviteOnly && !invited)
                throw new Exception("This channel is invite only!");

            lock (m_users)
            {
                foreach (var u in m_users)
                {
                    if (u.Name.ToLower().Equals(user.Name.ToLower()))
                        throw new Exception("User already in channel");
                }

                if (Password.Equals(password))
                {
                    m_timeout.Stop();
                    m_users.Add(user);
                    HubGroup.UpdateUsers(Users.AsString());
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
                HubGroup.UpdateUsers(Users.AsString());

                if (m_users.Count == 0)
                    m_timeout.Start(KeepAliveTimeout);
            }
        }

        private void TimeoutExpired(object sender, ElapsedEventArgs e)
        {
            lock (m_users)
            {
                if (m_users.Count == 0)
                {
                    Channel tmp_channel;
                    m_channels.TryRemove(Name, out tmp_channel);

                    Dispose();
                }
            }
        }

        public void Dispose()
        {
            m_cmdparser.Dispose();
            m_botmgr.Dispose();
        }

        static private Channel DoLogin(User user, string channelname, string channelpw, bool invited)
        {
            if (string.IsNullOrEmpty(channelname) || !Regex.IsMatch(channelname, "^[a-zA-Z0-9_.-]*$"))
                throw new Exception("Invalid channel name!");

            if (channelname.Length > 64)
                throw new Exception("Channel name too long");

            if (channelpw == null)
                channelpw = string.Empty;

            Channel channel;
            if (m_channels.TryGetValue(channelname.ToLower(), out channel))
            {
                try
                {
                    if (channel.Login(user, channelpw, invited))
                        return channel;
                    else
                        throw new Exception("Authentication to channel failed!");
                }
                catch (Exception e)
                {
                    channel.Send(user.Name + " failed to login with exception: " + e.Message);
                    throw e;
                }
            }
            else
            {
                return new Channel(channelname, channelpw, user);
            }
        }

        static public Channel Login(User user, string channelname, string channelpw)
        {
            return DoLogin(user, channelname, channelpw, false);
        }

        static public Channel Invite(User user, string channelname, string channelpw)
        {
            return DoLogin(user, channelname, channelpw, true);
        }
    }
}
