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

using GiphyDotNet.Manager;
using GiphyDotNet.Model.Parameters;
using System;
using System.Timers;

namespace raztalk.bot
{
    public class GiphyBot : Bot
    {
        static private string ApiKey { get; set; }
        static private Giphy m_giphy;
        private Timer m_timer;

        static GiphyBot()
        {
            var parser = new IniParser.FileIniDataParser();
            var ini = parser.ReadFile("bots/giphybot.ini");
            
            ApiKey = ini.Global["API_KEY"];
            m_giphy = new Giphy(ApiKey);
        }

        public GiphyBot(ChannelConnector connector) : base(connector)
        {
            Connector.Message += OnMessage;

            m_timer = new Timer();
            m_timer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
            m_timer.Elapsed += TimerElapsed;
            m_timer.AutoReset = true;
        }

        protected override void OnConfigChanged(string arg, string value)
        {
            switch (arg)
            {
                case "tranding":
                    m_timer.Enabled = bool.Parse(value);
                    break;

                case "interval":
                    int interval = int.Parse(value);
                    m_timer.Interval = TimeSpan.FromMinutes(interval).TotalMilliseconds;
                    break;
            }
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            var trending = new TrendingParameter()
            {
                Limit = 1
            };
            m_giphy.TrendingGifs(trending).ContinueWith(task => {
                var results = task.Result.Data;
                Send(results[0].Images.Original.Url);
            });
        }

        protected void OnMessage(string user, string message, DateTime timestamp)
        {
            if (message.StartsWith("@giphy ", StringComparison.InvariantCultureIgnoreCase))
            {
                var search = new RandomParameter()
                {
                    Tag = message.Substring(7)
                };
                m_giphy.RandomGif(search).ContinueWith(task => Send("@" + user + ": " + task.Result.Data.ImageUrl));
            }
        }

        public override void Dispose()
        {
            m_timer.Dispose();
            base.Dispose();
        }
    }
}
