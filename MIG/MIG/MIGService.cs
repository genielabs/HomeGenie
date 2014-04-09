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
using System.Net;
using System.Text;
using System.Linq;

using System.IO;
using MIG.Gateways;

namespace MIG
{


    public class GatewayClientRequest
    {
        public string Domain;
        public GatewayCommand Command;     // serializable enum ?!?
        public string Parameters;  // serializable KeyValuePair list
    }

    public interface GatewayCommand
    {
        Dictionary<int, string> ListCommands();
    }

    /// <summary>
    /// used for passing parameters
    /// needed to instantiate a gateway
    /// in the GatewayInterface constructor
    /// </summary>
    public interface GatewayParameters
    {
        //List<Parameters> ListCommands();
    }


    public class MIGClientRequest
    {
        public object Context { get; internal set; }
        public string RequestOrigin { get; internal set; }
        public string RequestMessage { get; internal set; }
        public string SubjectName { get; internal set; }
        public String SubjectValue { get; internal set; }
        public Stream InputStream { get; internal set; }
        public Stream OutputStream { get; internal set; }
    }

    public class ServiceRequestAction
    {
        public MIGService ServiceInstance { get; private set; }
        public MIGClientRequest ClientRequest { get; private set; }

        public ServiceRequestAction(MIGService sender, MIGClientRequest request)
        {
            this.ServiceInstance = sender;
            this.ClientRequest = request;
        }
    }

    class WebFileCache
    {
        public DateTime Timestamp = DateTime.Now;
        public string FilePath = "";
        public string Content = "";
    }

    public class MIGService
    {
        //public event Action<object> ServiceStarted;
        //public event Action<object> ServiceStopped;

        public event Action<InterfacePropertyChangedAction> InterfacePropertyChanged;

        public delegate void WebServiceRequestPreProcessEventHandler(MIGClientRequest request, MIGInterfaceCommand migCmd);
        public event WebServiceRequestPreProcessEventHandler ServiceRequestPreProcess;
        public delegate void WebServiceRequestPostProcessEventHandler(MIGClientRequest request, MIGInterfaceCommand migCmd);
        public event WebServiceRequestPostProcessEventHandler ServiceRequestPostProcess;

        public Dictionary<string, MIGInterface> Interfaces; // TODO: this should be read-only, so implement pvt member _interfaces

        private WebServiceGateway webGateway;
        private TcpSocketGateway tcpGateway;

        private int tcpGatewayPort = 4502;

        private WebServiceGatewayConfiguration webServiceConfig;
        private List<WebFileCache> webFileCache = new List<WebFileCache>();

        #region Lifecycle
        public MIGService()
        {
            webGateway = new WebServiceGateway();
            tcpGateway = new TcpSocketGateway();
            //
            Interfaces = new Dictionary<string, MIGInterface>();
            //
            webServiceConfig = new WebServiceGatewayConfiguration()
            {
                Port = 80,
                SslPort = 443,
                HomePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html"),
                BaseUrl = "hg/html",
                Password = ""
            };
        }

        public bool IsInterfacePresent(string domain)
        {
            bool isPresent = false;
            MIGInterface migInterface = null;
            try
            {
                var type = Type.GetType("MIG.Interfaces." + domain);
                migInterface = (MIGInterface)Activator.CreateInstance(type);
                isPresent = migInterface.IsDevicePresent();
            }
            catch { }
            return isPresent;
        }

        public void AddInterface(string domain)
        {
            MIGInterface migInterface = null;
            var type = Type.GetType("MIG.Interfaces." + domain);
            migInterface = (MIGInterface)Activator.CreateInstance(type);
            Interfaces.Add(domain, migInterface);
            migInterface.InterfacePropertyChangedAction += new Action<InterfacePropertyChangedAction>(MigService_InterfacePropertyChanged);
            //TODO: implement eventually a RemoveInterface method containing code:
            //			mif.InterfacePropertyChangedAction -= MIGService_InterfacePropertyChangedAction;
        }

        // try to bind httpport, launch WebGateway threads, and listen to Interfaces' changes
        public bool StartService()
        {
            bool success = false;
            try
            {
                // TODO: collects gateways to a List<MIGGateway> and expose it by public member Gateways
                //
                webGateway.Configure(webServiceConfig);
                webGateway.ProcessRequest += webGateway_ProcessRequest;
                webGateway.Start();
                //
                tcpGateway.Configure(new TcpSocketGatewayConfiguration()
                {
                    Port = tcpGatewayPort
                });
                tcpGateway.ProcessRequest += tcpGateway_ProcessRequest;
                tcpGateway.Start();
                //
                success = true;
            }
            catch (Exception ex)
            {
                // TODO: add error logging 
            }
            return success;
        }

        //TODO: temporary specific method, get rid of it later
        public void SetWebServicePassword(string passwordHash)
        {
            webGateway.SetPasswordHash(passwordHash);
        }

        public void StopService()
        {
            webGateway.Stop();
            //_tcpgateway.Stop();
            foreach (var migInterface in Interfaces.Values)
            {
                migInterface.Disconnect();
            }
        }
        #endregion

        #region MigInterface events

        private void MigService_InterfacePropertyChanged(InterfacePropertyChangedAction propertyChangedAction)
        {
            // TODO: route event to MIG.ProtocolAdapters
            if (InterfacePropertyChanged != null)
            {
                InterfacePropertyChanged(propertyChangedAction);
            }
        }

        #endregion

        #region TcpGateway

        public void ConfigureTcpGateway(int port)
        {
            tcpGatewayPort = port;
        }

        private void tcpGateway_ProcessRequest(object gwRequest)
        {
            var request = (TcpSocketGatewayRequest)gwRequest;
            int clientId = request.ClientId;
            byte[] data = request.Request;

            var encoding = new System.Text.UTF8Encoding();
            string commandLine = encoding.GetString(data).Trim(new char[] { '\0', ' ' });
            // TODO: 
            // parse command line as <domain>/<target>/<command>/<parameter>[/<parameter>]
            // or as XML serialized instance of GatewayClientRequest
            // then deliver de-serialized command by firing  InterfaceRequestReceived
            // and route to the interface with requested domain
        }

        #endregion

        #region WebGateway

        public void ConfigureWebGateway(int port, int sslPort, string homePath, string baseUrl, string adminPasswordHash)
        {
            webServiceConfig.Port = port;
            webServiceConfig.SslPort = sslPort;
            webServiceConfig.HomePath = homePath;
            webServiceConfig.BaseUrl = baseUrl.TrimStart('/');
            webServiceConfig.Password = adminPasswordHash;
        }

        public void ResetWebFileCache()
        {
            webFileCache.Clear();
        }

        private void webGateway_ProcessRequest(object gwRequest)
        {
            var request = (WebServiceGatewayRequest)gwRequest;
            var context = request.Context;
            string requestedUrl = request.UrlRequest;

            var migRequest = new MIGClientRequest()
            {
                Context = context,
                RequestOrigin = context.Request.RemoteEndPoint.Address.ToString(),
                RequestMessage = requestedUrl,
                SubjectName = "HTTP",
                SubjectValue = context.Request.HttpMethod,
                InputStream = context.Request.InputStream,
                OutputStream = context.Response.OutputStream
            };

            // we are expecting url in the forms http://<hgserver>/<hgservicekey>/<servicedomain>/<servicegroup>/<command>/<opt1>/.../<optn>
            // arguments up to <command> are mandatory. CHANGED, CHECK COMMAND IMPLEMENTATION
            string migCommand = requestedUrl.Substring(requestedUrl.IndexOf('/', 1) + 1);
            //string section = requestedurl.Substring (0, requestedurl.IndexOf ('/', 1) - 1); TODO: "api" section keyword, ignored for now
            //TODO: implement "api" keyword in MIGInterfaceCommand?
            var command = new MIGInterfaceCommand(migCommand);

            //PREPROCESS request: if domain != html, execute command
            if (ServiceRequestPreProcess != null)
            {
                ServiceRequestPreProcess(migRequest, command);
                // request was handled by preprocess listener
                if (!string.IsNullOrEmpty(command.Response))
                {
                    // simple automatic json response type detection
                    if (command.Response.StartsWith("[") && command.Response.EndsWith("]") || (command.Response.StartsWith("{") && command.Response.EndsWith("}")))
                    {
                        context.Response.ContentType = "application/json";
                        context.Response.ContentEncoding = Encoding.UTF8;
                    }
                    WebServiceUtility.WriteStringToContext(context, command.Response);
                    return;
                }
            }

            // TODO: move dupe code to WebServiceUtility

            //if request begins /hg/html, process
            if (requestedUrl.StartsWith(webServiceConfig.BaseUrl))
            {
                string requestedFile = GetWebFilePath(requestedUrl);
                if (!System.IO.File.Exists(requestedFile))
                {
                    context.Response.StatusCode = 404;
                    //context.Response.OutputStream.WriteByte();
                }
                else
                {
                    bool isText = false;
                    if (requestedUrl.EndsWith(".js")) // || requestedurl.EndsWith(".json"))
                    {
                        context.Response.ContentType = "text/javascript";
                        isText = true;
                    }
                    else if (requestedUrl.EndsWith(".css"))
                    {
                        context.Response.ContentType = "text/css";
                        isText = true;
                    }
                    else if (requestedUrl.EndsWith(".zip"))
                    {
                        context.Response.ContentType = "application/zip";
                    }
                    else if (requestedUrl.EndsWith(".png"))
                    {
                        context.Response.ContentType = "image/png";
                    }
                    else if (requestedUrl.EndsWith(".jpg"))
                    {
                        context.Response.ContentType = "image/jpeg";
                    }
                    else if (requestedUrl.EndsWith(".gif"))
                    {
                        context.Response.ContentType = "image/gif";
                    }
                    else if (requestedUrl.EndsWith(".mp3"))
                    {
                        context.Response.ContentType = "audio/mp3";
                    }
                    else
                    {
                        context.Response.ContentType = "text/html";
                        isText = true;
                    }

                    var file = new System.IO.FileInfo(requestedFile);
                    context.Response.AddHeader("Last-Modified", file.LastWriteTimeUtc.ToString("r"));
                    context.Response.Headers.Set(HttpResponseHeader.LastModified, file.LastWriteTimeUtc.ToString("r"));
                    // PRE PROCESS text output
                    //TODO: add callback for handling caching (eg. function that returns true or false with requestdfile as input and that will return false for widget path)
                    if (isText && !requestedFile.Contains("/widgets/"))
                    {
                        try
                        {
                            string body = GetWebFileCache(requestedFile);
                            //
                            bool tagFound;
                            do
                            {
                                tagFound = false;
                                int ts = body.IndexOf("{include ");
                                if (ts > 0)
                                {
                                    int te = body.IndexOf("}", ts);
                                    if (te > ts)
                                    {
                                        string rs = body.Substring(ts + (te - ts) + 1);
                                        string cs = body.Substring(ts, te - ts + 1);
                                        string ls = body.Substring(0, ts);
                                        //
                                        try
                                        {
                                            if (cs.StartsWith("{include "))
                                            {
                                                string fileName = cs.Substring(9).TrimEnd('}').Trim();
                                                fileName = GetWebFilePath(fileName);
                                                body = ls + System.IO.File.ReadAllText(fileName) + rs;
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            body = ls + "<h5 style=\"color:red\">Error processing '" + cs.Replace("{", "[").Replace("}", "]") + "'</h5>" + rs;
                                        }
                                        tagFound = true;
                                    }
                                }
                            } while (tagFound); // pre processor tag found
                            //
                            PutWebFileCache(requestedFile, body);
                            //
                            WebServiceUtility.WriteStringToContext(context, body);
                        }
                        catch
                        {
                        }
                    }
                    else
                    {
                        WebServiceUtility.WriteBytesToContext(context, System.IO.File.ReadAllBytes(requestedFile));
                    }
                }
            }

            object responseObject = null;
            bool wroteBytes = false;
            //domain == HomeAutomation._Interface_ call InterfaceControl
            var result = (from miginterface in Interfaces.Values
                          let ns = miginterface.GetType().Namespace
                          let domain = ns.Substring(ns.LastIndexOf(".") + 1) + "." + miginterface.GetType().Name
                          where (command.Domain != null && command.Domain.StartsWith(domain))
                          select miginterface).FirstOrDefault();
            if (result != null)
            {
                try
                {
                    responseObject = result.InterfaceControl(command);
                }
                catch (Exception ex)
                {
                    // TODO: report internal mig interface  error
                    context.Response.StatusCode = 500;
                    responseObject = ex.Message + "\n" + ex.StackTrace;
                }
            }
            //
            if (responseObject == null || responseObject.Equals(String.Empty))
            {
                responseObject = WebServiceDynamicApiCall(command);
            }
            //
            if (responseObject != null && responseObject.GetType().Equals(typeof(string)))
            {
                command.Response = (string)responseObject;
                //
                // simple automatic json response type detection
                if (command.Response.StartsWith("[") && command.Response.EndsWith("]") || (command.Response.StartsWith("{") && command.Response.EndsWith("}")))
                {
                    context.Response.ContentType = "application/json";
                    context.Response.ContentEncoding = Encoding.UTF8;
                }
            }
            else
            {
                WebServiceUtility.WriteBytesToContext(context, (Byte[])responseObject);
                wroteBytes = true;
            }
            //
            //POSTPROCESS 
            if (ServiceRequestPostProcess != null)
            {
                ServiceRequestPostProcess(migRequest, command);
                if (!string.IsNullOrEmpty(command.Response) && !wroteBytes)
                {
                    // request was handled by postprocess listener
                    WebServiceUtility.WriteStringToContext(context, command.Response);
                    return;
                }
            }

        }

        public object WebServiceDynamicApiCall(MIGInterfaceCommand command)
        {
            object response = "";
            // Dynamic Interface API 
            var handler = MIG.Interfaces.DynamicInterfaceAPI.Find(command.Domain + "/" + command.NodeId + "/" + command.Command);
            if (handler != null)
            {
                response = handler(command.GetOption(0) + (command.GetOption(1) != "" ? "/" + command.GetOption(1) : ""));
            }
            else
            {
                handler = MIG.Interfaces.DynamicInterfaceAPI.FindMatching(command.OriginalRequest.Trim('/'));
                if (handler != null)
                {
                    response = handler(command.OriginalRequest.Trim('/'));
                }
            }
            return response;
        }

        #endregion


        #region Web Service File Management

        private string GetWebFileCache(string file)
        {
            string content = "";
            var cachedItem = webFileCache.Find(wfc => wfc.FilePath == file);
            if (cachedItem != null && (DateTime.Now - cachedItem.Timestamp).TotalSeconds < 600)
            {
                content = cachedItem.Content;
            }
            else
            {
                content = System.IO.File.ReadAllText(file);  //Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                if (cachedItem != null)
                {
                    webFileCache.Remove(cachedItem);
                }
            }
            return content;
        }

        private void PutWebFileCache(string file, string content)
        {
            var cachedItem = webFileCache.Find(wfc => wfc.FilePath == file);
            if (cachedItem == null)
            {
                cachedItem = new WebFileCache();
                webFileCache.Add(cachedItem);
            }
            cachedItem.FilePath = file;
            cachedItem.Content = content;
        }

        private string GetWebFilePath(string file)
        {
            string path = webServiceConfig.HomePath;
            file = file.Replace(webServiceConfig.BaseUrl, "");
            //
            var os = Environment.OSVersion;
            var platformId = os.Platform;
            //
            switch (platformId)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    path = System.IO.Path.Combine(path, file.Replace("/", "\\").TrimStart('\\'));
                    break;
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                default:
                    path = System.IO.Path.Combine(path, file.TrimStart('/'));
                    break;
            }
            return path;
        }

        #endregion

    }
}