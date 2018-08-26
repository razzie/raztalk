using RedditSharp;
using RedditSharp.Things;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace raztalk.bot
{
    public class RedditBot : Bot
    {
        static private string Username { get; set; }
        static private string Password { get; set; }
        static private string ClientID { get; set; }
        static private string ClientSecret { get; set; }
        static private string RedirectUri { get; set; }

        private Reddit m_reddit;
        private Subreddit m_subreddit;
        private CancellationTokenSource m_cancel;
        private bool m_nsfw = false;

        static RedditBot()
        {
            var parser = new IniParser.FileIniDataParser();
            var ini = parser.ReadFile("bots/redditbot.ini");

            Username = ini.Global["USERNAME"];
            Password = ini.Global["PASSWORD"];
            ClientID = ini.Global["CLIENT_ID"];
            ClientSecret = ini.Global["CLIENT_SECRET"];
            RedirectUri = ini.Global["REDIRECT_URI"];
        }

        public RedditBot()
        {
            ArgChanged += (bot, arg, value) => Configure(arg, value);

            var webagent = new BotWebAgent(Username, Password, ClientID, ClientSecret, RedirectUri);
            m_reddit = new Reddit(webagent, true);
            m_cancel = null;
        }

        private void Configure(string arg, string value)
        {
            switch (arg)
            {
                case "r":
                    m_subreddit = m_reddit.GetSubreddit(value);
                    RunBot();
                    break;

                case "nsfw":
                    bool.TryParse(value, out m_nsfw);
                    break;

                default:
                    break;
            }
        }

        private void RunBot()
        {
            if (m_cancel != null)
            {
                m_cancel.Cancel();
                m_cancel = null;
            }

            m_cancel = new CancellationTokenSource();
            Task.Factory.StartNew(() =>
            {
                var start = DateTime.Now;
                var posts = m_subreddit.New.GetListingStream();
                foreach (var post in posts)
                {
                    if (m_cancel.Token.IsCancellationRequested)
                        break;

                    if ((DateTime.Now - start) > TimeSpan.FromSeconds(5) && (!post.NSFW || m_nsfw))
                        FireNewMessage(post.Title + "\n" + post.Url.AbsoluteUri);
                }

                FireNewMessage("END");

            }, m_cancel.Token);
        }

        public override void Dispose()
        {
            if (m_cancel != null)
            {
                m_cancel.Cancel();
                m_cancel = null;
            }

            base.Dispose();
        }
    }
}
