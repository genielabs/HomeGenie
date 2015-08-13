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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.Xml.Serialization;
using System.IO;
using ZWaveLib.CommandClasses;
using ZWaveLib.Values;

namespace ZWaveLib
{

    public class Controller : ZWaveNode
    {
        
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

        #region Public fields

        public override event ManufacturerSpecificResponseEventHandler ManufacturerSpecificResponse;

        public Action<object, ControllerEventArgs> ControllerEvent;

        #endregion Public fields

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
                    Utility.DebugLog(DebugMessageType.Error, "Exception occurred in _zwavemessagereceived(...) " + ex.Message + "\n" + ex.StackTrace);
                }
            });
            LoadNodesConfig();
        }

        #endregion Lifecycle

        #region Public members

        #region Controller

        public void SoftReset()
        {
            byte[] message = new byte[] {
                (byte)MessageHeader.SOF, /* Start Of Frame */
                0x04, /* packet length */
                (byte)MessageType.Request, /* Type of message */
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
                (byte)MessageType.Request, /* Type of message */
                (byte)Function.ControllerSetDefault,
                0xff, /* nodeid */
                0x00
            };
            SendMessage(message, true);
        }

        public void OnControllerEvent(ControllerEventArgs e)
        {
            if (ControllerEvent != null)
            {
                ControllerEvent(this, e);
            }
        }

        #endregion

        #region ZWave Discovery

        public void Discovery()
        {
            OnControllerEvent(new ControllerEventArgs(0x00, ControllerStatus.DiscoveryStart));
            zwavePort.Discovery();
        }

        public void GetNodeInformationFrame(byte nodeId)
        {
            byte[] message = new byte[] {
                (byte)MessageHeader.SOF, /* Start Of Frame */
                0x04,
                (byte)MessageType.Request, /* Type of message */
                (byte)Function.RequestNodeInfo,
                nodeId,
                0x00
            };
            SendMessage(message, true);
        }

        public ZWaveNode GetDevice(byte nodeId)
        {
            return devices.Find(zn => zn.Id == nodeId);
        }

        public List<ZWaveNode> Devices
        {
            get { return devices; }
        }

        #endregion

        #region Node Neighbors

        public void RequestNeighborUpdateOptions(byte nodeId)
        {
            OnControllerEvent(new ControllerEventArgs(nodeId, ControllerStatus.NeighborUpdateStarted));
            var node = devices.Find(n => n.Id == nodeId);
            node.SendMessage(new byte[] {
                (byte)MessageHeader.SOF,
                0x06, /* packet length */
                (byte)MessageType.Request, /* Type of message */
                (byte)Function.RequestNodeNeighborUpdateOptions,
                node.Id,
                0x25,
                0x00,
                0x00
            });
        }

        public void RequestNeighborUpdate(ZWaveNode node)
        {
            node.SendMessage(new byte[] {
                (byte)MessageHeader.SOF,
                0x05, /* packet length */
                (byte)MessageType.Request, /* Type of message */
                (byte)Function.RequestNodeNeighborUpdate,
                node.Id,
                0x00,
                0x00    
            });
        }

        public void NeighborsGetRoutingInfo(ZWaveNode node)
        {
            node.SendMessage(new byte[] {
                (byte)MessageHeader.SOF,
                0x07, /* packet length */
                (byte)MessageType.Request, /* Type of message */
                (byte)Function.GetRoutingInfo,
                node.Id,
                0x00,
                0x00,
                0x03,
                0x00    
            });
        }

        #endregion

        #region Node Add/Remove

        public byte BeginNodeAdd()
        {
            byte[] header = new byte[] {
                (byte)MessageHeader.SOF, /* Start Of Frame */
                0x05, /*packet len */
                (byte)MessageType.Request, /* Type of message */
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
                (byte)MessageType.Request, /* Type of message */
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
                (byte)MessageType.Request, /* Type of message */
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
                (byte)MessageType.Request, /* Type of message */
                (byte)Function.NodeRemove
            };
            byte[] footer = new byte[] { (byte)NodeFunctionOption.RemoveNodeStop, 0x00, 0x00 };
            byte[] message = new byte[header.Length + footer.Length];

            System.Array.Copy(header, 0, message, 0, header.Length);
            System.Array.Copy(footer, 0, message, message.Length - footer.Length, footer.Length);

            return SendMessage(message);
        }

        #endregion

        #endregion

        #region Private members

        #region ZWave Discovery

        private void CreateDevices(byte[] receivedMessage)
        {
            var nodeList = ExtractNodesFromBitMask(receivedMessage);
            foreach (byte i in nodeList)
            {
                var node = devices.Find(n => n.Id == i);
                if (node == null)
                {
                    devices.Add(CreateDevice(i, 0x00));
                }
                else
                {
                    OnControllerEvent(new ControllerEventArgs(i, ControllerStatus.NodeUpdated));
                }
            }
            while (nodeList.Count > 0)
            {
                ZWaveNode nextNode = devices.Find(zn => zn.BasicClass == 0x00);
                if (nextNode != null)
                {
                    GetNodeCapabilities(nextNode.Id);
                }
                else
                {
                    OnControllerEvent(new ControllerEventArgs(0x00, ControllerStatus.DiscoveryEnd));
                    break;
                }
            }
        }

        private ZWaveNode CreateDevice(byte nodeId, byte genericClass)
        {
            ZWaveNode node;
            switch (genericClass)
            {
            case (byte) GenericType.StaticController:
                    // TODO: this is very untested...
                node = (ZWaveNode)new Controller(zwavePort);
                break;
            default: // generic node
                node = new ZWaveNode(nodeId, zwavePort, genericClass);
                break;
            }
            node.ParameterChanged += znode_ParameterChanged;
            node.ManufacturerSpecificResponse += znode_ManufacturerSpecificResponse;
            //
            OnControllerEvent(new ControllerEventArgs(nodeId, ControllerStatus.NodeAdded));
            //
            return node;
        }

        private void RemoveDevice(byte nodeId)
        {
            var node = devices.Find(n => n.Id == nodeId);
            if (node != null)
            {
                node.ParameterChanged -= znode_ParameterChanged;
                node.ManufacturerSpecificResponse -= znode_ManufacturerSpecificResponse;
            }
            devices.RemoveAll(zn => zn.Id == nodeId);
            OnControllerEvent(new ControllerEventArgs(nodeId, ControllerStatus.NodeRemoved));
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

        private void GetNodeCapabilities(byte nodeId)
        {
            lock (nodeCapabilityLock)
            {
                nodeCapabilityAck.Reset();
                currentCommandTargetNode = nodeId;
                byte[] message = new byte[] {
                    (byte)MessageHeader.SOF, /* Start Of Frame */
                    0x04,
                    (byte)MessageType.Request, /* Type of message */
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

        private void MessageResponseNodeCapabilityHandler(byte[] receivedMessage)
        {
            int length = receivedMessage.Length;
            if (length > 8)
            {
                try
                {
                    var node = devices.Find(n => n.Id == currentCommandTargetNode);
                    // TODO: node == null should not happen, deprecate this "if" block
                    if (node == null)
                    {
                        node = CreateDevice(currentCommandTargetNode, receivedMessage[8]);
                        devices.Add(node);
                    }
                    node.BasicClass = receivedMessage[7];
                    node.GenericClass = receivedMessage[8];
                    node.SpecificClass = receivedMessage[9];
                }
                catch (Exception e)
                {
                    Utility.DebugLog(DebugMessageType.Error, "Exception occurred while adding node: " + e.Message + "\n" + e.StackTrace);
                }
                nodeCapabilityAck.Set();
            }
        }

        private void NodeInformationFrameComplete(ZWaveNode znode)
        {
            // once we get the security command classes we'll issue the same events and call SaveNodesConfig();
            RaiseUpdateParameterEvent(new ZWaveEvent(znode, EventParameter.NodeInfo, Utility.ByteArrayToString(znode.NodeInformationFrame), 0));
            RaiseUpdateParameterEvent(new ZWaveEvent(znode, EventParameter.WakeUpNotify, "1", 0));
            SaveNodesConfig();
        }

        #endregion

        #region ZWaveNode list persistence

        private void LoadNodesConfig()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "zwavenodes.xml");
            try
            {
                var serializer = new XmlSerializer(nodesConfig.GetType());
                var reader = new StreamReader(configPath);
                nodesConfig = (List<ZWaveNodeConfig>)serializer.Deserialize(reader);
                foreach (ZWaveNodeConfig node in nodesConfig)
                {
                    var newNode = CreateDevice(node.NodeId, 0x00);
                    newNode.NodeInformationFrame = node.NodeInformationFrame;
                    newNode.SecuredNodeInformationFrame = node.SecuredNodeInformationFrame;
                    Security.GetSecurityData(newNode).SetPrivateNetworkKey(node.DevicePrivateNetworkKey);
                    devices.Add(newNode);
                }
                reader.Close();
            }
            catch
            {
                // TODO: report/handle exception
            }
        }

        public void SaveNodesConfig()
        {
            nodesConfig.Clear();
            for (int n = 0; n < devices.Count; n++)
            {
                // save only the nodes that are still in the network - not sure how is the best way to handle this
                // we just want to save the vlid nodes, not all the nodes that ever existed and were not cleanly removed
                if (devices[n].SpecificClass > 0)
                {
                    nodesConfig.Add(new ZWaveNodeConfig() {
                        NodeId = devices[n].Id,
                        NodeInformationFrame = devices[n].NodeInformationFrame,
                        SecuredNodeInformationFrame = devices[n].SecuredNodeInformationFrame,
                        DevicePrivateNetworkKey = Security.GetSecurityData(devices[n]).GetPrivateNetworkKey()
                    });
                }
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
            }
            catch
            {
                // TODO: report/handle exception
            }
        }

        #endregion

        #region ZWavePort events

        private void ZwaveMessageReceived(object sender, ZWaveMessageReceivedEventArgs args)
        {
            // discard repeated messages within last 2 seconds time range
            bool repeated = false;
            if (lastMessage != null)
            {
                var elapsed = (DateTime.UtcNow - lastMessageTimestamp);
                if (elapsed.TotalSeconds <= 2 && lastMessage.SequenceEqual(args.Message))
                {
                    //Utility.DebugLog(DebugMessageType.Information, " lastMessage: " + Utility.ByteArrayToString(lastMessage));
                    //Utility.DebugLog(DebugMessageType.Information, "args.Message: " + Utility.ByteArrayToString(args.Message));
                    repeated = true;
                }
            }
            lastMessageTimestamp = DateTime.UtcNow;
            lastMessage = new byte[args.Message.Length];
            //Utility.DebugLog(DebugMessageType.Information, " lastMessage2: " + Utility.ByteArrayToString(lastMessage));
            //Utility.DebugLog(DebugMessageType.Information, "args.Message2: " + Utility.ByteArrayToString(args.Message));
            Buffer.BlockCopy(args.Message, 0, lastMessage, 0, args.Message.Length * sizeof(byte));
            if (repeated)
            {
                Utility.DebugLog(DebugMessageType.Warning, "Repeated message discarded.");
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
                    // RESEND ?!?!
                    break;

                case MessageHeader.ACK:
                    break;

                case MessageHeader.SOF: // start of zwave frame
                    var messageType = MessageType.None;
                    Enum.TryParse(args.Message[2].ToString(), out messageType);
                    var functionType = Function.None;
                    Enum.TryParse(args.Message[3].ToString(), out functionType);

                    switch (messageType)
                    {

                    case MessageType.Request:

                        if (devices.Count == 0)
                            break;

                        switch (functionType)
                        {

                        case Function.None:
                            break;

                        case Function.NodeAdd:
                            
                            var nodeAddStatus = NodeFunctionStatus.None;
                            Enum.TryParse(args.Message[5].ToString(), out nodeAddStatus);
                            switch (nodeAddStatus)
                            {

                            case NodeFunctionStatus.AddNodeAddingSlave:
                                
                                nodeOperationIdCheck = args.Message[6];
                                var newNode = CreateDevice(nodeOperationIdCheck, 0x00);
                                // Extract node information frame
                                int nodeInfoLength = (int)args.Message[7];
                                // we don't need to exclude the last 2 CommandClasses
                                byte[] nodeInfo = new byte[nodeInfoLength];
                                Array.Copy(args.Message, 8, nodeInfo, 0, nodeInfoLength);
                                newNode.NodeInformationFrame = nodeInfo;

                                newNode.BasicClass = args.Message[8];
                                newNode.GenericClass = args.Message[9];
                                newNode.SpecificClass = args.Message[10];
                                devices.Add(newNode);

                                if (newNode.SupportCommandClass(CommandClass.Security))
                                {
                                    var nodeSecurityData = Security.GetSecurityData(newNode);
                                    nodeSecurityData.IsAddingNode = true;

                                    Security.GetScheme(newNode);
                                }
                                else
                                {
                                    NodeInformationFrameComplete(newNode);
                                }
                                break;

                            //case NodeFunctionStatus.AddNodeDone:
                            case NodeFunctionStatus.AddNodeProtocolDone:

                                if (nodeOperationIdCheck == args.Message[6])
                                {
                                    Thread.Sleep(500);
                                    GetNodeCapabilities(args.Message[6]);
                                    var addedNode = devices.Find(n => n.Id == args.Message[6]);
                                    if (addedNode != null)
                                        ManufacturerSpecific.Get(addedNode);
                                }
                                OnControllerEvent(new ControllerEventArgs(0x00, ControllerStatus.NodeAdded));
                                // force refresh of nodelist sending DiscoverEnd event
                                // TODO: deprecate this and update the Web UI to refresh modules list on NodeAdded event
                                OnControllerEvent(new ControllerEventArgs(0x00, ControllerStatus.DiscoveryEnd));
                                break;

                            case NodeFunctionStatus.AddNodeFailed:

                                Utility.DebugLog(DebugMessageType.Warning, "ADDING NODE FAILED (" + args.Message[6] + ")");
                                break;

                            }
                            break;

                        case Function.NodeRemove:

                            var nodeRemoveStatus = NodeFunctionStatus.None;
                            Enum.TryParse(args.Message[5].ToString(), out nodeRemoveStatus);
                            switch (nodeRemoveStatus)
                            {

                            case NodeFunctionStatus.RemoveNodeRemovingSlave:

                                nodeOperationIdCheck = args.Message[6];
                                break;
                            
                            case NodeFunctionStatus.RemoveNodeDone:

                                if (nodeOperationIdCheck == args.Message[6])
                                    RemoveDevice(nodeOperationIdCheck);
                                OnControllerEvent(new ControllerEventArgs(0x00, ControllerStatus.NodeRemoved));
                                // force refresh of nodelist sending DiscoverEnd event
                                // TODO: deprecate this and update the Web UI to refresh modules list on NodeRemoved event
                                OnControllerEvent(new ControllerEventArgs(0x00, ControllerStatus.DiscoveryEnd));
                                break;

                            case NodeFunctionStatus.RemoveNodeFailed:

                                OnControllerEvent(new ControllerEventArgs(0x00, ControllerStatus.NodeError));
                                Utility.DebugLog(DebugMessageType.Warning, "REMOVING NODE FAILED (" + args.Message[6] + ")");
                                break;

                            }
                            break;

                        case Function.ApplicationCommandHandler:

                            var node = devices.Find(n => n.Id == args.Message[5]);
                            if (node != null)
                            {
                                try
                                {
                                    node.ApplicationCommandHandler(args.Message);
                                }
                                catch (Exception ex)
                                {
                                    Utility.DebugLog(DebugMessageType.Error, "Exception occurred in node.ApplicationCommandHandler: " + ex.Message + "\n" + ex.StackTrace);
                                }
                            }
                            else
                            {
                                Utility.DebugLog(DebugMessageType.Error, "Unknown node id " + args.Message[5]);
                            }
                            break;

                        case Function.SendData:

                            byte commandId = args.Message[4];
                            byte commandType = args.Message[5];
                            if (commandId == 0x01) // SEND DATA OK
                            {
                                // TODO: ... is there anything to be done here?
                            }
                            else if (commandType == 0x00) // CALLBACK ACK 
                            {
                                // Messaging complete, remove callbackid
                                zwavePort.NodeRequestAck(commandId);
                            }
                            else if (commandType == 0x01) // CALLBACK NACK (Node did not respond)
                            {
                                var pendingMessage = zwavePort.NodeRequestNack(commandId);
                                if (pendingMessage != null && pendingMessage.ResendCount >= ZWaveMessage.ResendMaxAttempts)
                                {
                                    // Resend timed out
                                    OnControllerEvent(new ControllerEventArgs(pendingMessage.Node.Id, ControllerStatus.NodeError));
                                    // Check if node supports WakeUp class, and add message to wake up message queue
                                    if (pendingMessage != null)
                                    {
                                        var sleepingNode = pendingMessage.Node;
                                        if (sleepingNode != null && sleepingNode.SupportCommandClass(CommandClass.WakeUp))
                                        {
                                            WakeUp.ResendOnWakeUp(sleepingNode, pendingMessage.Message);
                                        }
                                    }
                                }
                            }
                            else if (commandType == 0x21)
                            {
                                // Neighbour Update Options STARTED
                                //var message = zwavePort.GetPendingMessage(commandId);
                                // TODO: don't know what to do here...
                            }
                            else if (commandType == 0x22)
                            {
                                // Neighbour Update Options COMPLETE
                                var message = zwavePort.GetPendingMessage(commandId);
                                if (message != null)
                                {
                                    RequestNeighborUpdate(message.Node);
                                    // send ack so the message is removed from the pending message list
                                    zwavePort.NodeRequestAck(commandId);
                                }
                            }
                            break;

                        case Function.ApplicationUpdate:

                            int nifLength = (int)args.Message[6];
                            var znode = devices.Find(n => n.Id == args.Message[5]);
                            if (znode != null)
                            {
                                // we don't need to exclude the last 2 CommandClasses
                                byte[] nodeInfo = new byte[nifLength];
                                Array.Copy(args.Message, 7, nodeInfo, 0, nifLength);
                                znode.NodeInformationFrame = nodeInfo;
                                if (znode.SupportCommandClass(CommandClass.Security))
                                {
                                    // ask the node what security command classes are supported
                                    Security.GetSupported(znode);
                                }
                                else
                                {
                                    NodeInformationFrameComplete(znode);
                                }
                            }
                            break;

                        case Function.RequestNodeNeighborUpdateOptions:
                        case Function.RequestNodeNeighborUpdate:

                            var neighborUpdateStatus = NeighborUpdateStatus.None;
                            Enum.TryParse(args.Message[5].ToString(), out neighborUpdateStatus);
                            var pm = zwavePort.GetPendingMessage(args.Message[4]);
                            switch (neighborUpdateStatus)
                            {

                            case NeighborUpdateStatus.NeighborUpdateStared:
                                
                                OnControllerEvent(new ControllerEventArgs(pm.Node.Id, ControllerStatus.NeighborUpdateStarted));
                                break;

                            case NeighborUpdateStatus.NeighborUpdateDone:
                                
                                nodeOperationIdCheck = pm.Node.Id;
                                OnControllerEvent(new ControllerEventArgs(nodeOperationIdCheck, ControllerStatus.NeighborUpdateDone));
                                NeighborsGetRoutingInfo(pm.Node);
                                if (pm != null) zwavePort.NodeRequestAck(pm.CallbackId);
                                break;

                            case NeighborUpdateStatus.NeighborUpdateFailed:
                                
                                OnControllerEvent(new ControllerEventArgs(pm.Node.Id, ControllerStatus.NeighborUpdateFailed));
                                if (pm != null) zwavePort.NodeRequestNack(pm.CallbackId);
                                break;

                            default:
                                Utility.DebugLog(DebugMessageType.Warning, "Unhandled Node Neighbor Update REQUEST " + Utility.ByteArrayToString(args.Message));
                                break;

                            }
                            break;

                        default:
                            Utility.DebugLog(DebugMessageType.Warning, "Unhandled REQUEST " + Utility.ByteArrayToString(args.Message));
                            break;

                        }

                        break;

                    case MessageType.Response:

                        switch (functionType)
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

                        case Function.GetRoutingInfo:
                            string nodeRouting = "";
                            for (int by = 0; by < 29; by++)
                            {
                                for (int bi = 0; bi < 8; bi++)
                                {
                                    int result = args.Message[4 + by] & (0x01 << bi);
                                    if (result > 0)
                                    {
                                        int nodeRoute = (by << 3) + bi + 1;
                                        nodeRouting += nodeRoute.ToString() + " ";
                                    }
                                }
                            }
                            if (String.IsNullOrWhiteSpace(nodeRouting))
                            {
                                Utility.DebugLog(DebugMessageType.Warning, "No routing nodes reported.");
                            }
                            else
                            {
                                nodeRouting = nodeRouting.TrimEnd();
                                var routedNode = devices.Find(n => n.Id == nodeOperationIdCheck);
                                if (routedNode != null)
                                {
                                    routedNode.RaiseUpdateParameterEvent(new ZWaveEvent(routedNode, EventParameter.RoutingInfo, nodeRouting, 0));
                                }
                            }
                            break;

                        default:
                            Utility.DebugLog(DebugMessageType.Warning, "Unhandled RESPONSE " + Utility.ByteArrayToString(args.Message));
                            break;

                        }

                        break;

                    default:
                        Utility.DebugLog(DebugMessageType.Warning, "Unhandled MESSAGE TYPE " + Utility.ByteArrayToString(args.Message));
                        break;
                    }

                    break;
                }

            }
            catch (Exception ex)
            {

                Utility.DebugLog(DebugMessageType.Error, "Exception occurred :" + ex.Message + "\n" + ex.StackTrace);

            }
        }

        #endregion

        #region ZWaveNode events

        // TODO: deprecate this
        private void znode_ManufacturerSpecificResponse(object sender, ManufacturerSpecificResponseEventArg mfargs)
        {
            // Route event to other listeners
            if (this.ManufacturerSpecificResponse != null)
            {
                ManufacturerSpecificResponse(sender, mfargs);
            }
        }

        private void znode_ParameterChanged(object sender, ZWaveEvent eventData)
        {
            if (sender is ZWaveNode)
            {
                ZWaveNode node = (ZWaveNode)sender;
                if (eventData.Parameter == EventParameter.SecurityDecriptedMessage && eventData.Value is byte[])
                {
                    node.ApplicationCommandHandler((byte[])eventData.Value);
                    return;
                }
                else if (eventData.Parameter == EventParameter.SecurityGeneratedKey && eventData.Value is int)
                {
                    SaveNodesConfig();
                    return;
                }
                else if (eventData.Parameter == EventParameter.SecurityNodeInformationFrame)
                {
                    node.SecuredNodeInformationFrame = (byte[])eventData.Value;

                    // we take them one a a time to make sure we keep the list with unique elements
                    foreach (byte nodeInfo in node.SecuredNodeInformationFrame)
                    {
                        // if we found the COMMAND_CLASS_MARK we get out of the for loop
                        if (nodeInfo == (byte)0xEF)
                            break;
                        node.NodeInformationFrame = Utility.AppendByteToArray(node.NodeInformationFrame, nodeInfo);
                    }

                    // we just send other events and save the node data
                    NodeInformationFrameComplete(node);
                }
            }
            // Route node event
            RaiseUpdateParameterEvent(eventData);
        }

        #endregion

        #endregion Private members

    }

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
        NeighborUpdateStarted = 0x21,
        NeighborUpdateDone = 0x22,
        NeighborUpdateFailed = 0x23,
        DiscoveryStart = 0xDD,
        NodeUpdated = 0xEE,
        NodeError = 0xFE,
        DiscoveryEnd = 0xFF
    }

    [Serializable]
    public class ZWaveNodeConfig
    {
        public byte NodeId { get; internal set; }
        // we keep the list of CommandClasses that are supported by the node
        // both clear or encrypted communication
        public byte[] NodeInformationFrame { get; internal set; }
        // we keep the list of CommandClasses that need encryption for this node
        public byte[] SecuredNodeInformationFrame { get; internal set; }
        // we kep the decices Private Network Key
        public byte[] DevicePrivateNetworkKey { get; internal set; }
    }

}
