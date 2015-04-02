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
using MIG.Utility;

using Ude;
using Ude.Core;
using Newtonsoft.Json;
using System.Runtime.Serialization.Formatters.Binary;

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
        public Encoding Encoding;
    }

    public class MIGService
    {

        #region Private fields

        private WebServiceGateway webGateway;
        //private TcpSocketGateway tcpGateway;

        //private int tcpGatewayPort = 4502;
        private Encoding defaultWebFileEncoding = Encoding.GetEncoding("UTF-8");

        private WebServiceGatewayConfiguration webServiceConfig;
        // TODO: move webFileCache to WebServiceGateway.cs
        private List<WebFileCache> webFileCache = new List<WebFileCache>();

        private MIGServiceConfiguration configuration;

        #endregion

        #region Public fields

        //public event Action<object> ServiceStarted;
        //public event Action<object> ServiceStopped;

        public event Action<InterfacePropertyChangedAction> InterfacePropertyChanged;
        public event Action<InterfaceModulesChangedAction> InterfaceModulesChanged;

        public delegate void WebServiceRequestPreProcessEventHandler(MIGClientRequest request, MIGInterfaceCommand migCmd);
        public event WebServiceRequestPreProcessEventHandler ServiceRequestPreProcess;
        public delegate void WebServiceRequestPostProcessEventHandler(MIGClientRequest request, MIGInterfaceCommand migCmd);
        public event WebServiceRequestPostProcessEventHandler ServiceRequestPostProcess;

        public Dictionary<string, MIGInterface> Interfaces; // TODO: this should be read-only, so implement pvt member _interfaces

        #endregion

        #region Lifecycle
        public MIGService()
        {
            Interfaces = new Dictionary<string, MIGInterface>();
            //
            //tcpGateway = new TcpSocketGateway();
            //tcpGateway.ProcessRequest += tcpGateway_ProcessRequest;
            //
            webGateway = new WebServiceGateway();
            webGateway.ProcessRequest += webGateway_ProcessRequest;
            webServiceConfig = new WebServiceGatewayConfiguration()
            {
                Port = 80,
                HomePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html"),
                BaseUrl = "hg/html",
                Password = ""
            };
        }

        //TODO: implement a ShutDown method that releases all interfaces and related resources
        public MIGServiceConfiguration Configuration
        {
            get { return configuration; }
            set {
                configuration = value;

                if (configuration.EnableWebCache == "true")
                {
                    IsWebCacheEnabled = true;
                }
                else
                {
                    IsWebCacheEnabled = false;
                }
                //
                // add MIG interfaces
                //
                foreach (MIGServiceConfiguration.Interface iface in configuration.Interfaces)
                {
                    AddInterface(iface.Domain);
                }
            }
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

        // TODO: allow third party interface loading from external assembly dll
        //          eg. AddInterface(string domain, string assemblyFileName)
        public MIGInterface AddInterface(string domain)
        {
            MIGInterface migInterface = null;
            if (!Interfaces.ContainsKey(domain))
            {
                try
                {
                    var type = Type.GetType("MIG.Interfaces." + domain);
                    migInterface = (MIGInterface)Activator.CreateInstance(type);
                    migInterface.Options = configuration.GetInterface(domain).Options;
                }
                catch
                {
                    // TODO: add error logging
                }
                if (migInterface != null)
                {
                    Interfaces.Add(domain, migInterface);
                    migInterface.InterfaceModulesChangedAction += MigService_InterfaceModulesChanged;
                    migInterface.InterfacePropertyChangedAction += MigService_InterfacePropertyChanged;
                }
            }
            else
            {
                migInterface = Interfaces[domain];
            }
            return migInterface;
        }
        //TODO: implement eventually a RemoveInterface method containing code:
        //          migInterface.ModulesChangedAction -= MigService_ModulesChanged;
        //          mif.InterfacePropertyChangedAction -= MIGService_InterfacePropertyChangedAction;

        public void EnableInterface(string domain)
        {
            if (Interfaces.ContainsKey(domain))
            {
                MIGInterface migInterface = Interfaces[domain];
                migInterface.Options = configuration.GetInterface(domain).Options;
                migInterface.Connect();
            }
        }

        public void DisableInterface(string domain)
        {
            if (Interfaces.ContainsKey(domain))
            {
                MIGInterface migInterface = Interfaces[domain];
                migInterface.Disconnect();
            }
        }

        // try to bind httpport, launch WebGateway threads, and listen to Interfaces' changes
        public bool StartGateways()
        {
            bool success = false;
            try
            {
                // TODO: collects gateways to a List<MIGGateway> and expose it by public member Gateways
                //
                webGateway.Configure(webServiceConfig);
                webGateway.Start();
                //
                //tcpGateway.Configure(new TcpSocketGatewayConfiguration()
                //{
                //    Port = tcpGatewayPort
                //});
                //tcpGateway.Start();
                //
                success = true;
            }
            catch
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

        public void StartInterfaces()
        {
            //
            // initialize MIG interfaces
            //
            foreach (MIGServiceConfiguration.Interface iface in configuration.Interfaces)
            {
                if (iface.IsEnabled)
                {
                    EnableInterface(iface.Domain);
                }
                else
                {
                    DisableInterface(iface.Domain);
                }
            }
        }

        public void StopService()
        {
            foreach (var migInterface in Interfaces.Values)
            {
                migInterface.Disconnect();
            }
            webGateway.Stop();
            //tcpGateway.Stop();
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

        private void MigService_InterfaceModulesChanged(InterfaceModulesChangedAction args)
        {
            if (InterfaceModulesChanged != null)
            {
                InterfaceModulesChanged(args);
            }
        }

        #endregion

        #region TcpGateway

        /*
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

            // TODO: parse command line as <domain>/<target>/<command>/<parameter>[/<parameter>]
            // TODO: or as XML serialized instance of GatewayClientRequest
            // TODO: then deliver de-serialized command by firing  InterfaceRequestReceived
            // TODO: and route to the interface with requested domain
        }
        */

        #endregion

        #region WebGateway

        public void ConfigureWebGateway(int port, string homePath, string baseUrl, string adminPasswordHash)
        {
            webServiceConfig.Port = port;
            webServiceConfig.HomePath = homePath;
            webServiceConfig.BaseUrl = baseUrl.TrimStart('/');
            webServiceConfig.Password = adminPasswordHash;
        }

		public bool IsWebCacheEnabled
		{
			get { return webServiceConfig.CacheEnable; }
			set {
				if (value) {
					webServiceConfig.CacheEnable = true;
				} else {
					webServiceConfig.CacheEnable = false;
					webFileCache.Clear();
				}
			}
		}

        public void ClearWebCache()
        {
            webFileCache.Clear();
        }

        private void webGateway_ProcessRequest(object gwRequest)
        {
            var request = (WebServiceGatewayRequest)gwRequest;
            var context = request.Context;
            string requestedUrl = request.UrlRequest;
            bool wroteBytes = false;

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
            // arguments up to <command> are mandatory.
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
                        // TODO: check the reason why this cause ajax/json error on some browser
                        context.Response.ContentType = "application/json";
                        context.Response.ContentEncoding = defaultWebFileEncoding;
                    }
                    WebServiceUtility.WriteStringToContext(context, command.Response);
                    return;
                }
            }

            if (command.Domain == "MIGService.Interfaces")
            {
                // This is a MIGService namespace Web API
                context.Response.ContentType = "application/json";
                switch (command.Command)
                {
                case "IsEnabled.Set":
                    if (command.GetOption(0) == "1")
                    {
                        configuration.GetInterface(command.NodeId).IsEnabled = true;
                        EnableInterface(command.NodeId);
                    }
                    else
                    {
                        configuration.GetInterface(command.NodeId).IsEnabled = false;
                        DisableInterface(command.NodeId);
                    }
                    WebServiceUtility.WriteStringToContext(context, "[{ \"ResponseValue\" : \"OK\" }]");
                    //
                    if (InterfacePropertyChanged != null)
                    {
                        InterfacePropertyChanged(new InterfacePropertyChangedAction() {
                            Domain = "MIGService.Interfaces",
                            SourceId = command.NodeId,
                            SourceType = "MIG Interface",
                            Path = "Status.IsEnabled",
                            Value = command.GetOption(0)                           
                        });
                    }
                    break;
                case "IsEnabled.Get":
                    WebServiceUtility.WriteStringToContext(
                        context,
                        "[{ \"ResponseValue\" : \"" + (configuration.GetInterface(command.NodeId).IsEnabled ? "1" : "0") + "\" }]"
                    );
                    break;
                case "Options.Set":
                    Interfaces[command.NodeId].SetOption(command.GetOption(0), command.GetOption(1));
                    WebServiceUtility.WriteStringToContext(context, "[{ \"ResponseValue\" : \"OK\" }]");
                    //
                    if (InterfacePropertyChanged != null)
                    {
                        InterfacePropertyChanged(new InterfacePropertyChangedAction() {
                            Domain = "MIGService.Interfaces",
                            SourceId = command.NodeId,
                            SourceType = "MIG Interface",
                            Path = "Options." + command.GetOption(0),
                            Value = command.GetOption(1)
                        });
                    }
                    break;
                case "Options.Get":
                    string optionValue = Interfaces[command.NodeId].GetOption(command.GetOption(0)).Value;
                    WebServiceUtility.WriteStringToContext(
                        context,
                        "[{ \"ResponseValue\" : \"" + Uri.EscapeDataString(optionValue) + "\" }]"
                    );
                    break;
                default:
                    break;
                }
            }
            else if (requestedUrl.StartsWith(webServiceConfig.BaseUrl))
            {
                // If request begins /hg/html, process as standard Web request
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
                    else if (requestedUrl.EndsWith(".appcache"))
                    {
                        context.Response.ContentType = "text/cache-manifest";
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
                    if (isText)
                    {
                        try
                        {
                            WebFileCache cachedItem = GetWebFileCache(requestedFile);
                            context.Response.ContentEncoding = cachedItem.Encoding;
                            context.Response.ContentType += "; charset=" + cachedItem.Encoding.BodyName;
                            string body = cachedItem.Content;
                            //
                            // expand preprocessor tags
                            body = body.Replace("{hostos}", Environment.OSVersion.Platform.ToString());
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
                                                //
                                                Encoding fileEncoding = DetectWebFileEncoding(fileName);
                                                if (fileEncoding == null)
                                                    fileEncoding = defaultWebFileEncoding;
                                                body = ls + System.IO.File.ReadAllText(fileName, fileEncoding) + rs;
                                            }
                                        }
                                        catch
                                        {
                                            body = ls + "<h5 style=\"color:red\">Error processing '" + cs.Replace(
                                                "{",
                                                "["
                                            ).Replace(
                                                "}",
                                                "]"
                                            ) + "'</h5>" + rs;
                                        }
                                        tagFound = true;
                                    }
                                }
                            } while (tagFound); // pre processor tag found
                            //
                            if (webServiceConfig.CacheEnable)
                            {
                                PutWebFileCache(requestedFile, body, context.Response.ContentEncoding);
                            }
                            //
                            WebServiceUtility.WriteStringToContext(context, body);
                        }
                        catch (Exception ex)
                        {
                            // TODO: report internal mig interface  error
                            context.Response.StatusCode = 500;
                            WebServiceUtility.WriteStringToContext(context, ex.Message + "\n" + ex.StackTrace);
                            Console.WriteLine("\nMIGService ERROR: " + ex.Message + "\n" + ex.StackTrace + "\n");
                        }
                    }
                    else
                    {
                        WebServiceUtility.WriteBytesToContext(context, System.IO.File.ReadAllBytes(requestedFile));
                    }
                }
            }
            else
            {
                // Try processing as MigInterface Api or Web Service Dynamic Api
                object responseObject = null;
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
                    var postData =  new StreamReader(context.Request.InputStream).ReadToEnd();
                    if (!String.IsNullOrEmpty(postData))
                    {
                        command.OriginalRequest += "/" + postData;
                    }
                    responseObject = WebServiceDynamicApiCall(command);
                }
                //
                if (responseObject != null && responseObject.GetType().Equals(typeof(byte[])) == false)
                {
                    if (responseObject.GetType() == typeof(String))
                    {
                        command.Response = responseObject.ToString();
                    }
                    else
                    {
                        command.Response = JsonConvert.SerializeObject(responseObject);
                    }
                    // simple automatic json response type detection
                    if (command.Response.StartsWith("[") && command.Response.EndsWith("]") || (command.Response.StartsWith("{") && command.Response.EndsWith("}")))
                    {
                        context.Response.ContentType = "application/json";
                        context.Response.ContentEncoding = defaultWebFileEncoding;
                    }
                }
                else
                {
                    WebServiceUtility.WriteBytesToContext(context, (byte[])responseObject);
                    wroteBytes = true;
                }
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
            var registeredApi = command.Domain + "/" + command.NodeId + "/" + command.Command;
            var handler = MIG.Interfaces.DynamicInterfaceAPI.Find(registeredApi);
            if (handler != null)
            {
                // explicit command API handlers registered in the form <domain>/<address>/<command>
                // receives only the remaining part of the request after the <command>
                var args = command.OriginalRequest.Substring(registeredApi.Length).Trim('/');
                response = handler(args);
            }
            else
            {
                handler = MIG.Interfaces.DynamicInterfaceAPI.FindMatching(command.OriginalRequest.Trim('/'));
                if (handler != null)
                {
                    // other command API handlers
                    // receives the full request string
                    response = handler(command.OriginalRequest.Trim('/'));
                }
            }
            return response;
        }

        #endregion

        #region Web Service File Management
        //
        private WebFileCache GetWebFileCache(string file)
        {
            WebFileCache fileItem = new WebFileCache(), cachedItem = null;
            try { cachedItem = webFileCache.Find(wfc => wfc.FilePath == file); } 
            catch (Exception ex) 
            {
                //TODO: sometimes the Find method fires an "object reference not set" error (who knows why???)
                Console.WriteLine("\nMIGService ERROR: " + ex.Message + "\n" + ex.StackTrace + "\n");
                // clear possibly corrupted cache items
                ClearWebCache();
            }
            //
            if (cachedItem != null && (DateTime.Now - cachedItem.Timestamp).TotalSeconds < 600)
            {
                fileItem = cachedItem;
            }
            else
            {
                Encoding fileEncoding = DetectWebFileEncoding(file);  //TextFileEncodingDetector.DetectTextFileEncoding(file);
                if (fileEncoding == null) fileEncoding = defaultWebFileEncoding;
                fileItem.Content = System.IO.File.ReadAllText(file, fileEncoding);  //Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                fileItem.Encoding = fileEncoding;
                if (cachedItem != null)
                {
                    webFileCache.Remove(cachedItem);
                }
            }
            return fileItem;
        }

        private Encoding DetectWebFileEncoding(string filename)
        {
            Encoding enc = defaultWebFileEncoding;
            using (FileStream fs = File.OpenRead(filename))
            {
                ICharsetDetector cdet = new CharsetDetector();
                cdet.Feed(fs);
                cdet.DataEnd();
                if (cdet.Charset != null)
                {
					//Console.WriteLine("Charset: {0}, confidence: {1}",
					//     cdet.Charset, cdet.Confidence);
                    enc = Encoding.GetEncoding(cdet.Charset);
                }
                else
                {
					//Console.WriteLine("Detection failed.");
                }
            }
            return enc;
        }

        private void PutWebFileCache(string file, string content, Encoding encoding)
        {
            var cachedItem = webFileCache.Find(wfc => wfc.FilePath == file);
            if (cachedItem == null)
            {
                cachedItem = new WebFileCache();
                webFileCache.Add(cachedItem);
            }
            cachedItem.FilePath = file;
            cachedItem.Content = content;
            cachedItem.Encoding = encoding;
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