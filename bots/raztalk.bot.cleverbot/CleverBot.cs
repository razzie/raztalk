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

        protected override void ConsumeMessage(string user, string message, DateTime timestamp)
        {
            CleverbotSession session;
            if (!Sessions.TryGetValue(user, out session))
            {
                session = CleverbotSession.NewSession(ApiUser, ApiKey);
                Sessions.Add(user, session);
                this[user] = session.BotNick;
            }

            string result = session.Send(message);
            FireNewMessage("@" + user + ": " + result);
        }

        public override void Dispose()
        {
            Sessions.Clear();
            base.Dispose();
        }
    }
}
