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
                    RunBot(value);
                    break;

                case "nsfw":
                    bool.TryParse(value, out m_nsfw);
                    break;

                default:
                    break;
            }
        }

        private void RunBot(string subreddit)
        {
            if (m_cancel != null)
            {
                m_cancel.Cancel();
                m_cancel = null;
            }

            if (string.IsNullOrEmpty(subreddit)) return;

            m_cancel = new CancellationTokenSource();
            Task.Factory.StartNew(() =>
            {
                var start = DateTime.Now;
                var cancel = m_cancel;
                try
                {
                    var posts = m_reddit.GetSubreddit(subreddit).New.GetListingStream();
                    foreach (var post in posts)
                    {
                        cancel.Token.ThrowIfCancellationRequested();

                        if ((DateTime.Now - start) > TimeSpan.FromSeconds(10) && (!post.NSFW || m_nsfw))
                            FireNewMessage(post.Title + "\n" + post.Url.AbsoluteUri);
                    }
                }
                catch (OperationCanceledException)
                {
                    FireNewMessage("Thread canceled");
                    throw;
                }
                catch (Exception e)
                {
                    for (; e.InnerException != null; e = e.InnerException) ;
                    FireNewMessage(e.Message);
                }
                finally
                {
                    FireNewMessage("Thread ended");
                    cancel.Dispose();
                }
            });
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
