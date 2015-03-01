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

using KNXLib;
using KNXLib.DPT;

namespace HomeGenie.Automation.Scripting
{

    /// <summary>
    /// KNX client helper.
    /// Class instance accessor: **KnxClient**
    /// </summary>
    public class KnxClientHelper
    {
        public class KnxEndPoint
        {
            public string LocalIp = null;
            public int LocalPort = 0;
            public string RemoteIp = null;
            public int RemotePort = 0;
        }

        private KNXConnection knxClient = null;
        private KnxEndPoint knxEndPoint = null;
        private Action<string, string> statusReceived;
        private Action<string, string> eventReceived;
        private Action<bool> statusChanged;

        /// <summary>
        /// Set the endpoint to connect to.
        /// </summary>
        /// <param name="host">Endpoint address.</param>
        /// <param name="port">Endpoint port.</param>
        public KnxClientHelper EndPoint(string host, int port)
        {
            knxEndPoint = new KnxEndPoint() { LocalIp = host, LocalPort = port };
            return this;
        }

        /// <summary>
        /// Set the endpoint to connect to.
        /// </summary>
        /// <param name="host">Endpoint address.</param>
        public KnxClientHelper EndPoint(string host)
        {
            knxEndPoint = new KnxEndPoint() { LocalIp = host };
            return this;
        }

        /// <summary>
        /// Set the endpoint to connect to.
        /// </summary>
        /// <param name="port">Endpoint port.</param>
        public KnxClientHelper EndPoint(int port)
        {
            knxEndPoint = new KnxEndPoint() { LocalPort = port };
            return this;
        }

        /// <summary>
        /// Set the endpoint to connect to using tunneling.
        /// </summary>
        /// <param name="localIp">Local IP.</param>
        /// <param name="localPort">Local port.</param>
        /// <param name="remoteIp">Remote IP.</param>
        /// <param name="remotePort">Remote port.</param>
        public KnxClientHelper EndPoint(string localIp, int localPort, string remoteIp, int remotePort)
        {
            knxEndPoint = new KnxEndPoint() { 
                LocalIp = localIp, 
                LocalPort = localPort, 
                RemoteIp = remoteIp, 
                RemotePort = remotePort 
            };
            return this;
        }
        
        /// <summary>
        /// Connect to the remote host using the specified port.
        /// </summary>
        /// <param name="port">Port number.</param>
        public KnxClientHelper Connect()
        {
            if (knxClient != null)
            {
                knxClient.Disconnect();
            }
            if (knxEndPoint == null)
            {
                knxClient = new KNXConnectionRouting();
            }
            else
            {
                if (knxEndPoint.RemoteIp != null && knxEndPoint.LocalIp != null)
                {
                    knxClient = new KNXConnectionTunneling(knxEndPoint.RemoteIp, knxEndPoint.RemotePort, knxEndPoint.LocalIp, knxEndPoint.LocalPort);
                }
                else if (knxEndPoint.LocalIp != null && knxEndPoint.LocalPort > 0)
                {
                    knxClient = new KNXConnectionRouting(knxEndPoint.LocalIp, knxEndPoint.LocalPort);
                }
                else if (knxEndPoint.LocalIp != null && knxEndPoint.LocalPort == 0)
                {
                    knxClient = new KNXConnectionRouting(knxEndPoint.LocalIp);
                }
                else if (knxEndPoint.LocalPort > 0)
                {
                    knxClient = new KNXConnectionRouting(knxEndPoint.LocalPort);
                }
            }
            knxClient.Connect();
            knxClient.KNXConnectedDelegate += knxClient_Connected;
            knxClient.KNXDisconnectedDelegate += knxClient_Disconnected;
            knxClient.KNXEventDelegate += knxClient_EventReceived;
            knxClient.KNXStatusDelegate += knxClient_StatusReceived;
            return this;
        }
        
        /// <summary>
        /// Disconnects from the remote host.
        /// </summary>
        public KnxClientHelper Disconnect()
        {
            if (knxClient != null)
            {
                knxClient.KNXConnectedDelegate -= knxClient_Connected;
                knxClient.KNXDisconnectedDelegate -= knxClient_Disconnected;
                knxClient.KNXEventDelegate -= knxClient_EventReceived;
                knxClient.KNXStatusDelegate -= knxClient_StatusReceived;
                try { knxClient.Disconnect(); } catch { }
                knxClient = null;
            }
            return this;
        }

        /// <summary>
        /// Send action data to the specified address.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="data">boolean action value.</param>
        public KnxClientHelper Action(string address, bool data)
        {
            knxClient.Action(address, data);
            return this;
        }

        /// <summary>
        /// Send action data to the specified address.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="data">int action value.</param>
        public KnxClientHelper Action(string address, int data)
        {
            knxClient.Action(address, data);
            return this;
        }

        /// <summary>
        /// Send action data to the specified address.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="data">byte action value.</param>
        public KnxClientHelper Action(string address, byte data)
        {
            knxClient.Action(address, data);
            return this;
        }
        
        /// <summary>
        /// Send action data to the specified address.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="data">byte array action value.</param>
        public KnxClientHelper Action(string address, byte[] data)
        {
            knxClient.Action(address, data);
            return this;
        }
        
        /// <summary>
        /// Send action data to the specified address.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="data">string action value.</param>
        public KnxClientHelper Action(string address, string data)
        {
            knxClient.Action(address, data);
            return this;
        }

        /// <summary>
        /// Send action data to the specified address.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="data">generic object action value.</param>
        public KnxClientHelper Action(string address, object data)
        {
            knxClient.Action(address, knxClient.toDPT("9001", data));
            return this;
        }

        /// <summary>
        /// Requests the status.
        /// </summary>
        /// <param name="address">Address.</param>
        public KnxClientHelper RequestStatus(string address)
        {
            knxClient.RequestStatus(address);
            return this;
        }

        /// <summary>
        /// Converts to KNX Data Point Type.
        /// </summary>
        /// <returns>KNX Data Point Type byte array.</returns>
        /// <param name="type">Type</param>
        /// <param name="data">Data</param>
        public byte[] ConvertToDpt(string type, object data)
        {
            return knxClient.toDPT(type, data);
        }

        /// <summary>
        /// Converts from KNX Data Point Type.
        /// </summary>
        /// <returns>Converted object from KNX Data Point Type.</returns>
        /// <param name="type">Type.</param>
        /// <param name="data">Data.</param>
        public object ConvertFromDpt(string type, object data)
        {
            object result;
            if (data.GetType() == typeof(String))
            {
                result = knxClient.fromDPT(type, (String)data);
            }
            else
            {
                result = knxClient.fromDPT(type, (byte[])data);
            }
            return result;
        }

        /// <summary>
        /// Sets the function to call when the status of the connection changes.
        /// </summary>
        /// <param name="statusChangeAction">Function or inline delegate.</param>
        public KnxClientHelper OnStatusChanged(Action<bool> statusChangeAction)
        {
            statusChanged = statusChangeAction;
            return this;
        }
                
        /// <summary>
        /// Sets the function to call when a new event is received.
        /// </summary>
        /// <param name="eventAction">Function or inline delegate.</param>
        public KnxClientHelper OnEventReceived(Action<string, string> eventAction)
        {
            eventReceived = eventAction;
            return this;
        }

        /// <summary>
        /// Sets the function to call when a new status is received.
        /// </summary>
        /// <param name="statusAction">Function or inline delegate.</param>
        public KnxClientHelper OnStatusReceived(Action<string, string> statusAction)
        {
            statusReceived = statusAction;
            return this;
        }
        
        public void Reset()
        {
            knxEndPoint = null;
            Disconnect();
        }

        #region Private helpers and event delegates

        private void knxClient_Connected()
        {
            if (statusChanged != null)
            {
                statusChanged(true);
            }
        }

        
        private void knxClient_Disconnected()
        {
            if (statusChanged != null)
            {
                statusChanged(false);
            }
        }

        private void knxClient_EventReceived(string address, string state)
        {
            if (eventReceived != null)
            {
                eventReceived(address, state);
            }
        }
        
        private void knxClient_StatusReceived(string address, string state)
        {
            if (statusReceived != null)
            {
                statusReceived(address, state);
            }
        }

        #endregion

    }
}
