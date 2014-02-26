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

namespace HomeGenie.Automation.Scripting
{
    public class NetHelper
    {

        private string webserviceurl = "";
        private string method = "post";
        private string putdata = "";
        private string mailservice = "";
        private int mailport = -1; // unset
        private int mailssl = -1; // unset
        private NameValueCollection customheaders = new NameValueCollection();
        private NetworkCredential networkcredential = null;
        private string mailfrom = "homegenie@localhost";
        private string mailto = "";
        private string mailbody = "";
        private string mailsubject = "";
        private Dictionary<string, byte[]> attachments = new Dictionary<string, byte[]>();

        private HomeGenieService _homegenie;

        public NetHelper(HomeGenieService hg)
        {
            ServicePointManager.ServerCertificateValidationCallback = Validator;
            _homegenie = hg;
        }

        public static bool Validator(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public NetHelper MailService(string smtpserver)
        {
            this.mailservice = smtpserver;
            return this;
        }
        public NetHelper MailService(string smtpserver, int port, bool usessl)
        {
            this.mailport = port;
            this.mailssl = (usessl ? 1 : 0);
            return this;
        }
        /*
        public NetHelper MailFrom(string mailfrom)
        {
            this.mailfrom = mailfrom;
            return this;
        }
        public NetHelper MailSubject(string mailsubject)
        {
            this.mailsubject = mailsubject;
            return this;
        }
        public NetHelper MailBody(string mailbody)
        {
            this.mailbody = mailbody;
            return this;
        }
         */
        public NetHelper WithCredentials(string user, string pass)
        {
            this.networkcredential = new NetworkCredential(user, pass);
            return this;
        }
        // TODO: implement a callback
        public void SendMessageAsync(string from, string recipients, string subject, string messagetext)
        {
            Thread t = new Thread(new ThreadStart(delegate()
            {
                this.SendMessage(from, recipients, subject, messagetext);
            }));
            t.Start();
        }
        //
        public NetHelper AddAttachment(string name, byte[] data)
        {
            attachments.Add(name, data);
            return this;
        }
        //
        public bool SendMessage(string recipients, string subject, string messagetext)
        {
            this.mailfrom = "";
            ModuleParameter systemparam = _homegenie.Parameters.Find(delegate(ModuleParameter mp) { return mp.Name == "Messaging.Email.Sender"; });
            if (systemparam != null && systemparam.Value != "")
            {
                this.mailfrom = systemparam.Value;
            }
            return SendMessage(this.mailfrom, recipients, subject, messagetext);
        }
        public bool SendMessage(string from, string recipients, string subject, string messagetext)
        {
            try
            {
                this.mailfrom = from;
                this.mailto = recipients;
                this.mailsubject = subject;
                this.mailbody = messagetext;
                //
                MailMessage message = new System.Net.Mail.MailMessage();
                string[] tomails = recipients.Split(';');
                for (int e = 0; e < tomails.Length; e++)
                {
                    message.To.Add(tomails[e]);
                }
                message.Subject = this.mailsubject;
                message.From = new MailAddress(this.mailfrom);
                message.Body = this.mailbody;
                //
                for (int a = 0; a < attachments.Count; a++)
                {
                    Attachment att = new Attachment(new MemoryStream(attachments.ElementAt(a).Value), attachments.ElementAt(a).Key);
                    message.Attachments.Add(att);
                }
                //
                if (this.mailservice == "")
                {
                    ModuleParameter systemparam = _homegenie.Parameters.Find(delegate(ModuleParameter mp) { return mp.Name == "Messaging.Email.SmtpServer"; });
                    if (systemparam != null)
                    {
                        this.mailservice = systemparam.Value;
                    }
                }
                if (this.mailport == -1)
                {
                    ModuleParameter systemparam = _homegenie.Parameters.Find(delegate(ModuleParameter mp) { return mp.Name == "Messaging.Email.SmtpPort"; });
                    if (systemparam != null && systemparam.DecimalValue > 0)
                    {
                        this.mailport = (int)systemparam.DecimalValue;
                    }
                }
                if (this.mailssl == -1)
                {
                    ModuleParameter systemparam = _homegenie.Parameters.Find(delegate(ModuleParameter mp) { return mp.Name == "Messaging.Email.SmtpUseSsl"; });
                    if (systemparam != null && systemparam.Value.ToLower() == "true")
                    {
                        this.mailssl = 1;
                    }
                }
                if (this.networkcredential == null)
                {
                    var username = "";
                    ModuleParameter systemparam = _homegenie.Parameters.Find(delegate(ModuleParameter mp) { return mp.Name == "Messaging.Email.SmtpUserName"; });
                    if (systemparam != null)
                    {
                        username = systemparam.Value;
                    }
                    if (username != "")
                    {
                        var password = "";
                        systemparam = _homegenie.Parameters.Find(delegate(ModuleParameter mp) { return mp.Name == "Messaging.Email.SmtpPassword"; });
                        if (systemparam != null)
                        {
                            password = systemparam.Value;
                        }
                        this.networkcredential = new NetworkCredential(username, password);
                    }
                }
                //
                SmtpClient smtp = new SmtpClient(this.mailservice);
                smtp.Credentials = this.networkcredential;
                if (this.mailport > 0)
                {
                    smtp.Port = this.mailport;
                }
                if (this.mailssl > 0)
                {
                    smtp.EnableSsl = (this.mailssl == 1);
                }
                smtp.Send(message);
                smtp.Dispose();
                //
                attachments.Clear();
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie_Automation, this.GetType().Name, ex.Message, "Exception.StackTrace", ex.StackTrace);
                Console.WriteLine("Net.SendMail ERROR: " + ex.Message + "\n" + ex.StackTrace);
                return false;
            }
            return true;
        }
        public NetHelper WebService(string serviceurl)
        {
            this.method = "";
            this.customheaders.Clear();
            this.webserviceurl = serviceurl;
            return this;
        }
        public NetHelper Put(string data)
        {
            this.method = "PUT";
            this.putdata = data;
            return this;
        }
        public NetHelper Post(string data)
        {
            this.method = "POST";
            this.putdata = data;
            return this;
        }

        public NetHelper AddHeader(string name, string value)
        {
            customheaders.Add(name, value);
            return this;
        }

        public string Call()
        {
            string returnvalue = "";
            try
            {
                WebClient webcli = new WebClient();
                if (this.networkcredential != null)
                {
                    webcli.Credentials = networkcredential;
                }
                webcli.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0)");
                webcli.Headers.Add(customheaders);
                if (this.method == "")
                {
                    returnvalue = webcli.DownloadString(this.webserviceurl);
                }
                else
                {
                    byte[] data = Encoding.ASCII.GetBytes(this.putdata);
                    byte[] responsebytes = webcli.UploadData(this.webserviceurl, this.method, data);
                    returnvalue = Encoding.ASCII.GetString(responsebytes);
                }
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie_Automation, this.GetType().Name, ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
            return returnvalue;
        }
        // TODO: implement a callback
        public void CallAsync()
        {
            Thread t = new Thread(new ThreadStart(delegate()
                {
                    this.Call();
                }));
            t.Start();
        }

        public dynamic GetData()
        {
            dynamic returnvalue = null;
            string response = this.Call();
            if (response.Trim().StartsWith("<?xml"))
            {
                returnvalue = Utility.XmlParseToDynamic(response);
            }
            else
            {
                string json = "[" + response + "]";
                try
                {
                    returnvalue = (JsonConvert.DeserializeObject(json) as JArray)[0];
                }
                catch
                {
                    returnvalue = response;
                }
            }
            return returnvalue;
        }

        public byte[] GetBytes()
        {
            byte[] responsebytes = null;
            try
            {
                WebClient webcli = new WebClient();
                if (this.networkcredential != null)
                {
                    webcli.Credentials = networkcredential;
                }
                webcli.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0)");
                responsebytes = webcli.DownloadData(this.webserviceurl);
            }
            catch { }
            return responsebytes;
        }

        public bool Ping(string ipaddress)
        {
            bool success = false;
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            //
            // Use the default Ttl value which is 128,
            // but change the fragmentation behavior.
            options.DontFragment = true;
            //
            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "01010101010101010101010101010101";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 1000;
            PingReply reply = pingSender.Send(ipaddress, timeout, buffer, options);
            if (reply.Status == IPStatus.Success)
            {
                success = true;
            }
            return success;
        }


        public NetHelper SignalModuleEvent(string hgaddress, ModuleHelper m, ModuleParameter p)
        {
            string eventrouteurl = "http://" + hgaddress + "/api/HomeAutomation.HomeGenie/Interconnection/Events.Push/" + _homegenie.GetHttpServicePort();
            // propagate event to remote hg endpoint
            this.WebService(eventrouteurl)
              .Put(JsonConvert.SerializeObject(new ModuleEvent(m.SelectedModules[0], p)))
              .CallAsync();
            return this;
        }

        public void Reset()
        {
            this.webserviceurl = "";
            this.mailservice = "localhost";
            this.networkcredential = null;
            this.mailto = "";
            this.mailbody = "";
            this.mailsubject = "";
        }


    }
}
