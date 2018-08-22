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

using Nancy;
using Nancy.Security;
using System.Dynamic;

namespace raztalk.Modules
{
    public class ChannelModule : NancyModule
    {
        public ChannelModule()
        {
            After += ctx =>
            {
                this.RequiresHttps();
            };

            Get["/view-channel/{token}"] = ctx =>
            {
                var connection = Connection.Get((string)ctx.token);

                if (connection == null)
                    return Response.AsRedirect("/");

                ViewBag.Token = connection.Token;
                ViewBag.User = connection.User;
                ViewBag.Users = connection.Channel.Users.AsString();
                ViewBag.Channel = connection.Channel;

                return View["channel"];
            };

            Get["/channel/{channel_name}"] = ctx =>
            {
                return Response.AsRedirect("/login/" + (string)ctx.channel_name);
            };

            Get["/channel-stats"] = ctx =>
            {
                return string.Format("Channels: {0} - Users: {1}", Channel.ChannelCount, Channel.UserCount);
            };
        }
    }
}
