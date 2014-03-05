/*
    This file is part of HomeGenie Project source code.

    HomeGenie is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    HomeGenie is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with HomeGenie.  If not, see <http://www.gnu.org/licenses/>.  
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: http://homegenie.it
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

using System.Text;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace MIG.Gateways
{
    class WebServiceGatewayConfiguration
    {
        public string HomePath;
        public string BaseUrl;
        public int Port;
        public bool UseSSL = false;
        public string Password;
        public int SslPort;
    }

    class WebServiceGatewayRequest
    {
        public HttpListenerContext Context;
        public string UrlRequest;

        public WebServiceGatewayRequest(HttpListenerContext context, string request)
        {
            this.Context = context;
            this.UrlRequest = request;
        }
    }

    public class WebServiceGateway : MIGGateway, IDisposable
    {
        public event Action<object> ProcessRequest;
        //
        private string servicePassword;
        private string homePath;
        private string baseUrl;
        private string[] bindingPrefixes;
        //
        private const int httpThreads = 10;
        private const int maxQueuable = 30;
        //
        private HttpListener listener;
        private Thread listenerThread;
        private Thread[] workers;
        private ManualResetEvent stop, ready;
        private Queue<HttpListenerContext> queue;

        public WebServiceGateway()
        {
            queue = new Queue<HttpListenerContext>();
        }

        public void Configure(object gwConfiguration)
        {
            var config = (WebServiceGatewayConfiguration)gwConfiguration;
            homePath = config.HomePath;
            baseUrl = config.BaseUrl;
            bindingPrefixes = new string[2] { 
                String.Format(@"http://+:{0}/", config.Port),
                String.Format(@"https://+:{0}/", config.SslPort)
            };
            //
            SetPasswordHash(config.Password);
        }

        public void SetPasswordHash(string password)
        {
            servicePassword = password;
        }

        public void Start()
        {
            stop = new ManualResetEvent(false);
            ready = new ManualResetEvent(false);
            //
            listener = new HttpListener();
            listenerThread = new Thread(HandleRequests);
            workers = new Thread[httpThreads];
            //
            string[] bindprefixes = bindingPrefixes;
            foreach (string prefix in bindprefixes)
            {
                listener.Prefixes.Add(prefix);
            }
            listener.Start();
            listenerThread.Start();
            for (int i = 0; i < workers.Length; i++)
            {
                workers[i] = new Thread(Worker);
                workers[i].Start();
            }
        }

        public void Stop()
        {
            foreach (Thread worker in workers)
            {
                worker.Abort();
            }
            stop.Set();
            listener.Stop();
            listenerThread.Join();
            queue.Clear();
        }

        public void Dispose()
        {
            Stop();
        }



        private void HandleRequests()
        {
            int shutdown = -1;
            while (listener.IsListening && shutdown != 0)
            {
                var context = listener.BeginGetContext(ContextReady, null);
                shutdown = WaitHandle.WaitAny(new[] { stop, context.AsyncWaitHandle });
            }
        }

        private void ContextReady(IAsyncResult ar)
        {
            HttpListenerContext context = null;
            try
            {
                context = listener.EndGetContext(ar);
                //
                // Basic flooding prevention
                //
                if (queue.Count >= maxQueuable)
                {
                    context.Response.Abort();
                    context = null;
                }
            }
            catch { }
            //
            if (context == null) return;
            ///
            lock (queue)
            {
                //
                // Enqueue new request
                //
                queue.Enqueue(context);
                ready.Set();
            }
        }

        private void Worker()
        {
            WaitHandle[] wait = new[] { ready, stop };
            while (0 == WaitHandle.WaitAny(wait) && listener.IsListening)
            {
                HttpListenerContext context;
                lock (queue)
                {
                    if (queue.Count > 0)
                    {
                        context = queue.Dequeue();
                    }
                    else
                    {
                        ready.Reset();
                        continue;
                    }
                }
                //
                ProcessWebRequest(context);
            }
        }


        private void ProcessWebRequest(object o)
        {
            try
            {
                var context = o as HttpListenerContext;
                //
                var request = context.Request;
                var response = context.Response;
                //
                if (request.IsSecureConnection)
                {
                    var clientCertificate = context.Request.GetClientCertificate();
                    var chain = new X509Chain();
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain.Build(clientCertificate);
                    if (chain.ChainStatus.Length != 0)
                    {
                        // Invalid certificate
                        response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        response.OutputStream.Close();
                        return;
                    }
                }
                //
                // TODO: why AddHeader is issued twice???
                context.Response.AddHeader("Server", "MIG WebService Gateway");
                //context.Response.Headers.Remove(HttpResponseHeader.Server);
                //context.Response.AddHeader("Server", "MIG WebService Gateway");
                //context.Response.Headers.Set(HttpResponseHeader.Server, "MIG WebService Gateway");
                //
                response.KeepAlive = false;
                //
                bool isauthenticated = (request.Headers["Authorization"] != null);
                //
                if (servicePassword == "" || isauthenticated) //request.IsAuthenticated)
                {
                    bool verified = false;
                    //
                    string authUser = "";
                    string authPassword = "";
                    //
                    //NOTE: context.User.Identity and request.IsAuthenticated
                    //aren't working under MONO with this code =/
                    //so we proceed by manually parsing Authorization header
                    //
                    //HttpListenerBasicIdentity identity = null;
                    //
                    if (isauthenticated)
                    {
                        //identity = (HttpListenerBasicIdentity)context.User.Identity;
                        // authuser = identity.Name;
                        // authpass = identity.Password;
                        byte[] encodedDataAsBytes = System.Convert.FromBase64String(request.Headers["Authorization"].Split(' ')[1]);
                        string authToken = System.Text.Encoding.UTF8.GetString(encodedDataAsBytes);
                        authUser = authToken.Split(':')[0];
                        authPassword = authToken.Split(':')[1];
                    }
                    //
                    //TODO: complete authorization (for now with one fixed user 'admin', add multiuser support)
                    //
                    if (servicePassword == "" || (authUser == "admin" && Utility.Encryption.SHA1.GenerateHashString(authPassword) == servicePassword))
                    {
                        verified = true;
                    }
                    //
                    if (verified)
                    {
                        string url = request.RawUrl.TrimStart('/');
                        if (url.IndexOf("?") > 0) url = url.Substring(0, url.IndexOf("?"));
                        //
                        // url aliasing check
                        if (url == "" || url.TrimEnd('/') == baseUrl.TrimEnd('/'))
                        {
                            // default home redirect
                            response.Redirect("/" + baseUrl.TrimEnd('/') + "/index.html");
                            response.Close();
                        }
                        else
                        {

                            if (url.IndexOf('/') > 0)
                            {
                                try
                                {
                                    if (ProcessRequest != null)
                                    {
                                        ProcessRequest(new WebServiceGatewayRequest(context, url));
                                    }
                                }
                                catch (Exception eh)
                                {
                                    // TODO: add error logging 
                                    Console.Error.WriteLine(eh);
                                }
                            }

                        }
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        response.AddHeader("WWW-Authenticate", "Basic");
                        //context.Response.Headers.Set(HttpResponseHeader.WwwAuthenticate, "Basic");
                    }
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.AddHeader("WWW-Authenticate", "Basic");
                    //context.Response.Headers.Set(HttpResponseHeader.WwwAuthenticate, "Basic");
                }
                //
                try
                {
                    response.OutputStream.Close();
                    response.Close();
                }
                catch { }
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(ObjectDisposedException))
                {
                    // TODO: add error logging 
                    Console.WriteLine("WEBGATEWAY ERROR: " + ex.Message + "\n" + ex.StackTrace);
                }
            }
        }


    }

}
