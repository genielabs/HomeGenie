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

        private WebServiceGateway _webgateway;
        private TcpSocketGateway _tcpgateway;

        private int _tcpgateway_port = 4502;

        private WebServiceGatewayConfiguration _webserviceconfig;
        private List<WebFileCache> _webfilecache = new List<WebFileCache>();

        #region Lifecycle
        public MIGService()
        {
            _webgateway = new WebServiceGateway();
            _tcpgateway = new TcpSocketGateway();
            //
            Interfaces = new Dictionary<string, MIGInterface>();
            //
            _webserviceconfig = new WebServiceGatewayConfiguration()
            {
                Port = 80,
                SslPort = 443,
                HomePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html"),
                BaseUrl = "hg/html",
                Password = ""
            };
        }

        public bool IsInterfacePresent(string intfdomain)
        {
            bool ispresent = false;
            MIGInterface migintf = null;
            try
            {
                Type type = Type.GetType("MIG.Interfaces." + intfdomain);
                migintf = (MIGInterface)Activator.CreateInstance(type);
                ispresent = migintf.IsDevicePresent();
            }
            catch { }
            return ispresent;
        }

        public void AddInterface(string intfdomain)
        {
            MIGInterface migintf = null;
            Type type = Type.GetType("MIG.Interfaces." + intfdomain);
            migintf = (MIGInterface)Activator.CreateInstance(type);
            Interfaces.Add(intfdomain, migintf);
            migintf.InterfacePropertyChangedAction += new Action<InterfacePropertyChangedAction>(MIGService_InterfacePropertyChanged);
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
                _webgateway.Configure(_webserviceconfig);
                _webgateway.ProcessRequest += _webgateway_ProcessRequest;
                _webgateway.Start();
                //
                _tcpgateway.Configure(new TcpSocketGatewayConfiguration()
                {
                    Port = _tcpgateway_port
                });
                _tcpgateway.ProcessRequest += _tcpgateway_ProcessRequest;
                _tcpgateway.Start();
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
        public void SetWebServicePassword(string passwordhash)
        {
            _webgateway.SetPasswordHash(passwordhash);
        }

        public void StopService()
        {
            _webgateway.Stop();
            //_tcpgateway.Stop();
            foreach (MIGInterface mif in Interfaces.Values)
            {
                mif.Disconnect();
            }
        }
        #endregion

        #region MigInterface events

        void MIGService_InterfacePropertyChanged(InterfacePropertyChangedAction propertychangedaction)
        {
            // TODO: route event to MIG.ProtocolAdapters
            if (InterfacePropertyChanged != null)
            {
                InterfacePropertyChanged(propertychangedaction);
            }
        }

        #endregion

        #region TcpGateway

        public void ConfigureTcpGateway(int port)
        {
            _tcpgateway_port = port;
        }

        private void _tcpgateway_ProcessRequest(object request)
        {
            TcpSocketGateyRequest req = (TcpSocketGateyRequest)request;
            int clientid = req.ClientId;
            byte[] data = req.Request;

            System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
            string cmdline = enc.GetString(data).Trim(new char[] { '\0', ' ' });
            // TODO: 
            // parse command line as <domain>/<target>/<command>/<parameter>[/<parameter>]
            // or as XML serialized instance of GatewayClientRequest
            // then deliver de-serialized command by firing  InterfaceRequestReceived
            // and route to the interface with requested domain
        }

        #endregion

        #region WebGateway

        public void ConfigureWebGateway(int port, int sslport, string homepath, string baseurl, string adminpasswordhash)
        {
            _webserviceconfig.Port = port;
            _webserviceconfig.SslPort = sslport;
            _webserviceconfig.HomePath = homepath;
            _webserviceconfig.BaseUrl = baseurl.TrimStart('/');
            _webserviceconfig.Password = adminpasswordhash;
        }

        private void _webgateway_ProcessRequest(object gwrequest)
        {
            WebServiceGatewayRequest req = (WebServiceGatewayRequest)gwrequest;
            HttpListenerContext context = req.Context;
            string requestedurl = req.UrlRequest;

            MIGClientRequest migrequest = new MIGClientRequest()
            {
                Context = context,
                RequestOrigin = context.Request.RemoteEndPoint.Address.ToString(),
                RequestMessage = requestedurl,
                SubjectName = "HTTP",
                SubjectValue = context.Request.HttpMethod,
                InputStream = context.Request.InputStream,
                OutputStream = context.Response.OutputStream
            };

            // we are expecting url in the forms http://<hgserver>/<hgservicekey>/<servicedomain>/<servicegroup>/<command>/<opt1>/.../<optn>
            // arguments up to <command> are mandatory. CHANGED, CHECK COMMAND IMPLEMENTATION
            string no_hg_request = requestedurl.Substring(requestedurl.IndexOf('/', 1) + 1);
            //string section = requestedurl.Substring (0, requestedurl.IndexOf ('/', 1) - 1); TODO: "api" section keyword, ignored for now
            //TODO: implement "api" keyword in MIGInterfaceCommand?
            MIGInterfaceCommand cmd = new MIGInterfaceCommand(no_hg_request);

            //PREPROCESS request: if domain != html, execute command
            if (ServiceRequestPreProcess != null)
            {
                ServiceRequestPreProcess(migrequest, cmd);
                // request was handled by preprocess listener
                if (!string.IsNullOrEmpty(cmd.response))
                {
                    // simple automatic json response type detection
                    if (cmd.response.StartsWith("[") && cmd.response.EndsWith("]") || (cmd.response.StartsWith("{") && cmd.response.EndsWith("}")))
                    {
                        context.Response.ContentType = "application/json";
                        context.Response.ContentEncoding = Encoding.UTF8;
                    }
                    WebServiceUtility.WriteStringToContext(context, cmd.response);
                    return;
                }
            }

            // TODO: move dupe code to WebServiceUtility

            //if request begins /hg/html, process
            if (requestedurl.StartsWith(_webserviceconfig.BaseUrl))
            {
                string requestedfile = GetWebFilePath(requestedurl);
                if (!System.IO.File.Exists(requestedfile))
                {
                    context.Response.StatusCode = 404;
                    //context.Response.OutputStream.WriteByte();
                }
                else
                {
                    bool isText = false;
                    if (requestedurl.EndsWith(".js")) // || requestedurl.EndsWith(".json"))
                    {
                        context.Response.ContentType = "text/javascript";
                        isText = true;
                    }
                    else if (requestedurl.EndsWith(".css"))
                    {
                        context.Response.ContentType = "text/css";
                        isText = true;
                    }
                    else if (requestedurl.EndsWith(".zip"))
                    {
                        context.Response.ContentType = "application/zip";
                    }
                    else if (requestedurl.EndsWith(".png"))
                    {
                        context.Response.ContentType = "image/png";
                    }
                    else if (requestedurl.EndsWith(".jpg"))
                    {
                        context.Response.ContentType = "image/jpeg";
                    }
                    else if (requestedurl.EndsWith(".gif"))
                    {
                        context.Response.ContentType = "image/gif";
                    }
                    else if (requestedurl.EndsWith(".mp3"))
                    {
                        context.Response.ContentType = "audio/mp3";
                    }
                    else
                    {
                        context.Response.ContentType = "text/html";
                        isText = true;
                    }

                    System.IO.FileInfo fi = new System.IO.FileInfo(requestedfile);
                    context.Response.AddHeader("Last-Modified", fi.LastWriteTimeUtc.ToString("r"));
                    context.Response.Headers.Set(HttpResponseHeader.LastModified, fi.LastWriteTimeUtc.ToString("r"));
                    // PRE PROCESS text output
                    //TODO: add callback for handling caching (eg. function that returns true or false with requestdfile as input and that will return false for widget path)
                    if (isText && !requestedfile.Contains("/widgets/"))
                    {
                        try
                        {
                            string body = GetWebFileCache(requestedfile); 
                            //
                            bool pp_tagfound;
                            do
                            {
                                pp_tagfound = false;
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
                                                string file = cs.Substring(9).TrimEnd('}').Trim();
                                                file = GetWebFilePath(file);
                                                body = ls + System.IO.File.ReadAllText(file) + rs;
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            body = ls + "<h5 style=\"color:red\">Error processing '" + cs.Replace("{", "[").Replace("}", "]") + "'</h5>" + rs;
                                        }
                                        pp_tagfound = true;
                                    }
                                }
                            } while (pp_tagfound); // pre processor tag found
                            //
                            PutWebFileCache(requestedfile, body);
                            //
                            WebServiceUtility.WriteStringToContext(context, body);
                        }
                        catch
                        {
                        }
                    }
                    else
                    {
                        WebServiceUtility.WriteBytesToContext(context, System.IO.File.ReadAllBytes(requestedfile));
                    }
                }
            }

            object resobj = null;
            bool _wrotebytes = false;
            //domain == HomeAutomation._Interface_ call InterfaceControl
            var result = (from miginterface in Interfaces.Values
                          let ns = miginterface.GetType().Namespace
                          let domain = ns.Substring(ns.LastIndexOf(".") + 1) + "." + miginterface.GetType().Name
                          where (cmd.domain != null && cmd.domain.StartsWith(domain))
                          select miginterface).FirstOrDefault();
            if (result != null)
            {
                try
                {
                    resobj = result.InterfaceControl(cmd);
                }
                catch (Exception ex)
                {
                    // TODO: report internal mig interface  error
                    context.Response.StatusCode = 500;
                    resobj = ex.Message + "\n" + ex.StackTrace;
                }
            }
            //
            if (resobj == null || resobj.Equals(String.Empty))
            {
                resobj = WebServiceDynamicApiCall(cmd);
            }
            //
            if (resobj != null && resobj.GetType().Equals(typeof(string)))
            {
                cmd.response = (string)resobj;
                //
                // simple automatic json response type detection
                if (cmd.response.StartsWith("[") && cmd.response.EndsWith("]") || (cmd.response.StartsWith("{") && cmd.response.EndsWith("}")))
                {
                    context.Response.ContentType = "application/json";
                    context.Response.ContentEncoding = Encoding.UTF8;
                }
            }
            else
            {
                WebServiceUtility.WriteBytesToContext(context, (Byte[])resobj);
                _wrotebytes = true;
            }
            //
            //POSTPROCESS 
            if (ServiceRequestPostProcess != null)
            {
                ServiceRequestPostProcess(migrequest, cmd);
                if (!string.IsNullOrEmpty(cmd.response) && !_wrotebytes)
                {
                    // request was handled by postprocess listener
                    WebServiceUtility.WriteStringToContext(context, cmd.response);
                    return;
                }
            }

        }

        public object WebServiceDynamicApiCall(MIGInterfaceCommand cmd)
        {
            object response = "";
            // Dynamic Interface API 
            Func<object, object> handler = MIG.Interfaces.DynamicInterfaceAPI.Find(cmd.domain + "/" + cmd.nodeid + "/" + cmd.command);
            if (handler != null)
            {
                response = handler(cmd.GetOption(0) + (cmd.GetOption(1) != "" ? "/" + cmd.GetOption(1) : ""));
            }
            else
            {
                handler = MIG.Interfaces.DynamicInterfaceAPI.FindMatching(cmd.originalRequest.Trim('/'));
                if (handler != null)
                {
                    response = handler(cmd.originalRequest.Trim('/'));
                }
            }
            return response;
        }

        #endregion


        #region Web Service File Management

        private string GetWebFileCache(string file)
        {
            string content = "";
            WebFileCache item = _webfilecache.Find(wfc => wfc.FilePath == file);
            if (item != null && (DateTime.Now - item.Timestamp).TotalSeconds < 600)
            {
                content = item.Content;
            }
            else
            {
                content = System.IO.File.ReadAllText(file);  //Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            }
            return content;
        }

        private void PutWebFileCache(string file, string content)
        {
            WebFileCache item = _webfilecache.Find(wfc => wfc.FilePath == file);
            if (item == null)
            {
                item = new WebFileCache();
                _webfilecache.Add(item);
            }
            item.FilePath = file;
            item.Content = content;
        }

        private string GetWebFilePath(string file)
        {
            string path = _webserviceconfig.HomePath;
            file = file.Replace(_webserviceconfig.BaseUrl, "");
            //
            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;
            //
            switch (pid)
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