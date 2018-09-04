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
using Nancy.ModelBinding;
using Nancy.Security;
using System;
using System.Security.Cryptography;
using System.Text;

namespace raztalk.Modules
{
    public class LoginModule : NancyModule
    {
        class LoginData
        {
            public string User { get; set; }
            public string Channel { get; set; }
            public string Password { get; set; }

            public string PasswordMD5
            {
                get
                {
                    if (string.IsNullOrEmpty(Password))
                        return string.Empty;

                    using (MD5 md5 = MD5.Create())
                    {
                        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(Password));
                        return Convert.ToBase64String(hash);
                    }
                }
            }
        }

        public LoginModule()
        {
            After += ctx =>
            {
                this.RequiresHttps();
            };

            Get["/"] = ctx =>
            {
                return View["login"];
            };

            Get["/login/{channel_name}"] = ctx =>
            {
                ViewBag.Channel = (string)ctx.channel_name;
                return View["login"];
            };

            Post["/"] = ctx =>
            {
                try
                {
                    var data = this.Bind<LoginData>();
                    var connection = Connection.Login(data.User, data.Channel, data.PasswordMD5);
                    return Response.AsRedirect("/view-channel/" + connection.Token);
                }
                catch (Exception e)
                {
                    ViewBag.Error = e.Message;
                }

                return View["login"];
            };
        }
    }
}
