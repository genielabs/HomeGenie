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
        public string Password;
        public bool CacheEnable;
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

    public class HttpListenerCallbackState
    {
        private readonly HttpListener listener;
        private readonly AutoResetEvent listenForNextRequest;

        public HttpListenerCallbackState(HttpListener listener)
        {
            if (listener == null) throw new ArgumentNullException("listener");
            this.listener = listener;
            listenForNextRequest = new AutoResetEvent(false);
        }

        public HttpListener Listener { get { return listener; } }

        public AutoResetEvent ListenForNextRequest { get { return listenForNextRequest; } }
    }

    public class WebServiceGateway : MIGGateway, IDisposable
    {
        public event Action<object> ProcessRequest;
        //
        private ManualResetEvent stopEvent = new ManualResetEvent(false);
        //
        private string servicePassword;
        private string baseUrl;
        private string[] bindingPrefixes;

        public WebServiceGateway()
        {
        }

        public void Configure(object gwConfiguration)
        {
            WebServiceGatewayConfiguration config = (WebServiceGatewayConfiguration)gwConfiguration;
            baseUrl = config.BaseUrl;
            bindingPrefixes = new string[1] { 
                String.Format(@"http://+:{0}/", config.Port)
            };
            SetPasswordHash(config.Password);
        }

        public void SetPasswordHash(string password)
        {
            servicePassword = password;

        }

        public void Start()
        {
            ListenAsynchronously(bindingPrefixes);
        }

        public void Stop()
        {
            StopListening();
        }

        public void Dispose()
        {
            Stop();
        }

        private void Worker(object state)
        {
            HttpListenerRequest request = null;
            HttpListenerResponse response = null;
            try
            {
                var context = state as HttpListenerContext;
                //
                request = context.Request;
                response = context.Response;
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
                // TODO: why AddHeader is issued two times???
                context.Response.AddHeader("Server", "MIG WebService Gateway");
                //context.Response.Headers.Remove(HttpResponseHeader.Server);
                context.Response.AddHeader("Server", "MIG WebService Gateway");
                //context.Response.Headers.Set(HttpResponseHeader.Server, "MIG WebService Gateway");
                //
                response.KeepAlive = false;
                //
                bool isAuthenticated = (request.Headers[ "Authorization" ] != null);
                //
                if (servicePassword == "" || isAuthenticated) //request.IsAuthenticated)
                {
                    bool verified = false;
                    //
                    string authUser = "";
                    string authPass = "";
                    //
                    //NOTE: context.User.Identity and request.IsAuthenticated
                    //aren't working under MONO with this code =/
                    //so we proceed by manually parsing Authorization header
                    //
                    //HttpListenerBasicIdentity identity = null;
                    //
                    if (isAuthenticated)
                    {
                        //identity = (HttpListenerBasicIdentity)context.User.Identity;
                        // authuser = identity.Name;
                        // authpass = identity.Password;
                        byte[] encodedDataAsBytes = System.Convert.FromBase64String(request.Headers[ "Authorization" ].Split(' ')[ 1 ]);
                        string authtoken = System.Text.Encoding.UTF8.GetString(encodedDataAsBytes);
                        authUser = authtoken.Split(':')[ 0 ];
                        authPass = authtoken.Split(':')[ 1 ];
                    }
                    //
                    //TODO: complete authorization (for now with one fixed user 'admin', add multiuser support)
                    //
                    if (servicePassword == "" || (authUser == "admin" && Utility.Encryption.SHA1.GenerateHashString(authPass) == servicePassword))
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
                            response.Redirect("/" + baseUrl.TrimEnd('/') + "/index.html?" + new TimeSpan(DateTime.UtcNow.Ticks).TotalMilliseconds);
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
            }
            catch (Exception ex)
            {
                // TODO: add error logging 
                Console.WriteLine("WEBGATEWAY ERROR: " + ex.Message + "\n" + ex.StackTrace);
            }
            //
            try
            {
                response.OutputStream.Close();
                response.Close();
            }
            catch
            {
            }
            try
            {
                request.InputStream.Close();
            }
            catch
            {
            }
        }

        private void ListenAsynchronously(IEnumerable<string> prefixes)
        {
            HttpListener listener = new HttpListener();
            foreach (string s in prefixes)
            {
                listener.Prefixes.Add(s);
            }
            listener.Start();
            HttpListenerCallbackState state = new HttpListenerCallbackState(listener);
            ThreadPool.QueueUserWorkItem(Listen, state);
        }

        private void StopListening()
        {
            stopEvent.Set();
        }

        private void Listen(object state)
        {
            HttpListenerCallbackState callbackState = (HttpListenerCallbackState)state;
            while (callbackState.Listener.IsListening)
            {
                callbackState.Listener.BeginGetContext(new AsyncCallback(ListenerCallback), callbackState);
                int n = WaitHandle.WaitAny(new WaitHandle[] { callbackState.ListenForNextRequest, stopEvent });
                if (n == 1)
                {
                    // stopEvent was signalled 
                    callbackState.Listener.Stop();
                    break;
                }
            }
        }

        private void ListenerCallback(IAsyncResult result)
        {
            HttpListenerCallbackState callbackState = (HttpListenerCallbackState)result.AsyncState;
            HttpListenerContext context = null;
            callbackState.ListenForNextRequest.Set();
            try
            {
                context = callbackState.Listener.EndGetContext(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("WebServiceGateway: " + ex.Message + "\n" + ex.StackTrace);
            }
            //finally
            //{
            //    callbackState.ListenForNextRequest.Set();
            //}
            if (context == null) return;
            Worker(context);
        }


    }

}
