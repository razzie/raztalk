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

using Cleverbot.Net;
using System;
using System.Collections.Generic;

namespace raztalk.bot
{
    public class CleverBot : Bot
    {
        static private string ApiUser { get; set; }
        static private string ApiKey { get; set; }
        private Dictionary<string, CleverbotSession> Sessions { get; } = new Dictionary<string, CleverbotSession>();

        static CleverBot()
        {
            var parser = new IniParser.FileIniDataParser();
            var ini = parser.ReadFile("bots/cleverbot.ini");

            ApiUser = ini.Global["API_USER"];
            ApiKey = ini.Global["API_KEY"];
        }

        public CleverBot(ChannelConnector connector) : base(connector)
        {
            Connector.Message += OnMessage;
        }

        protected override void OnConfigChanged(string arg, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            switch (arg)
            {
                case "add":
                    var session = CleverbotSession.NewSession(ApiUser, ApiKey);
                    Sessions.Add(value, session);
                    this[arg] = null;
                    this[value] = session.BotNick;
                    break;

                case "remove":
                    Sessions.Remove(value);
                    this[arg] = null;
                    break;
            }
        }

        private void OnMessage(string user, string message, DateTime timestamp)
        {
            CleverbotSession session;
            if (Sessions.TryGetValue(user, out session))
            {
                session.SendAsync(message).ContinueWith(task => Send("@" + user + ": " + task.Result));
            }
        }

        public override void Dispose()
        {
            Sessions.Clear();
            base.Dispose();
        }
    }
}
