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
using System.Net;
using System.Text;
using System.Threading;

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Serializer;

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
        private static MqttFactory factory = new MqttFactory();
        private NetworkCredential networkCredential = null;
        private MqttEndPoint endPoint = new MqttEndPoint();
        private bool usingWebSockets;

        private MqttClient mqttClient;
        private string subscribeTopic;
        private Action<string, string> messageCallback;

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
            Connect();
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
            Connect();
            return this;
        }
        
        /// <summary>
        /// Connects to the MQTT server using the specified port and client identifier and a callback function in case of lost connection.
        /// </summary>
        /// <param name="port">MQTT server port.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="callback">The name of callback function, like RefreshConnection</param>
        /// <example>Action RefreshConnection = () => {...}</example>
        public MqttClientHelper Connect(int port, string clientId, Action callback)
        {
            endPoint.Port = port;
            endPoint.ClientId = clientId;
            Disconnect();
            mqttClient = (MqttClient)factory.CreateMqttClient();
            mqttClient.Connected += (sender, args) =>
            {
                callback();
                if (subscribeTopic != null) mqttClient.SubscribeAsync(subscribeTopic);
            };
            mqttClient.Disconnected += (sender, args) =>
            {
                Console.WriteLine(args.Exception.Message);
                Disconnect();
                Thread.Sleep(5000);
                Connect();
            };
            var options = GetMqttOption(clientId);
            mqttClient.ConnectAsync(options);            
            return this;
        }
        
        /// <summary>
        /// Disconnects from the MQTT server.
        /// </summary>
        public MqttClientHelper Disconnect()
        {
            if (mqttClient != null)
            {
                var m = mqttClient;
                mqttClient = null;
                if (messageCallback != null)
                {d .
                    m.ApplicationMessageReceived -= MessageReceived;
                    messageCallback = null;
                }
                subscribeTopic = null;
                if (m.IsConnected) m.DisconnectAsync();
                m.Dispose();
            }
            return this;
        }

        /// <summary>
        /// Subscribe the specified topic.
        /// </summary>
        /// <param name="topic">Topic name.</param>
        /// <param name="callback">Callback for receiving the subscribed topic messages.</param>
        public MqttClientHelper Subscribe(string topic, Action<string,string> callback)
        {
            subscribeTopic = topic;
            messageCallback = callback;
            if (mqttClient != null)
            {
                mqttClient.ApplicationMessageReceived += MessageReceived;
                if (mqttClient.IsConnected)
                {
                    mqttClient.SubscribeAsync(subscribeTopic);
                }
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
                mqttClient.PublishAsync(topic, message, MqttQualityOfServiceLevel.AtLeastOnce, false);
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
                mqttClient.PublishAsync(topic, Encoding.UTF8.GetString(message), MqttQualityOfServiceLevel.AtLeastOnce, false);
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

        public void Reset()
        {
            networkCredential = null;
            endPoint = new MqttEndPoint();
            Disconnect();
        }

        #region private helper methods

        private void Connect()
        {
            Disconnect();
            mqttClient = (MqttClient)factory.CreateMqttClient();
            mqttClient.Disconnected += (sender, args) =>
            {
                Console.WriteLine(args.Exception.Message);
                Disconnect();
                Thread.Sleep(5000);
                Connect();
            };
            var options = GetMqttOption(endPoint.ClientId);
            mqttClient.ConnectAsync(options);
        }

        private IMqttClientOptions GetMqttOption(string clientId)
        {
            var builder = new MqttClientOptionsBuilder()
                .WithProtocolVersion(MqttProtocolVersion.V311)
                .WithClientId(clientId);
                /*
                .WithWillMessage(new MqttApplicationMessage()
                {
                    Payload = Encoding.UTF8.GetBytes("Hello World!!!"),
                    Topic = "/homegenie",
                    Retain = true,
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce
                });
                */
                // TODO: ...
                //.WithKeepAlivePeriod(TimeSpan.FromSeconds(...))
                //.WithCommunicationTimeout(TimeSpan.FromSeconds(...))
                // .WithTls()
                //.WithCleanSession();
            if (usingWebSockets)
            {
                builder.WithWebSocketServer(endPoint.Address + ":" + endPoint.Port + "/mqtt");
            }
            else
            {
                builder.WithTcpServer(endPoint.Address, endPoint.Port);
            }
            if (networkCredential != null)
            {
                builder.WithCredentials(networkCredential.UserName, networkCredential.Password);
            }
            return builder.Build();
        }

        public void MessageReceived(object sender, MqttApplicationMessageReceivedEventArgs args)
        {
            var msg = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);
            messageCallback(args.ApplicationMessage.Topic, msg);
        }

        #endregion

    }
}
