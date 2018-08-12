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
using System.Linq;

namespace raztalk.Modules
{
    public class ChannelModule : NancyModule
    {
        public ChannelModule()
        {
            Get["/channel/{channel_name}"] = ctx =>
            {
                this.RequiresHttps();

                string token = (string)Context.Request.Session["connection"];
                Connection connection = Connection.Get(token);

                if (connection == null)
                    return Response.AsRedirect("/");

                dynamic model = new ExpandoObject();
                model.User = connection.User;
                model.Users = string.Join(", ", connection.Channel.Users.Select(x => x.Name).ToArray());
                model.Channel = connection.Channel;
                model.Token = connection.Token;

                return View["channel", model];
            };
        }
    }
}
