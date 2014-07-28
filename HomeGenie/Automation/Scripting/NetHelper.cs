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
using System.Text;

using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading;

//using Microsoft.Runtime;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using HomeGenie.Service;
using HomeGenie.Data;
using System.Net.NetworkInformation;
using System.Collections.Specialized;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using HomeGenie.Service.Constants;
using Nmqtt;

namespace HomeGenie.Automation.Scripting
{
    public class NetHelper
    {

        private string webServiceUrl = "";
        private string method = "post";
        private string putData = "";
        private string mailService = "";
        private int mailPort = -1;
        // unset
        private int mailSsl = -1;
        // unset
        private NameValueCollection customHeaders = new NameValueCollection();
        private NetworkCredential networkCredential = null;
        private string mailFrom = "homegenie@localhost";
        //private string mailTo = "";
        private string mailBody = "";
        private string mailSubject = "";
        private Dictionary<string, byte[]> attachments = new Dictionary<string, byte[]>();

        private MqttClient mqttClient = null;

        private HomeGenieService homegenie;

        public NetHelper(HomeGenieService hg)
        {
            ServicePointManager.ServerCertificateValidationCallback = Validator;
            homegenie = hg;
        }

        public static bool Validator(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors
        )
        {
            return true;
        }


        #region SMTP client

        public NetHelper MailService(string smtpServer)
        {
            this.mailService = smtpServer;
            return this;
        }

        public NetHelper MailService(string smtpServer, int port, bool useSsl)
        {
            this.mailPort = port;
            this.mailSsl = (useSsl ? 1 : 0);
            return this;
        }

        public NetHelper WithCredentials(string user, string pass)
        {
            this.networkCredential = new NetworkCredential(user, pass);
            return this;
        }
        // TODO: implement a callback
        public void SendMessageAsync(string from, string recipients, string subject, string messageText)
        {
            var t = new Thread(() =>
            {
                this.SendMessage(from, recipients, subject, messageText);
            });
            t.Start();
        }
        //
        public NetHelper AddAttachment(string name, byte[] data)
        {
            attachments.Add(name, data);
            return this;
        }
        //
        public bool SendMessage(string recipients, string subject, string messageText)
        {
            this.mailFrom = "";
            // this is a System Parameter
            var spEmailSender = homegenie.Parameters.Find(delegate(ModuleParameter mp)
            {
                return mp.Name == "Messaging.Email.Sender";
            });
            if (spEmailSender != null && spEmailSender.Value != "")
            {
                this.mailFrom = spEmailSender.Value;
            }
            return SendMessage(this.mailFrom, recipients, subject, messageText);
        }

        public bool SendMessage(string from, string recipients, string subject, string messageText)
        {
            try
            {
                this.mailFrom = from;
                //this.mailTo = recipients;
                this.mailSubject = subject;
                this.mailBody = messageText;
                //
                using (var message = new System.Net.Mail.MailMessage())
                {
                    string[] mailRecipients = recipients.Split(';');
                    for (int e = 0; e < mailRecipients.Length; e++)
                    {
                        message.To.Add(mailRecipients[e]);
                    }
                    message.Subject = this.mailSubject;
                    message.From = new MailAddress(this.mailFrom);
                    message.Body = this.mailBody;
                    //
                    for (int a = 0; a < attachments.Count; a++)
                    {
                        var attachment = new Attachment(
                                             new MemoryStream(attachments.ElementAt(a).Value),
                                             attachments.ElementAt(a).Key
                                         );
                        message.Attachments.Add(attachment);
                    }
                    //
                    if (this.mailService == "")
                    {
                        // this is a System Parameter
                        var spSmtpServer = homegenie.Parameters.Find(delegate(ModuleParameter mp)
                        {
                            return mp.Name == "Messaging.Email.SmtpServer";
                        });
                        if (spSmtpServer != null)
                        {
                            this.mailService = spSmtpServer.Value;
                        }
                    }
                    if (this.mailPort == -1)
                    {
                        // this is a System Parameter
                        var spSmtpPort = homegenie.Parameters.Find(delegate(ModuleParameter mp)
                        {
                            return mp.Name == "Messaging.Email.SmtpPort";
                        });
                        if (spSmtpPort != null && spSmtpPort.DecimalValue > 0)
                        {
                            this.mailPort = (int)spSmtpPort.DecimalValue;
                        }
                    }
                    if (this.mailSsl == -1)
                    {
                        // this is a System Parameter
                        var spSmtpUseSsl = homegenie.Parameters.Find(delegate(ModuleParameter mp)
                        {
                            return mp.Name == "Messaging.Email.SmtpUseSsl";
                        });
                        if (spSmtpUseSsl != null && spSmtpUseSsl.Value.ToLower() == "true")
                        {
                            this.mailSsl = 1;
                        }
                    }
                    if (this.networkCredential == null)
                    {
                        var username = "";
                        // this is a System Parameter
                        var spSmtpUserName = homegenie.Parameters.Find(delegate(ModuleParameter mp)
                        {
                            return mp.Name == "Messaging.Email.SmtpUserName";
                        });
                        if (spSmtpUserName != null)
                        {
                            username = spSmtpUserName.Value;
                        }
                        if (username != "")
                        {
                            var password = "";
                            // this is a System Parameter
                            var spSmtpPassword = homegenie.Parameters.Find(delegate(ModuleParameter mp)
                            {
                                return mp.Name == "Messaging.Email.SmtpPassword";
                            });
                            if (spSmtpPassword != null)
                            {
                                password = spSmtpPassword.Value;
                            }
                            this.networkCredential = new NetworkCredential(username, password);
                        }
                    }
                    //
                    using (var smtpClient = new SmtpClient(this.mailService))
                    {
                        smtpClient.Credentials = this.networkCredential;
                        if (this.mailPort > 0)
                        {
                            smtpClient.Port = this.mailPort;
                        }
                        if (this.mailSsl > 0)
                        {
                            smtpClient.EnableSsl = (this.mailSsl == 1);
                        }
                        smtpClient.Send(message);
                        smtpClient.Dispose();
                        //
                        attachments.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(
                    Domains.HomeAutomation_HomeGenie_Automation,
                    this.GetType().Name,
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
                return false;
            }
            return true;
        }

        #endregion


        #region HTTP client

        public NetHelper WebService(string serviceurl)
        {
            this.method = "";
            this.customHeaders.Clear();
            this.webServiceUrl = serviceurl;
            return this;
        }

        public NetHelper Put(string data)
        {
            this.method = "PUT";
            this.putData = data;
            return this;
        }

        public NetHelper Post(string data)
        {
            this.method = "POST";
            this.putData = data;
            return this;
        }

        public NetHelper AddHeader(string name, string value)
        {
            customHeaders.Add(name, value);
            return this;
        }

        public string Call()
        {
            string returnvalue = "";
            try
            {
                using (var webClient = new WebClient())
                {
                    webClient.Encoding = Encoding.UTF8;
                    if (this.networkCredential != null)
                    {
                        webClient.Credentials = networkCredential;
                    }
                    webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0)");
                    webClient.Headers.Add(customHeaders);
                    if (this.method == "")
                    {
                        returnvalue = webClient.DownloadString(this.webServiceUrl);
                    }
                    else
                    {
                        byte[] data = Encoding.ASCII.GetBytes(this.putData);
                        byte[] responsebytes = webClient.UploadData(this.webServiceUrl, this.method, data);
                        returnvalue = Encoding.ASCII.GetString(responsebytes);
                    }
                }
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(
                    Domains.HomeAutomation_HomeGenie_Automation,
                    this.GetType().Name,
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }
            return returnvalue;
        }
        // TODO: implement a callback
        public void CallAsync()
        {
            var t = new Thread(() =>
            {
                this.Call();
            });
            t.Start();
        }

        public dynamic GetData()
        {
            dynamic returnValue = null;
            string response = this.Call();
            if (response.Trim().StartsWith("<?xml"))
            {
                returnValue = Utility.ParseXmlToDynamic(response);
            }
            else
            {
                string json = "[" + response + "]";
                try
                {
                    returnValue = (JsonConvert.DeserializeObject(json) as JArray)[0];
                }
                catch
                {
                    returnValue = response;
                }
            }
            return returnValue;
        }

        public byte[] GetBytes()
        {
            byte[] responseBytes = null;
            try
            {
                using (var webClient = new WebClient())
                {
                    if (this.networkCredential != null)
                    {
                        webClient.Credentials = networkCredential;
                    }
                    webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0)");
                    responseBytes = webClient.DownloadData(this.webServiceUrl);
                }
            }
            catch
            {
            }
            return responseBytes;
        }

        #endregion


        #region Ping client

        public bool Ping(string ipAddress)
        {
            bool success = false;
            using (var pingClient = new Ping())
            {
                var options = new PingOptions();
                //
                // Use the default Ttl value which is 128,
                // but change the fragmentation behavior.
                options.DontFragment = true;
                //
                // Create a buffer of 32 bytes of data to be transmitted.
                string data = "01010101010101010101010101010101";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 1000;
                var reply = pingClient.Send(ipAddress, timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    success = true;
                }
            }
            return success;
        }

        #endregion


        #region MQTT client

        public NetHelper MqttService(string server, int port, string topic)
        {
            mqttClient = new MqttClient (server, port, topic);
            mqttClient.Connect();
            return this;
        }

        public NetHelper MqttService(string server, int port, string username, string password, string topic)
        {
            mqttClient = new MqttClient (server, port, topic);
            mqttClient.Connect(username, password);
            return this;
        }

        public NetHelper Subscribe(string topic, Action<string,string> callback)
        {
            mqttClient.ListenTo<String, AsciiPayloadConverter>(topic, (MqttQos)1)
                //.ObserveOn(System.Threading.SynchronizationContext.Current)
                .Subscribe(msg => { 
                    callback(msg.Topic, msg.Payload);
                    //Console.WriteLine("MQTT {0} : {1}", msg.Topic, msg.Payload); 
                });
            return this;
        }

        public NetHelper Publish(string topic, string message)
        {
            mqttClient.PublishMessage<string, AsciiPayloadConverter>(topic, (MqttQos)1, message);
            return this;
        }

        #endregion

        public NetHelper SignalModuleEvent(string hgAddress, ModuleHelper module, ModuleParameter parameter)
        {
            string eventRouteUrl = "http://" + hgAddress + "/api/HomeAutomation.HomeGenie/Interconnection/Events.Push/" + homegenie.GetHttpServicePort();
            // propagate event to remote hg endpoint
            this.WebService(eventRouteUrl)
              .Put(JsonConvert.SerializeObject(new ModuleEvent(module.SelectedModules[0], parameter)))
              .CallAsync();
            return this;
        }

        public void Reset()
        {
            this.webServiceUrl = "";
            this.mailService = "localhost";
            this.networkCredential = null;
            //this.mailTo = "";
            this.mailBody = "";
            this.mailSubject = "";
            //
            if (this.mqttClient != null)
            {
                try { this.mqttClient.Dispose(); } catch { }
            }
        }


    }
}
