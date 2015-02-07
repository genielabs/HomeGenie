﻿/*
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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.Xml.Serialization;
using System.IO;

namespace ZWaveLib.Devices
{
    public delegate void ControllerEventHandler(object source, ControllerEventArgs e);
    public class ControllerEventArgs : EventArgs
    {
        public readonly byte NodeId;
        public readonly ControllerStatus Status;
        public ControllerEventArgs(byte nodeId, ControllerStatus status)
        {
            this.Status = status;
            this.NodeId = nodeId;
        }
    }

    public enum ControllerStatus : byte
    {
        NodeAdded = 0x00,
        NodeRemoved = 0x01,
        DiscoveryStart = 0xDD,
        NodeUpdated = 0xEE,
        NodeError = 0xFE,
        DiscoveryEnd = 0xFF
    }

    [Serializable]
    public class ZWaveNodeConfig
    {
        public byte NodeId { get; set; }
        public string DeviceHandler { get; set; }
    }

    public class Controller : ZWaveNode
    {
        #region Public fields

        public override event ManufacturerSpecificResponseEventHandler ManufacturerSpecificResponse;

        public Action<object, ControllerEventArgs> ControllerEvent;
        public void OnControllerEvent(ControllerEventArgs e)
        {
            if (ControllerEvent != null)
            {
                ControllerEvent(this, e);
            }
        }


        #endregion Public fields

        #region Private fields

        private List<ZWaveNode> devices = new List<ZWaveNode>();
        private List<ZWaveNodeConfig> nodesConfig = new List<ZWaveNodeConfig>();
        private byte nodeOperationIdCheck = 0;
        private byte currentCommandTargetNode = 0;
        private byte[] lastMessage = null;
        private DateTime lastMessageTimestamp = DateTime.UtcNow;
        private ManualResetEvent nodeCapabilityAck = new ManualResetEvent(false);
        private object nodeCapabilityLock = new object();

        #endregion Private fields

        #region Lifecycle

        public Controller(ZWavePort zwavePort) : base(1, zwavePort)
        {
            zwavePort.ZWaveMessageReceived += new ZWavePort.ZWaveMessageReceivedEvent((object sender, ZWaveMessageReceivedEventArgs args) =>
            {
                try
                {
                    ZwaveMessageReceived(sender, args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ZWaveLib: ERROR in _zwavemessagereceived(...) " + ex.Message + "\n" + ex.StackTrace);
                }
            });
            LoadNodesConfig();
        }

        #endregion Lifecycle

        #region Public members

        public ZWaveNode GetDevice(byte nodeId)
        {
            return devices.Find(zn => zn.NodeId == nodeId);
        }

        public List<ZWaveNode> Devices
        {
            get { return devices; }
        }
        
        public void Discovery()
        {
            OnControllerEvent(new ControllerEventArgs(0x00, ControllerStatus.DiscoveryStart)); // Send event
            zwavePort.Discovery();
        }

        public void SoftReset()
        {
            byte[] message = new byte[] {
                (byte)MessageHeader.SOF, /* Start Of Frame */
                0x04, /* packet length */
                (byte)MessageType.REQUEST, /* Type of message */
                (byte)Function.ControllerSoftReset,
                0xff, /* nodeid */
                0x00
            };
            SendMessage(message, false);
        }

        public void HardReset()
        {
            byte[] message = new byte[] {
                (byte)MessageHeader.SOF, /* Start Of Frame */
                0x04, /* packet length */
                (byte)MessageType.REQUEST, /* Type of message */
                (byte)Function.ControllerSetDefault,
                0xff, /* nodeid */
                0x00
            };
            SendMessage(message, true);
        }

        public byte BeginNodeAdd()
        {
            byte[] header = new byte[] {
                (byte)MessageHeader.SOF, /* Start Of Frame */
                0x05, /*packet len */
                (byte)MessageType.REQUEST, /* Type of message */
                (byte)Function.NodeAdd
            };
            byte[] footer = new byte[] { (byte)NodeFunctionOption.AddNodeAny | 0x80, 0x00, 0x00 };
            byte[] message = new byte[header.Length + footer.Length];

            System.Array.Copy(header, 0, message, 0, header.Length);
            System.Array.Copy(footer, 0, message, message.Length - footer.Length, footer.Length);

            byte callbackId = SendMessage(message);

            return callbackId;
        }

        public byte StopNodeAdd()
        {
            byte[] header = new byte[] {
                (byte)MessageHeader.SOF, /* Start Of Frame */
                0x05 /*packet len */,
                (byte)MessageType.REQUEST, /* Type of message */
                (byte)Function.NodeAdd
            };
            byte[] footer = new byte[] { (byte)NodeFunctionOption.AddNodeStop, 0x00, 0x00 };
            byte[] message = new byte[header.Length + footer.Length];

            System.Array.Copy(header, 0, message, 0, header.Length);
            System.Array.Copy(footer, 0, message, message.Length - footer.Length, footer.Length);

            return SendMessage(message);
        }

        public byte BeginNodeRemove()
        {

            byte[] header = new byte[] {
                (byte)MessageHeader.SOF, /* Start Of Frame */
                0x05 /*packet len */,
                (byte)MessageType.REQUEST, /* Type of message */
                (byte)Function.NodeRemove
            };
            byte[] footer = new byte[] { (byte)NodeFunctionOption.RemoveNodeAny | 0x80, 0x00, 0x00 };
            byte[] message = new byte[header.Length + footer.Length];

            System.Array.Copy(header, 0, message, 0, header.Length);
            System.Array.Copy(footer, 0, message, message.Length - footer.Length, footer.Length);

            return SendMessage(message);
        }

        public byte StopNodeRemove()
        {
            byte[] header = new byte[] {
                (byte)MessageHeader.SOF, /* Start Of Frame */
                0x05 /*packet len */,
                (byte)MessageType.REQUEST, /* Type of message */
                (byte)Function.NodeRemove
            };
            byte[] footer = new byte[] { (byte)NodeFunctionOption.RemoveNodeStop, 0x00, 0x00 };
            byte[] message = new byte[header.Length + footer.Length];

            System.Array.Copy(header, 0, message, 0, header.Length);
            System.Array.Copy(footer, 0, message, message.Length - footer.Length, footer.Length);

            return SendMessage(message);
        }

        public void GetNodeInformationFrame(byte nodeId)
        {
            byte[] message = new byte[] {
                (byte)MessageHeader.SOF, /* Start Of Frame */
                0x04,
                (byte)MessageType.REQUEST, /* Type of message */
                (byte)Function.RequestNodeInfo,
                nodeId,
                0x00
            };
            SendMessage(message, true);
        }

        #endregion Public members

        #region Private members

        private void GetNodeCapabilities(byte nodeId)
        {
            lock (nodeCapabilityLock)
            {
                nodeCapabilityAck.Reset();
                currentCommandTargetNode = nodeId;
                byte[] message = new byte[] {
                    (byte)MessageHeader.SOF, /* Start Of Frame */
                    0x04,
                    (byte)MessageType.REQUEST, /* Type of message */
                    (byte)Function.GetNodeProtocolInfo,
                    nodeId,
                    0x00
                };
                // Wait for response
                int retries = 0;
                do
                {
                    SendMessage(message, true);
                } while (!nodeCapabilityAck.WaitOne(300) && retries++ < 3);
            }
        }

        private void ZwaveMessageReceived(object sender, ZWaveMessageReceivedEventArgs args)
        {
            // discard repeated messages within last 2 seconds time range
            bool repeated = false;
            if (lastMessage != null)
            {
                var elapsed = (DateTime.UtcNow - lastMessageTimestamp);
                if (elapsed.TotalSeconds <= 2 && lastMessage.SequenceEqual(args.Message)) repeated = true;
            }
            lastMessageTimestamp = DateTime.UtcNow;
            lastMessage = new byte[args.Message.Length];
            Buffer.BlockCopy(args.Message, 0, lastMessage, 0, args.Message.Length * sizeof(byte));
            if (repeated)
            {
                zwavePort.SendAck();
                Console.WriteLine("ZWaveLib: repeated message discarded.");
                return;
            }
            //
            int length = args.Message.Length;
            try
            {
                MessageHeader zwaveHeader = (MessageHeader)args.Message[0];
                switch (zwaveHeader)
                {
                case MessageHeader.CAN:
                    zwavePort.SendAck();
                    // RESEND
                    //Console.WriteLine("ZWaveLib: received CAN, resending last message");
                    //zp.ResendLastMessage();
                    break;

                case MessageHeader.ACK:
                    zwavePort.SendAck();
                    break;

                case MessageHeader.SOF: // start of zwave frame
                    //
                    // parse frame headers
                    //
                    //int msgLength = (int)args.Message[1];
                    var msgType = (MessageType)args.Message[2];
                    var cmdClass = (args.Message.Length > 3 ? (Function)args.Message[3] : 0);
                    byte sourceNodeId = 0;
                    byte nodeOperation = 0;
                    //
                    switch (msgType)
                    {
                    case MessageType.REQUEST:
                        zwavePort.SendAck();

                        if (devices.Count == 0) break;

                        switch (cmdClass)
                        {
                        case Function.None:
                            break;

                        case Function.NodeAdd:

                            nodeOperation = args.Message[5];
                            if (nodeOperation == (byte)NodeFunctionStatus.AddNodeAddingSlave)
                            {
                                //Console.WriteLine("\n\nADDING NODE SLAVE {0}\n     ->   ", zp.ByteArrayToString(args.Message));
                                nodeOperationIdCheck = args.Message[6];
                                var newNode = CreateDevice(nodeOperationIdCheck, 0x00);
                                // Extract node information frame
                                int nodeInfoLength = (int)args.Message[7];
                                byte[] nodeInfo = new byte[nodeInfoLength - 2];
                                Array.Copy(args.Message, 8, nodeInfo, 0, nodeInfoLength - 2);
                                RaiseUpdateParameterEvent(
                                    newNode,
                                    0,
                                    ParameterType.NODE_INFO,
                                    zwavePort.ByteArrayToString(nodeInfo)
                                );
                            }
                            else if (nodeOperation == (byte)NodeFunctionStatus.AddNodeProtocolDone /* || nodeOperation == (byte)NodeFunctionStatus.AddNodeDone */)
                            {
                                if (nodeOperationIdCheck == args.Message[6])
                                {
                                    //Console.WriteLine("\n\nADDING NODE DONE {0} {1}\n\n", args.Message[6], callbackid);
                                    Thread.Sleep(500);
                                    GetNodeCapabilities(args.Message[6]);
                                    var newNode = devices.Find(n => n.NodeId == args.Message[6]);
                                    if (newNode != null) newNode.ManufacturerSpecific_Get();
                                }
                                OnControllerEvent(new ControllerEventArgs(
                                    0x00,
                                    ControllerStatus.DiscoveryEnd
                                )); // Send event
                            }
                            else if (nodeOperation == (byte)NodeFunctionStatus.AddNodeFailed)
                            {
                                //Console.WriteLine("\n\nADDING NODE FAIL {0}\n\n", args.Message[6]);
                            }
                            break;

                        case Function.NodeRemove:

                            nodeOperation = args.Message[5];
                            if (nodeOperation == (byte)NodeFunctionStatus.RemoveNodeRemovingSlave)
                            {
                                //Console.WriteLine("\n\nREMOVING NODE SLAVE {0}\n\n", args.Message[6]);
                                nodeOperationIdCheck = args.Message[6];
                            }
                            else if (nodeOperation == (byte)NodeFunctionStatus.RemoveNodeDone)
                            {
                                if (nodeOperationIdCheck == args.Message[6])
                                {
                                    //Console.WriteLine("\n\nREMOVING NODE DONE {0} {1}\n\n", args.Message[6], callbackid);
                                    RemoveDevice(args.Message[6]);
                                }
                                OnControllerEvent(new ControllerEventArgs(
                                    0x00,
                                    ControllerStatus.DiscoveryEnd
                                )); // Send event
                            }
                            else if (nodeOperation == (byte)NodeFunctionStatus.RemoveNodeFailed)
                            {
                                //Console.WriteLine("\n\nREMOVING NODE FAIL {0}\n\n", args.Message[6]);
                            }
                            break;

                        case Function.ApplicationCommand:

                            sourceNodeId = args.Message[5];
                            var node = devices.Find(n => n.NodeId == sourceNodeId);
                            if (node == null)
                            {
                                CreateDevice(sourceNodeId, 0x00);
                                GetNodeCapabilities(sourceNodeId);
                            }
                            try
                            {
                                node.MessageRequestHandler(args.Message);
                            }
                            catch(Exception ex)
                            {
                                Console.WriteLine("# " + ex.Message + "\n" + ex.StackTrace);
                            }
                            break;

                        case Function.SendData:

                            byte commandId = args.Message[4];
                            if (commandId == 0x01) // SEND DATA OK
                            {
                                // TODO: ... what does that mean?
                            }
                            else if (args.Message[5] == 0x00)
                            {
                                // Messaging complete, remove callbackid
                                zwavePort.PendingMessages.RemoveAll(zm => zm.CallbackId == commandId);
                            }
                            else if (args.Message[5] == 0x01)
                            {
                                byte nodeID = zwavePort.ResendLastMessage(commandId) ;
                                if( nodeID != 0 )
                                {
                                    // Resend timed out
                                    OnControllerEvent(new ControllerEventArgs(nodeID, ControllerStatus.NodeError));
                                }
                            }
                            break;

                        case Function.NodeUpdateInfo:

                            sourceNodeId = args.Message[5];
                            int nifLength = (int)args.Message[6];
                            var znode = devices.Find(n => n.NodeId == sourceNodeId);
                            if (znode != null)
                            {
                                byte[] nodeInfo = new byte[nifLength - 2];
                                //Console.WriteLine(ByteArrayToString(args.Message));
                                Array.Copy(args.Message, 7, nodeInfo, 0, nifLength - 2);
                                //
                                RaiseUpdateParameterEvent(
                                    znode,
                                    0,
                                    ParameterType.NODE_INFO,
                                    zwavePort.ByteArrayToString(nodeInfo)
                                );
                                RaiseUpdateParameterEvent(
                                    znode,
                                    0,
                                    ParameterType.WAKEUP_NOTIFY,
                                    "1"
                                );
                            }
                            break;

                        default:
                            Console.WriteLine("\nUNHANDLED Z-Wave REQUEST\n     " + zwavePort.ByteArrayToString(args.Message) + "\n");
                            break;

                        }

                        break;

                    case MessageType.RESPONSE:

                        switch (cmdClass)
                        {
                        case Function.DiscoveryNodes:
                            MessageResponseNodeBitMaskHandler(args.Message);
                            break;
                        case Function.GetNodeProtocolInfo:
                            MessageResponseNodeCapabilityHandler(args.Message);
                            break;
                        case Function.RequestNodeInfo:
                            // TODO: shall we do something here?
                            break;
                        case Function.SendData:
                            // TODO: shall we do something here?
                            break;
                        default:
                            Console.WriteLine("\nUNHANDLED Z-Wave RESPONSE\n     " + zwavePort.ByteArrayToString(args.Message) + "\n");
                            break;
                        }

                        break;

                    default:
                        Console.WriteLine("\nUNHANDLED Z-Wave message TYPE\n     " + zwavePort.ByteArrayToString(args.Message) + "\n");
                        break;
                    }

                    break;
                }

            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);

            }
        }

        private void MessageResponseNodeBitMaskHandler(byte[] receivedMessage)
        {
            int length = receivedMessage.Length;
            if (length > 3)
            {
                // Is this a discovery response?
                if (receivedMessage[3] == 0x02)
                {
                    CreateDevices(receivedMessage);
                }
            }
        }

        private void MessageResponseNodeCapabilityHandler(byte[] receivedMessage)
        {
            int length = receivedMessage.Length;
            if (length > 8)
            {
                try
                {
                    var node = devices.Find(n => n.NodeId == currentCommandTargetNode);
                    // TODO: node == null should not happen, deprecate this if block
                    if (node == null)
                    {
                        //Console.WriteLine("Z-Wave Adding node " + currentCommandTargetNode + " Class[ Basic=" + receivedMessage[7].ToString("X2") + " Generic=" + ((GenericType)receivedMessage[8]).ToString() + " Specific=" + receivedMessage[9].ToString("X2") + " ]");
                        node = CreateDevice(currentCommandTargetNode, receivedMessage[8]);
                        devices.Add(node);
                    }
                    //
                    node.BasicClass = receivedMessage[7];
                    node.GenericClass = receivedMessage[8];
                    node.SpecificClass = receivedMessage[9];
                    //
                    SetDeviceHandler(node);
                    //
                    //if (node.NodeId != 1)
                    //{
                        //Console.WriteLine("Z-Wave Updating node " + node.NodeId + " Class[ Basic=" + receivedMessage[7].ToString("X2") + " Generic=" + ((GenericType)receivedMessage[8]).ToString() + " Specific=" + receivedMessage[9].ToString("X2") + " ]");
                    //}
                }
                catch (Exception e)
                {
                    Console.WriteLine("Z-Wave ERROR adding node : " + e.Message + "\n" + e.StackTrace);
                    //System.Diagnostics.Debugger.Break();
                }
                nodeCapabilityAck.Set();
            }
        }

        private void CreateDevices(byte[] receivedMessage)
        {
            var nodeList = ExtractNodesFromBitMask(receivedMessage);
            foreach (byte i in nodeList)
            {
                var node = devices.Find(n => n.NodeId == i);
                if (node == null)
                {
                    //Console.WriteLine("Z-Wave Adding node " + i + " Class[ Basic=" + receivedMessage[7].ToString("X2") + " Generic=" + ((GenericType)receivedMessage[8]).ToString() + " Specific=" + receivedMessage[9].ToString("X2") + " ]");
                    devices.Add(CreateDevice(i, 0x00));
                }
                else
                {
                    OnControllerEvent(new ControllerEventArgs(i, ControllerStatus.NodeUpdated)); // Send event
                }
            }
            while (nodeList.Count > 0)
            {
                ZWaveNode nextNode = devices.Find(zn => zn.BasicClass == 0x00);
                if (nextNode != null)
                {
                    GetNodeCapabilities(nextNode.NodeId);
                }
                else
                {
                    OnControllerEvent(new ControllerEventArgs(0x00, ControllerStatus.DiscoveryEnd)); // Send event
                    break;
                }
            }
        }

        private List<byte> ExtractNodesFromBitMask(byte[] receivedMessage)
        {
            var nodeList = new List<byte>();
            // Decode the nodes in the bitmask (byte 9 - 37)
            byte k = 1;
            for (int i = 7; i < 36; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    try
                    {
                        if ((receivedMessage[i] & ((byte)Math.Pow(2, j))) == ((byte)Math.Pow(2, j)))
                        {
                            //Console.WriteLine(this.GetType().Name.ToString() + " Node id: " + k + " discovered");
                            nodeList.Add(k);
                        }
                    }
                    catch
                    {

                        System.Diagnostics.Debugger.Break();
                    }
                    k++;
                }
            }
            return nodeList;
        }

        private void RemoveDevice(byte nodeId)
        {
            var node = devices.Find(n => n.NodeId == nodeId);
            if (node != null)
            {
                node.UpdateNodeParameter -= znode_UpdateNodeParameter;
                node.ManufacturerSpecificResponse -= znode_ManufacturerSpecificResponse;
            }
            devices.RemoveAll(zn => zn.NodeId == nodeId);
            OnControllerEvent(new ControllerEventArgs(nodeId, ControllerStatus.NodeRemoved)); // Send event
        }

        private ZWaveNode CreateDevice(byte nodeId, byte genericClass)
        {
            string className = "ZWaveLib.Devices.";
            switch (genericClass)
            {
            case (byte)GenericType.StaticController:
                // TODO: this is very untested...
                className += "Controller";
                break;
            default: // generic node
                className += "ZWaveNode";
                break;
            }
            var znode = (ZWaveNode)Activator.CreateInstance(
                Type.GetType(className),
                new object[] {
                    nodeId,
                    zwavePort,
                    genericClass
                }
            );
            znode.UpdateNodeParameter += znode_UpdateNodeParameter;
            znode.ManufacturerSpecificResponse += znode_ManufacturerSpecificResponse;
            //
            OnControllerEvent(new ControllerEventArgs(nodeId, ControllerStatus.NodeAdded)); // Send event
            //
            return znode;
        }
        
        private List<Type> GetTypesInNamespace(Assembly assembly, string nameSpace)
        {
            var typeList = new List<Type>();
            foreach (Type type in assembly.GetTypes())
            {
                if (type.Namespace != null && type.Namespace.StartsWith(nameSpace)) typeList.Add(type);
            }
            //return assembly.GetTypes().Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToArray();
            return typeList;
        }

        private void CheckDeviceHandler(ZWaveNode node, ManufacturerSpecific manufacturerspecs)
        {
            //if (node.DeviceHandler == null)
            {
                var typeList = GetTypesInNamespace(
                    Assembly.GetExecutingAssembly(),
                    "ZWaveLib.Devices.ProductHandlers."
                    );
                for (int i = 0; i < typeList.Count; i++)
                {
                    //Console.WriteLine(typelist[i].FullName);
                    Type type = Assembly.GetExecutingAssembly().GetType(typeList[i].FullName); // full name - i.e. with namespace (perhaps concatenate)
                    try
                    {
                        IZWaveDeviceHandler deviceHandler = (IZWaveDeviceHandler)Activator.CreateInstance(type);
                        //
                        if (deviceHandler.CanHandleProduct(manufacturerspecs))
                        {
                            node.DeviceHandler = deviceHandler;
                            node.DeviceHandler.SetNodeHost(node);
                            SaveNodesConfig();
                            break;
                        }
                    }
                    catch
                    {
                        // TODO: add error logging 
                        //Console.WriteLine("ERROR!!!!!!! " + ex.Message + " : " + ex.StackTrace);
                    }
                }
            }

        }
                
        public void SetDeviceHandler(ZWaveNode node)
        {
            // Search handler in nodesConfig first
            for (int n = 0; n < nodesConfig.Count; n++)
            {
                var config = nodesConfig[n];
                if (config.NodeId == node.NodeId && config.DeviceHandler != null && !config.DeviceHandler.Contains(".Generic."))
                {
                    // set to last known handler
                    SetDeviceHandlerFromName(node, config.DeviceHandler);
                    break;
                }
            }
            // If no specific devicehandler could be found, then set a generic handler
            if (node.DeviceHandler == null)
            {
                IZWaveDeviceHandler deviceHandler = null;
                switch (node.GenericClass)
                {
                case 0x00:
                    // need to query node capabilities
                    //GetNodeCapabilities(node.NodeId);
                    break;
                case (byte)ZWaveLib.GenericType.SwitchBinary:
                    deviceHandler = new ProductHandlers.Generic.Switch();
                    break;
                case (byte)ZWaveLib.GenericType.SwitchMultilevel: // eg. dimmer
                    deviceHandler = new ProductHandlers.Generic.Dimmer();
                    break;
                case (byte)ZWaveLib.GenericType.Thermostat:
                    deviceHandler = new ProductHandlers.Generic.Thermostat();
                    break;
                    // Fallback to generic Sensor driver if type is not directly supported.
                    // The Generic.Sensor handler is currently used as some kind of multi-purpose driver 
                default:
                    deviceHandler = new ProductHandlers.Generic.Sensor();
                    break;
                }
                if (deviceHandler != null)
                {
                    node.DeviceHandler = deviceHandler;
                    node.DeviceHandler.SetNodeHost(node);
                }
            }
        }

        public void SetDeviceHandlerFromName(ZWaveNode node, string fullName)
        {
            var type = Assembly.GetExecutingAssembly().GetType(fullName); // full name - i.e. with namespace (perhaps concatenate)
            try
            {
                var deviceHandler = (IZWaveDeviceHandler)Activator.CreateInstance(type);
                node.DeviceHandler = deviceHandler;
                node.DeviceHandler.SetNodeHost(node);
            }
            catch
            {
                // TODO: add error logging 
            }
        }

        private void LoadNodesConfig()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "zwavenodes.xml");
            try
            {
                var serializer = new XmlSerializer(nodesConfig.GetType());
                var reader = new StreamReader(configPath);
                nodesConfig = (List<ZWaveNodeConfig>)serializer.Deserialize(reader);
                reader.Close();
            } catch {
                // TODO: report/handle exception
            }
        }

        private void SaveNodesConfig()
        {
            nodesConfig.Clear();
            for (int n = 0; n < devices.Count; n++)
            {
                nodesConfig.Add(new ZWaveNodeConfig() {
                    NodeId = devices[n].NodeId,
                    DeviceHandler = devices[n].DeviceHandler.GetType().FullName
                });
            }
            // TODO: save config to xml
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "zwavenodes.xml");
            try
            {
                var settings = new System.Xml.XmlWriterSettings();
                settings.Indent = true;
                var serializer = new System.Xml.Serialization.XmlSerializer(nodesConfig.GetType());
                var writer = System.Xml.XmlWriter.Create(configPath, settings);
                serializer.Serialize(writer, nodesConfig);
                writer.Close();
            } catch {
                // TODO: report/handle exception
            }
        }

        #region Events Handling

        private void znode_ManufacturerSpecificResponse(object sender, ManufacturerSpecificResponseEventArg mfargs)
        {
            CheckDeviceHandler((ZWaveNode)sender, mfargs.ManufacturerSpecific);
            RaiseUpdateParameterEvent(
                (ZWaveNode)sender,
                0,
                ParameterType.MANUFACTURER_SPECIFIC,
                mfargs.ManufacturerSpecific
            );
            // Route event to other listeners
            if (this.ManufacturerSpecificResponse != null)
            {
                ManufacturerSpecificResponse(sender, mfargs);
            }
        }

        private void znode_UpdateNodeParameter(object sender, UpdateNodeParameterEventArgs upargs)
        {
            RaiseUpdateParameterEvent((ZWaveNode)sender, upargs.ParameterId, upargs.ParameterType, upargs.Value);
        }

        #endregion

        #endregion Private members
    }
}
