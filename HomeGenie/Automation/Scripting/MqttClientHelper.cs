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

using uPLibrary.Networking.M2Mqtt;

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
        private NetworkCredential networkCredential = null;
        private MqttEndPoint endPoint = new MqttEndPoint();

        private MqttClient mqttClient = null;
        //private object mqttSyncLock = new object();

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
            mqttClient = new MqttClient(endPoint.Address, endPoint.Port, false, null, null, MqttSslProtocols.None);

            if (this.networkCredential != null)
            {
                mqttClient.Connect(this.endPoint.ClientId, this.networkCredential.UserName, this.networkCredential.Password);
            }
            else
            {
                mqttClient.Connect(endPoint.ClientId);
            }

            mqttClient.ConnectionClosed += (sender, e) => callback();

            return this;
        }
        
        /// <summary>
        /// Disconnects from the MQTT server.
        /// </summary>
        public MqttClientHelper Disconnect()
        {
            if (this.mqttClient != null && this.mqttClient.IsConnected)
            {
                this.mqttClient.Disconnect();
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
            mqttClient.MqttMsgPublishReceived += (sender, e) =>
            {
                var msg = Encoding.UTF8.GetString(e.Message);
                callback(e.Topic, msg);
            };

            mqttClient.Subscribe(new string[] { topic }, new byte[] { uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

            return this;
        }

        /// <summary>
        /// Publish a message to the specified topic.
        /// </summary>
        /// <param name="topic">Topic name.</param>
        /// <param name="message">Message text.</param>
        public MqttClientHelper Publish(string topic, string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            mqttClient.Publish(topic, body, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
            if (!mqttClient.IsConnected)
            {
                throw new Exception("Mqtt not connected when publishing");
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
            mqttClient.Publish(topic, message);
            if (!mqttClient.IsConnected)
            {
                throw new Exception("Mqtt not connected when publishing");
            }
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
            this.networkCredential = new NetworkCredential(user, pass);
            return this;
        }

        public void Reset()
        {
            this.networkCredential = null;
            this.endPoint = new MqttEndPoint();
            Disconnect();
        }

        #region private helper methods

        private void Connect()
        {
            Disconnect();
            mqttClient = new MqttClient(endPoint.Address, endPoint.Port, false, null, null, MqttSslProtocols.None);

            if (this.networkCredential != null)
            {
                mqttClient.Connect(this.endPoint.ClientId, this.networkCredential.UserName, this.networkCredential.Password);
            }
            else
            {
                mqttClient.Connect(endPoint.ClientId);
            }
        }

        #endregion

    }
}
