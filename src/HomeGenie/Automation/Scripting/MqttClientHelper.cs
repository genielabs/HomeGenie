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
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

using NLog;
using MqttClient = MQTTnet.Client.MqttClient;

namespace HomeGenie.Automation.Scripting
{
    public class MqttEndPoint
    {
        public string Address = "127.0.0.1";
        public int Port = 1883;
        public string ClientId = "hg-" + new Random().Next(10000).ToString();
    }

    /// <summary>
    /// MQTT client helper.
    /// Class instance accessor: **MqttClient**
    /// </summary>
    [Serializable]
    public class MqttClientHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly MqttFactory Factory = new MqttFactory();
        private NetworkCredential networkCredential;
        private MqttEndPoint endPoint = new MqttEndPoint();
        private bool usingWebSockets;
        private bool useTls;

        private MqttClient mqttClient;
        private readonly Dictionary<string, Action<string, byte[]>> subscribeTopics = new Dictionary<string, Action<string, byte[]>>();

        /// <summary>
        /// Sets the MQTT server to use.
        /// </summary>
        /// <param name="server">MQTT server address.</param>
        public MqttClientHelper Service(string server)
        {
            endPoint.Address = server;
            return this;
        }

        /// <summary>
        /// Connects to the MQTT server using the default port (1883) and the specified client identifier.
        /// </summary>
        /// <param name="clientId">The client identifier.</param>
        public MqttClientHelper Connect(string clientId)
        {
            endPoint.ClientId = clientId;
            Connect(endPoint.Port, endPoint.ClientId, null);
            return this;
        }

        /// <summary>
        /// Connects to the MQTT server using the specified port and client identifier.
        /// </summary>
        /// <param name="port">MQTT server port.</param>
        /// <param name="clientId">The client identifier.</param>
        public MqttClientHelper Connect(int port, string clientId)
        {
            endPoint.Port = port;
            endPoint.ClientId = clientId;
            Connect(endPoint.Port, endPoint.ClientId, null);
            return this;
        }

        /// <summary>
        /// Connects to the MQTT server using the specified port and client identifier.
        /// </summary>
        /// <param name="port">MQTT server port.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="callback">Optional callback `Action&lt;bool&gt;` invoked when the connection status changed (the argument value will be true if connected, false otherwise)</param>
        /// <example>
        /// MqttClient
        ///     .Service(server)
        ///     .UsingWebSockets(useWebSockets)
        ///     .Connect(port, clientid, (connected) => {
        ///     MqttIsConnected = connected;
        ///     if (connected) {
        ///         Program.Notify("Connected!");
        ///     } else {
        ///         Program.Notify("Disconnected!");
        ///     }
        /// });
        /// </example>
        public MqttClientHelper Connect(int port, string clientId, Action<bool> callback = null)
        {
            Connect(port, clientId, null, callback);
            return this;
        }

        /// <summary>
        /// Connects to the MQTT server using the specified port / client identifier and the specified client options.
        /// </summary>
        /// <param name="port">MQTT server port.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="clientOptionsCallback">Callback `Action&lt;MqttClientOptionsBuilder&gt;` invoked before the connection is established to allow setting advanced connection options. See https://github.com/chkr1011/MQTTnet/wiki/Client for all available options.</param>
        /// <param name="callback">Optional callback `Action&lt;bool&gt;` invoked when the connection status changed (the argument value will be true if connected, false otherwise)</param>
        /// <example>
        /// Example:
        /// <code>
        /// MqttClient
        ///     .Service(server)
        ///     .UsingWebSockets(useWebSockets)
        ///     .Connect(port, clientid, (options) => {
        ///        options.WithTls(new MqttClientOptionsBuilderTlsParameters {
        ///            UseTls = true,
        ///            CertificateValidationCallback = (X509Certificate x, X509Chain y, SslPolicyErrors z, IMqttClientOptions o) =>
        ///            {
        ///                // TODO: Check conditions of certificate by using above parameters.
        ///                return true;
        ///            }
        ///        });
        ///     }, (connected) => {
        ///     MqttIsConnected = connected;
        ///     if (connected) {
        ///         Program.Notify("Connected!");
        ///     } else {
        ///         Program.Notify("Disconnected!");
        ///     }
        /// });
        /// </code>
        /// </example>
        public MqttClientHelper Connect(int port, string clientId, Action<MqttClientOptionsBuilder> clientOptionsCallback, Action<bool> callback = null)
        {
            endPoint.Port = port;
            endPoint.ClientId = clientId;
            Disconnect();
            mqttClient = (MqttClient)Factory.CreateMqttClient();
            mqttClient.ConnectedAsync += async e =>
            {
                if (callback != null)
                {
                    callback(true);
                }
                foreach(KeyValuePair<string, Action<string, byte[]>> subscription in subscribeTopics)
                {
                    await mqttClient.SubscribeAsync(new MqttTopicFilter() { Topic = subscription.Key });
                }
            };
            mqttClient.DisconnectedAsync += e =>
            {
                Log.Debug(e.Exception, "MqttClient Error");
                if (callback != null)
                {
                    callback(false);
                }
                return Task.CompletedTask;
            };
            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                MessageReceived(e);
                return Task.CompletedTask;
            };
            var clientOptionsBuilder = GetMqttOptionsBuilder(clientId);
            if (clientOptionsCallback != null)
            {
                clientOptionsCallback(clientOptionsBuilder);
            }
            mqttClient.ConnectAsync(clientOptionsBuilder.Build());
            return this;
        }

        /// <summary>
        /// Disconnects from the MQTT server.
        /// </summary>
        public MqttClientHelper Disconnect()
        {
            if (mqttClient != null)
            {
                if (mqttClient.IsConnected)
                {
                    mqttClient.DisconnectAsync();
                }
                mqttClient.Dispose();
                mqttClient = null;
            }
            return this;
        }

        /// <summary>
        /// Subscribes the specified topic.
        /// </summary>
        /// <param name="topic">Topic name.</param>
        /// <param name="callback">Callback for receiving the subscribed topic messages.</param>
        public MqttClientHelper Subscribe(string topic, Action<string, byte[]> callback)
        {
            if (!subscribeTopics.ContainsKey(topic))
            {
                subscribeTopics.Add(topic, callback);
                if (mqttClient != null && mqttClient.IsConnected)
                {
                    mqttClient.SubscribeAsync(topic);
                }
            }
            return this;
        }

        /// <summary>
        /// Unsubscribes the specified topic.
        /// </summary>
        /// <param name="topic">Topic name.</param>
        public MqttClientHelper Unsubscribe(string topic)
        {
            if (subscribeTopics.ContainsKey(topic))
            {
                subscribeTopics.Remove(topic);
            }
            return this;
        }

        /// <summary>
        /// Publish a message to the specified topic.
        /// </summary>
        /// <param name="topic">Topic name.</param>
        /// <param name="message">Message text.</param>
        public MqttClientHelper Publish(string topic, string message)
        {
            if (mqttClient != null)
            {
                mqttClient.PublishAsync(new MqttApplicationMessage()
                {
                    Topic = topic,
                    PayloadSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                    Retain = false
                });
            }
            return this;
        }

        /// <summary>
        /// Publish a message to the specified topic.
        /// </summary>
        /// <param name="topic">Topic name.</param>
        /// <param name="message">Message text as byte array.</param>
        public MqttClientHelper Publish(string topic, byte[] message)
        {
            if (mqttClient != null)
            {
                mqttClient.PublishAsync(new MqttApplicationMessage()
                {
                    Topic = topic,
                    PayloadSegment = new ArraySegment<byte>(message),
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                    Retain = false
                });
            }
            return this;
        }

        /// <summary>
        /// Publish a message using advanced options.
        /// </summary>
        /// <param name="applicationMessage">`MqttApplicationMessage` instance (See https://github.com/chkr1011/MQTTnet/wiki/Client#publishing-messages for documentation).</param>
        public MqttClientHelper Publish(MqttApplicationMessage applicationMessage)
        {
            if (mqttClient != null)
            {
                mqttClient.PublishAsync(applicationMessage);
            }
            return this;
        }

        /// <summary>
        /// Connect over WebSocket (default = false).
        /// </summary>
        /// <returns>NetHelper.</returns>
        /// <param name="useWebSocket">true/false</param>
        public MqttClientHelper UsingWebSockets(bool useWebSocket)
        {
            usingWebSockets = useWebSocket;
            return this;
        }

        /// <summary>
        /// Use provided credentials when connecting.
        /// </summary>
        /// <returns>NetHelper.</returns>
        /// <param name="user">Username.</param>
        /// <param name="pass">Password.</param>
        public MqttClientHelper WithCredentials(string user, string pass)
        {
            networkCredential = new NetworkCredential(user, pass);
            return this;
        }

        /// <summary>
        /// Sets whether to connect using TLS/SSL or not.
        /// </summary>
        /// <param name="useTls">Use TLS/SSL.</param>
        /// <returns></returns>
        public MqttClientHelper WithTls(bool useTls)
        {
            this.useTls = useTls;
            return this;
        }

        public void Reset()
        {
            networkCredential = null;
            endPoint = new MqttEndPoint();
            Disconnect();
        }

        #region private helper methods

        private MqttClientOptionsBuilder GetMqttOptionsBuilder(string clientId)
        {
            var builder = new MqttClientOptionsBuilder()
                .WithProtocolVersion(MqttProtocolVersion.V311)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(5))
                .WithTimeout(TimeSpan.FromSeconds(15))
                .WithClientId(clientId)
                // this message will be sent to all clients
                // subscribed to <clientId>/status topic
                // if this client gets disconnected
                .WithWillPayload("disconnected")
                .WithCleanSession();
            if (usingWebSockets)
            {
                builder.WithWebSocketServer(o => o.WithUri(endPoint.Address + ":" + endPoint.Port + "/mqtt"));
            }
            else
            {
                builder.WithTcpServer(endPoint.Address, endPoint.Port);
            }
            if (networkCredential != null)
            {
                builder.WithCredentials(networkCredential.UserName, networkCredential.Password);
            }
            if (useTls)
            {
                builder.WithTlsOptions(o => o.UseTls());
            }
            return builder;
        }

        private void MessageReceived(MqttApplicationMessageReceivedEventArgs args)
        {
            var msg = args.ApplicationMessage.PayloadSegment.Array;
            var topic = args.ApplicationMessage.Topic;
            foreach(KeyValuePair<string, Action<string, byte[]>> subscription in subscribeTopics)
            {
                if (MqttTopicFilterComparer.Compare(topic, subscription.Key) != MqttTopicFilterCompareResult.IsMatch) continue;
                var callback = subscription.Value;
                if (callback != null)
                {
                    callback(args.ApplicationMessage.Topic, msg);
                }
            }
        }

        #endregion

    }
}
