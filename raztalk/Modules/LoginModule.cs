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
using Nancy.Responses.Negotiation;
using Nancy.Security;
using System;
using System.Dynamic;
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
            Get["/"] = _ =>
            {
                this.RequiresHttps();

                dynamic model = new ExpandoObject();
                model.Error = null;

                return View["login", model];
            };

            Post["/"] = ctx =>
            {
                this.RequiresHttps();

                try
                {
                    var data = this.Bind<LoginData>();
                    var connection = Connection.Open(data.User, data.Channel, data.PasswordMD5);
                    Context.Request.Session["connection"] = connection.Token;
                    return Response.AsRedirect("/channel/" + connection.Channel.Name);
                }
                catch (Exception e)
                {
                    return Fail(e.Message);
                }
            };
        }

        private Negotiator Fail(string error)
        {
            dynamic model = new ExpandoObject();
            model.Error = error;
            return View["login", model];
        }
    }
}
