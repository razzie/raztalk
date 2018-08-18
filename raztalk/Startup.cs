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

using Owin;
using Nancy.Owin;
using System.Net;
using Microsoft.AspNet.SignalR;
using System;

namespace raztalk
{
    class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalHost.Configuration.ConnectionTimeout = TimeSpan.FromTicks(Connection.KeepAliveTimeout.Ticks * 9);
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromTicks(Connection.KeepAliveTimeout.Ticks * 3);
            GlobalHost.Configuration.KeepAlive = Connection.KeepAliveTimeout;

            //var listener = (HttpListener)app.Properties["System.Net.HttpListener"];
            //listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;

            app.MapSignalR("/signalr", new HubConfiguration());

            app.UseNancy((options) =>
            {
                options.Bootstrapper = new CustomBootstrapper();
                options.EnableClientCertificates = false;
            });
        }
    }
}
