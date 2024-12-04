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
 *     Project Homepage: https://homegenie.it
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Net;
using System.Net.Mail;

using System.Net.NetworkInformation;
using System.Collections.Specialized;
using System.Globalization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using HomeGenie.Service;
using HomeGenie.Data;
using HomeGenie.Service.Constants;

using NetClientLib;
using NLog;

namespace HomeGenie.Automation.Scripting
{
    /// <summary>
    /// Net Helper class.\n
    /// Class instance accessor: **Net**
    /// </summary>
    [Serializable]
    public class NetHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private string _webServiceUrl = "";
        private string _method = "post";
        private string _putData = "";

        private string _mailService = "";
        private int _mailPort = -1;
        // unset
        private bool? _mailSsl;

        // unset
        private readonly NameValueCollection _customHeaders = new NameValueCollection();
        private NetworkCredential _networkCredential;
        private bool _defaultCredentials;

        private readonly Dictionary<string, byte[]> _attachments = new Dictionary<string, byte[]>();

        // multithread safe lock objects
        private readonly object _smtpSyncLock = new object();
        //private object httpSyncLock = new object();

        private readonly HomeGenieService _homegenie;


        public NetHelper(HomeGenieService hg)
        {
            _homegenie = hg;
        }

        #region SMTP client

        /// <summary>
        /// Sets the SMTP server address for sending emails.
        /// If "E-Mail Account" program has been already configured, this method can be used to specify a different SMTP server to use for Net.SendMessage method.
        /// </summary>
        /// <returns>NetHelper.</returns>
        /// <param name="smtpServer">SMTP server address</param>
        public NetHelper MailService(string smtpServer)
        {
            _mailService = smtpServer;
            return this;
        }

        /// <summary>
        /// Sets the SMTP server address for sending emails.
        /// If "E-Mail Account" program has been already configured, this method can be used to specify a different SMTP server to use for Net.SendMessage method.
        /// </summary>
        /// <returns>NetHelper.</returns>
        /// <param name="smtpServer">SMTP server address</param>
        /// <param name="port">SMTP server port.</param>
        /// <param name="useSsl">If set to <c>true</c> use SSL.</param>
        public NetHelper MailService(string smtpServer, int port, bool useSsl)
        {
            _mailService = smtpServer;
            _mailPort = port;
            _mailSsl = useSsl;
            return this;
        }

        /// <summary>
        /// Adds an attachment to the message. Can be called multiple times for attaching more files.
        /// </summary>
        /// <returns>NetHelper</returns>
        /// <param name="name">File name (without path).</param>
        /// <param name="data">Binary data of the file to attach.</param>
        public NetHelper AddAttachment(string name, byte[] data)
        {
            _attachments.Add(name, data);
            return this;
        }

        /// <summary>
        /// Sends an E-Mail.
        /// </summary>
        /// <returns><c>true</c>, if message was sent, <c>false</c> otherwise.</returns>
        /// <param name="recipients">Message recipients.</param>
        /// <param name="subject">Message subject.</param>
        /// <param name="messageText">Message text.</param>
        public bool SendMessage(string recipients, string subject, string messageText)
        {
            var mailFrom = "";
            // this is a System Parameter
            var spEmailSender = _homegenie.Parameters.Find(mp => mp.Name == "Messaging.Email.Sender");
            if (spEmailSender != null && spEmailSender.Value != "")
            {
                mailFrom = spEmailSender.Value;
            }
            return SendMessage(mailFrom, recipients, subject, messageText);
        }

        /// <summary>
        /// Sends an E-Mail.
        /// </summary>
        /// <returns><c>true</c>, if message was sent, <c>false</c> otherwise.</returns>
        /// <param name="from">Message sender.</param>
        /// <param name="recipients">Message recipients.</param>
        /// <param name="subject">Message subject.</param>
        /// <param name="messageText">Message text.</param>
        public bool SendMessage(string from, string recipients, string subject, string messageText)
        {
            Log.Trace("SendMessage: called for recipients {0}", recipients);
            string mailFrom = from;
            string mailSubject = subject;
            string mailBody = messageText;
            lock (_smtpSyncLock)
            {
                using (var message = new MailMessage())
                {
                    var mailRecipients = recipients.Split(new[] {';', ','}, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var recipient in mailRecipients)
                    {
                        message.To.Add(recipient);
                    }
                    message.Subject = mailSubject;
                    message.From = new MailAddress(mailFrom);
                    message.Body = mailBody;
                    //
                    for (var a = 0; a < _attachments.Count; a++)
                    {
                        var attachment = new Attachment(new MemoryStream(_attachments.ElementAt(a).Value), _attachments.ElementAt(a).Key);
                        message.Attachments.Add(attachment);
                    }
                    //
                    string mailService = _mailService;
                    if (String.IsNullOrEmpty(mailService))
                    {
                        // this is a System Parameter
                        var spSmtpServer = _homegenie.Parameters.Find(mp => mp.Name == "Messaging.Email.SmtpServer");
                        if (spSmtpServer != null)
                        {
                            mailService = spSmtpServer.Value;
                        }
                    }
                    int mailPort = _mailPort;
                    if (mailPort == -1)
                    {
                        // this is a System Parameter
                        var spSmtpPort = _homegenie.Parameters.Find(mp => mp.Name == "Messaging.Email.SmtpPort");
                        if (spSmtpPort != null && spSmtpPort.DecimalValue > 0)
                        {
                            mailPort = (int) spSmtpPort.DecimalValue;
                        }
                    }
                    bool? mailSsl = _mailSsl;
                    if (!mailSsl.HasValue)
                    {
                        // this is a System Parameter
                        var spSmtpUseSsl = _homegenie.Parameters.Find(mp => mp.Name == "Messaging.Email.SmtpUseSsl");
                        if (spSmtpUseSsl != null && (spSmtpUseSsl.Value.ToLower() == "true" ||
                                                     spSmtpUseSsl.Value.ToLower() == "on" ||
                                                     spSmtpUseSsl.DecimalValue == 1))
                        {
                            mailSsl = true;
                        }
                    }
                    var credentials = _networkCredential;
                    if (credentials == null)
                    {
                        var username = "";
                        // this is a System Parameter
                        var spSmtpUserName = _homegenie.Parameters.Find(mp => mp.Name == "Messaging.Email.SmtpUserName");
                        if (spSmtpUserName != null)
                        {
                            username = spSmtpUserName.Value;
                        }
                        if (!string.IsNullOrWhiteSpace(username))
                        {
                            var password = "";
                            // this is a System Parameter
                            var spSmtpPassword = _homegenie.Parameters.Find(mp => mp.Name == "Messaging.Email.SmtpPassword");
                            if (spSmtpPassword != null)
                            {
                                password = spSmtpPassword.Value;
                            }
                            credentials = new NetworkCredential(username, password);
                        }
                    }
                    //
                    using (var smtpClient = new SmtpClient(mailService))
                    {
                        try
                        {
                            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                            smtpClient.UseDefaultCredentials = false;
                            smtpClient.Credentials = credentials;
                            smtpClient.Timeout = 30000;
                            if (mailPort > 0)
                            {
                                smtpClient.Port = mailPort;
                            }
                            smtpClient.EnableSsl = (mailSsl == true);

                            Log.Trace("SendMessage: going to send email {0} using mailService '{1}', port '{2}', credentials {3}, using SSL = {4}",
                                message.ToString(), mailService, mailPort, credentials, smtpClient.EnableSsl);
                            smtpClient.Send(message);
                            Log.Trace("Email sent");
                        }
                        catch (Exception ex)
                        {
                            Log.Trace(ex, "SendMessage: error sending email to {0} ({1}:{2})", recipients, mailService, mailPort);
                            Log.Error(ex);
                            HomeGenieService.LogError(Domains.HomeAutomation_HomeGenie_Automation, GetType().Name, ex.Message, "Exception.StackTrace", ex.StackTrace);
                            return false;
                        }
                        finally
                        {
                            Log.Trace("SendMessage: disposing smtpClient");
                            _attachments.Clear();
                            smtpClient.Dispose();
                        }
                    }
                }
            }
            return true;
        }

        #endregion


        #region IMAP client

        /// <summary>
        /// IMAP mail client helper.
        /// </summary>
        /// <returns>The IMAP client.</returns>
        /// <param name="host">Host.</param>
        /// <param name="port">Port.</param>
        /// <param name="useSsl">If set to <c>true</c> use ssl.</param>
        public ImapClient ImapClient(string host, int port, bool useSsl)
        {
            return new ImapClient(host, port, useSsl);
        }

        #endregion


        #region HTTP client

        /// <summary>
        /// Sets the web service URL to call.
        /// </summary>
        /// <returns>NetHelper.</returns>
        /// <param name="serviceUrl">Service URL.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// var iplocation = Net.WebService("http://freegeoip.net/json/").GetData();
        /// Program.Notify("IP to Location", iplocation.city);
        /// </code>
        /// </example>
        public NetHelper WebService(string serviceUrl)
        {
            _method = "";
            _customHeaders.Clear();
            _webServiceUrl = serviceUrl;
            return this;
        }

        /// <summary>
        /// Sends the specified string data using the PUT method.
        /// </summary>
        /// <param name="data">Data to send.</param>
        public NetHelper Put(string data)
        {
            _method = "PUT";
            _putData = data;
            return this;
        }

        /// <summary>
        /// Sends the specified string data using the POST method.
        /// </summary>
        /// <param name="data">String containing post data fields and values in the form field1=value1&filed2=value2&...&fieldn=valuen.</param>
        public NetHelper Post(string data)
        {
            _method = "POST";
            _putData = data;
            return this;
        }

        /// <summary>
        /// Adds the specified HTTP header to the HTTP request.
        /// </summary>
        /// <returns>NetHelper.</returns>
        /// <param name="name">Header name.</param>
        /// <param name="value">Header value.</param>
        public NetHelper AddHeader(string name, string value)
        {
            _customHeaders.Add(name, value);
            return this;
        }

        /// <summary>
        /// Calls the web service url.
        /// </summary>
        /// <returns>String containing the server response.</returns>
        public string Call()
        {
            string returnvalue = "";
            //lock(httpSyncLock)
            using (var webClient = new WebClient())
            {
                try
                {
                    webClient.Encoding = Encoding.UTF8;
                    if (_networkCredential != null)
                    {
                        webClient.Credentials = _networkCredential;
                    }
                    else if (_defaultCredentials)
                    {
                        webClient.UseDefaultCredentials = true;
                    }
                    webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0)");
                    if (_customHeaders.Count > 0)
                        webClient.Headers.Add(_customHeaders);
                    if (_method == "")
                    {
                        returnvalue = webClient.DownloadString(_webServiceUrl);
                    }
                    else
                    {
                        byte[] data = Encoding.UTF8.GetBytes(_putData);
                        byte[] responsebytes = webClient.UploadData(_webServiceUrl, _method, data);
                        returnvalue = Encoding.UTF8.GetString(responsebytes);
                    }
                }
                catch (Exception ex)
                {
                    HomeGenieService.LogError(Domains.HomeAutomation_HomeGenie_Automation, GetType().Name, ex.Message, "Exception.StackTrace", ex.StackTrace);
                }
                finally
                {
                    webClient.Dispose();
                }
            }
            return returnvalue;
        }

        /// <summary>
        /// Calls the web service url and returns the server response.
        /// </summary>
        /// <returns>The returned value can be a simple string or an object containing all fields mapped from the JSON/XML response (see Net.WebService example).</returns>
        public dynamic GetData()
        {
            dynamic returnValue = null;
            string response = Call();
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

        /// <summary>
        /// Calls the web service url and returns the server response as binary data.
        /// </summary>
        /// <returns>Byte array containing the raw server response.</returns>
        public byte[] GetBytes()
        {
            byte[] responseBytes = null;
            //lock(httpSyncLock)
            using (var webClient = new WebClient())
            {
                try
                {
                    if (_networkCredential != null)
                    {
                        webClient.Credentials = _networkCredential;
                    }
                    else if (_defaultCredentials)
                    {
                        webClient.UseDefaultCredentials = true;
                    }
                    webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0)");
                    if (_customHeaders.Count > 0)
                        webClient.Headers.Add(_customHeaders);
                    responseBytes = webClient.DownloadData(_webServiceUrl);
                }
                catch (Exception ex)
                {
                    HomeGenieService.LogError(Domains.HomeAutomation_HomeGenie_Automation, GetType().Name, ex.Message, "Exception.StackTrace", ex.StackTrace);
                }
                finally
                {
                    webClient.Dispose();
                }
            }
            return responseBytes;
        }

        #endregion


        #region Ping client

        /// <summary>
        /// Ping the specified remote host.
        /// </summary>
        /// <param name="remoteAddress">IP or DNS address.</param>
        public bool Ping(string remoteAddress)
        {
            const string data = "01010101010101010101010101010101";
            const int timeout = 1000;
            var success = false;

            using (var pingClient = new Ping())
            {
                var options = new PingOptions();
                //
                // Use the default Ttl value which is 128,
                // but change the fragmentation behavior.
                options.DontFragment = true;
                //
                // Create a buffer of 32 bytes of data to be transmitted.
                var buffer = Encoding.ASCII.GetBytes(data);

                var reply = pingClient.Send(remoteAddress, timeout, buffer, options);
                if (reply != null && reply.Status == IPStatus.Success)
                {
                    success = true;
                }
            }
            return success;
        }

        #endregion

        /// <summary>
        /// Uses provided credentials when connecting.
        /// </summary>
        /// <returns>NetHelper.</returns>
        /// <param name="user">Username.</param>
        /// <param name="pass">Password.</param>
        public NetHelper WithCredentials(string user, string pass)
        {
            _networkCredential = new NetworkCredential(user, pass);
            return this;
        }

        public NetHelper WithDefaultCredentials()
        {
            _defaultCredentials = true;
            return this;
        }

        public void Reset()
        {
            _webServiceUrl = "";
            _mailService = "localhost";
            _networkCredential = null;
        }

        private class WebClient : System.Net.WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                var webRequest = base.GetWebRequest(uri);
                // Disable Keep-Alive (this lead to poor performance, so let's keep it disabled by default)
                //if (w is HttpWebRequest)
                //{
                //    (w as HttpWebRequest).KeepAlive = false;
                //}
                // WebClient default timeout set to 10 seconds
                if (webRequest != null)
                    webRequest.Timeout = 10 * 1000;

                return webRequest;
            }
        }

    }

}
