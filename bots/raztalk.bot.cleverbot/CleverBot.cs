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

        public CleverBot()
        {
            ArgChanged += CleverBot_ArgChanged;
        }

        private void CleverBot_ArgChanged(Bot bot, string arg, string value)
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

        protected override void ConsumeMessage(string user, string message, DateTime timestamp)
        {
            CleverbotSession session;
            if (Sessions.TryGetValue(user, out session))
            {
                session.SendAsync(message).ContinueWith(task => FireNewMessage("@" + user + ": " + task.Result));
            }
        }

        public override void Dispose()
        {
            Sessions.Clear();
            base.Dispose();
        }
    }
}
