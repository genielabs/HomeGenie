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
        private string _servicepassword;
        private string _homepath;
        private string _baseurl;
        private string[] _bindingprefixes;
        //
        private const int _httpthreads = 10;
        private const int _maxqueuable = 30;
        //
        private HttpListener _listener;
        private Thread _listenerThread;
        private Thread[] _workers;
        private ManualResetEvent _stop, _ready;
        private Queue<HttpListenerContext> _queue;

        public WebServiceGateway()
        {
            _queue = new Queue<HttpListenerContext>();
        }

        public void Configure(object gwconfiguration)
        {
            WebServiceGatewayConfiguration cnf = (WebServiceGatewayConfiguration)gwconfiguration;
            _homepath = cnf.HomePath;
            _baseurl = cnf.BaseUrl;
            _bindingprefixes = new string[2] { 
                String.Format(@"http://+:{0}/", cnf.Port),
                String.Format(@"https://+:{0}/", cnf.SslPort)
            };
            //
            SetPasswordHash(cnf.Password);
        }

        public void SetPasswordHash(string password)
        {
            _servicepassword = password;
        }

        public void Start()
        {
            _stop = new ManualResetEvent(false);
            _ready = new ManualResetEvent(false);
            //
            _listener = new HttpListener();
            _listenerThread = new Thread(HandleRequests);
            _workers = new Thread[_httpthreads];
            //
            string[] bindprefixes = _bindingprefixes;
            foreach (string prefix in bindprefixes)
            {
                _listener.Prefixes.Add(prefix);
            }
            _listener.Start();
            _listenerThread.Start();
            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i] = new Thread(Worker);
                _workers[i].Start();
            }
        }

        public void Stop()
        {
            foreach (Thread worker in _workers)
            {
                worker.Abort();
            }
            _stop.Set();
            _listener.Stop();
            _listenerThread.Join();
            _queue.Clear();
        }

        public void Dispose()
        {
            Stop();
        }







        private void HandleRequests()
        {
            int shutdown = -1;
            while (_listener.IsListening && shutdown != 0)
            {
                var context = _listener.BeginGetContext(ContextReady, null);
                shutdown = WaitHandle.WaitAny(new[] { _stop, context.AsyncWaitHandle });
            }
        }

        private void ContextReady(IAsyncResult ar)
        {
            HttpListenerContext ctx = null;
            try
            {
                ctx = _listener.EndGetContext(ar);
                //
                // Basic flooding prevention
                //
                if (_queue.Count >= _maxqueuable)
                {
                    ctx.Response.Abort();
                    ctx = null;
                }
            }
            catch { }
            //
            if (ctx == null) return;
            ///
            lock (_queue)
            {
                //
                // Enqueue new request
                //
                _queue.Enqueue(ctx);
                _ready.Set();
            }
        }

        private void Worker()
        {
            WaitHandle[] wait = new[] { _ready, _stop };
            while (0 == WaitHandle.WaitAny(wait) && _listener.IsListening)
            {
                HttpListenerContext context;
                lock (_queue)
                {
                    if (_queue.Count > 0)
                    {
                        context = _queue.Dequeue();
                    }
                    else
                    {
                        _ready.Reset();
                        continue;
                    }
                }
                //
                _processrequest(context);
            }
        }


        private void _processrequest(object o)
        {
            try
            {
                var context = o as HttpListenerContext;
                //
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                //
                if (request.IsSecureConnection)
                {
                    var clientCertificate = context.Request.GetClientCertificate();
                    X509Chain chain = new X509Chain();
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
                if (_servicepassword == "" || isauthenticated) //request.IsAuthenticated)
                {
                    bool verified = false;
                    //
                    string authuser = "";
                    string authpass = "";
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
                        string authtoken = System.Text.Encoding.UTF8.GetString(encodedDataAsBytes);
                        authuser = authtoken.Split(':')[0];
                        authpass = authtoken.Split(':')[1];
                    }
                    //
                    //TODO: complete authorization (for now with one fixed user 'admin', add multiuser support)
                    //
                    if (_servicepassword == "" || (authuser == "admin" && Utility.Encryption.SHA1.GenerateHashString(authpass) == _servicepassword))
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
                        if (url == "" || url.TrimEnd('/') == _baseurl.TrimEnd('/'))
                        {
                            // default home redirect
                            response.Redirect("/" + _baseurl.TrimEnd('/') + "/index.html");
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
