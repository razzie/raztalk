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

using Imgur.API.Authentication;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Enums;
using Imgur.API.Models.Impl;
using System;
using System.Linq;

namespace raztalk.bot
{
    public class ImgurBot : Bot
    {
        static private string ClientID { get; set; }
        static private string ClientSecret { get; set; }
        static private IImgurClient m_imgur;
        static private IGalleryEndpoint m_endpoint;

        static ImgurBot()
        {
            var parser = new IniParser.FileIniDataParser();
            var ini = parser.ReadFile("bots/imgurbot.ini");

            ClientID = ini.Global["CLIENT_ID"];
            ClientSecret = ini.Global["CLIENT_SECRET"];

            m_imgur = new ImgurClient(ClientID, ClientSecret);
            m_endpoint = new GalleryEndpoint(m_imgur);
        }

        protected override void ConsumeMessage(string user, string message, DateTime timestamp)
        {
            if (message.StartsWith("@imgur ", StringComparison.InvariantCultureIgnoreCase))
            {
                m_endpoint.SearchGalleryAsync(message.Substring(7), GallerySortOrder.Top, TimeWindow.Week).ContinueWith(task =>
                {
                    var results = task.Result;
                    var item = results.First() as GalleryImage;
                    FireNewMessage("@" + user + ": " + item.Link);
                });
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
