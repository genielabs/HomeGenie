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
		// we keep the list of CommandClasses that are supported by the node
		// both clear or encrypted communication
        public byte[] NodeInformationFrame { get; set; }
		// we keep the list of CommandClasses that need encryption for this node
        public byte[] SecuredNodeInformationFrame { get; set; }
        // we kep the decices Private Network Key
        public byte[] DevicePrivateNetworkKey { get; set; }
    }

    public class ZWave_cb_to_node
    {
        public byte NodeId { get; set; }
        public byte CallBackId { get; set; }
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
        private List<ZWave_cb_to_node> cb_to_node = new List<ZWave_cb_to_node>();
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
                    Utility.DebugLog(DebugMessageType.Error, "Exception occurred in _zwavemessagereceived(...) " + ex.Message + "\n" + ex.StackTrace);
                }
            });
            LoadNodesConfig();
        }

        #endregion Lifecycle

        #region Public members

        public ZWaveNode GetDevice(byte nodeId)
        {
            return devices.Find(zn => zn.Id == nodeId);
        }

        public List<ZWaveNode> Devices
        {
            get { return devices; }
        }

        public void Discovery()
        {
            OnControllerEvent(new ControllerEventArgs(0x00, ControllerStatus.DiscoveryStart));
            zwavePort.Discovery();
        }

        private void AddNodeToList(byte nodeId, byte CallBackId) {
            if (CallBackId != null)
            {
                Boolean addIt = true;
                ZWave_cb_to_node element = cb_to_node.Find(item => item.NodeId == nodeId);
                if (element != null)
                {
                    if (element.CallBackId != CallBackId)
                    {
                        // the node is already there but it's expecting a different call back  so adding the new CallBackId
                        element.CallBackId = CallBackId;
                        addIt = false;
                    }
                }

                if (addIt)
                {
                    Utility.DebugLog(DebugMessageType.Information, "AddNodeToList - NodeId - " + nodeId.ToString("X2") + " CallbackId -" + CallBackId.ToString("X2"));
                    cb_to_node.Add(new ZWave_cb_to_node()
                    {
                        NodeId = nodeId,
                        CallBackId = CallBackId
                    });
                }
            }
        }

        public void NodeNeighborUpdate(byte nodeId)
        {
//            OnControllerEvent(new ControllerEventArgs(0x00, ControllerStatus.DiscoveryStart));
            //zwavePort.NodeNeighborUpdate(nodeId);
            var node = devices.Find(n => n.Id == nodeId);
            byte[] msg = new byte[] {
                (byte)MessageHeader.SOF,
                0x06, /* packet length */
                (byte)MessageType.Request, /* Type of message */
                (byte)Function.NodeNeighborUpdateOptions,
                nodeId,
                0x25,
                0x00,
                0x00};
            byte cb = node.SendMessage(msg);
            Utility.DebugLog(DebugMessageType.Information, "++++++++ AddNodeToList - CallbackId -" + cb.ToString("X2"));
            AddNodeToList(nodeId, cb);

            Thread.Sleep(10000);

            // we force the NodeNeighborUpdate
            msg = new byte[] {
                    (byte)MessageHeader.SOF,
                    0x05, /* packet length */
                    (byte)MessageType.Request, /* Type of message */
                    (byte)Function.NodeNeighborUpdate,
                    nodeId,
                    0x00,
                    0x00    
                };

            cb = node.SendMessage(msg);


        }

        public void NodeNeighbors(byte CallBackId)
        {
//            var unsentMessage = zwavePort.PendingMessages.Find(zm => zm.CallbackId == commandId);
//            byte nodeID = zwavePort.ResendLastMessage(commandId);

            ZWave_cb_to_node element = cb_to_node.Find(item => item.CallBackId == CallBackId);
            if (element != null)
            {
                var node = devices.Find(n => n.Id == element.NodeId);
                byte[] msg = new byte[] {
                    (byte)MessageHeader.SOF,
                    0x07, /* packet length */
                    (byte)MessageType.Request, /* Type of message */
                    (byte)Function.GetRoutingInfo,
                    element.NodeId,
                    0x00,
                    0x00,
                    0x03,
                    0x00    
                };
                byte cb = node.SendMessage(msg, true);

                Utility.DebugLog(DebugMessageType.Information, "---------------- NodeNeighbors - Removed NodeId - " + element.NodeId.ToString("X2") + " CallbackId -" + element.CallBackId.ToString("X2"));
                // done with the work now remove the element
                cb_to_node.Remove(element);
                
//                AddNodeToList(nodeId, cb);
            }
            else
            {
                Utility.DebugLog(DebugMessageType.Information, "???????????????????? NodeNeighbors - Unable to find callback - " + CallBackId.ToString("X2"));
            }
        }

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
                    // RESEND
                    //Utility.DebugLog(DebugMessageType.Warning, "Received CAN, resending last message");
                    //zp.ResendLastMessage();
                    break;

                case MessageHeader.ACK:
                    break;

                case MessageHeader.SOF: // start of zwave frame
                    //
                    // parse frame headers
                    //
                    //int msgLength = (int)args.Message[1];
                    var msgType = (MessageType)args.Message[2];
                    var function = (args.Message.Length > 3 ? (Function)args.Message[3] : 0);
                    byte sourceNodeId = 0;
                    byte nodeOperation = 0;
                    //
                    switch (msgType)
                    {
                    case MessageType.Request:

                        if (devices.Count == 0)
                            break;

                        switch (function)
                        {
                        case Function.None:
                            break;

                        case Function.NodeAdd:

                            nodeOperation = args.Message[5];
                            if (nodeOperation == (byte)NodeFunctionStatus.AddNodeAddingSlave)
                            {
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
                                    gotNodeUpdateInformation(newNode);
                                }
                            }
                            else if (nodeOperation == (byte)NodeFunctionStatus.AddNodeProtocolDone /* || nodeOperation == (byte)NodeFunctionStatus.AddNodeDone */)
                            {
                                if (nodeOperationIdCheck == args.Message[6])
                                {
                                    Thread.Sleep(500);
                                    GetNodeCapabilities(args.Message[6]);
                                    var newNode = devices.Find(n => n.Id == args.Message[6]);
                                    if (newNode != null) ManufacturerSpecific.Get(newNode);
                                }
                                OnControllerEvent(new ControllerEventArgs(0x00, ControllerStatus.DiscoveryEnd));
                            }
                            else if (nodeOperation == (byte)NodeFunctionStatus.AddNodeFailed)
                            {
                                Utility.DebugLog(DebugMessageType.Warning, "ADDING NODE FAILED (" + args.Message[6] + ")");
                            }
                            break;

                        case Function.NodeRemove:

                            nodeOperation = args.Message[5];
                            if (nodeOperation == (byte)NodeFunctionStatus.RemoveNodeRemovingSlave)
                            {
                                nodeOperationIdCheck = args.Message[6];
                            }
                            else if (nodeOperation == (byte)NodeFunctionStatus.RemoveNodeDone)
                            {
                                if (nodeOperationIdCheck == args.Message[6])
                                {
                                    RemoveDevice(args.Message[6]);
                                }
                                OnControllerEvent(new ControllerEventArgs(0x00, ControllerStatus.DiscoveryEnd));
                            }
                            else if (nodeOperation == (byte)NodeFunctionStatus.RemoveNodeFailed)
                            {
                                Utility.DebugLog(DebugMessageType.Warning, "REMOVING NODE FAILED (" + args.Message[6] + ")");
                            }
                            break;

                        case Function.ApplicationCommand:

                            sourceNodeId = args.Message[5];
                            var node = devices.Find(n => n.Id == sourceNodeId);
                            if (node != null)
                            {
                                try
                                {
                                    node.MessageRequestHandler(args.Message);
                                }
                                catch (Exception ex)
                                {
                                    Utility.DebugLog(DebugMessageType.Error, "Exception occurred in node.MessageRequestHandler: " + ex.Message + "\n" + ex.StackTrace);
                                }
                            }
                            else
                            {
                                Utility.DebugLog(DebugMessageType.Error, "Unknown node id " + sourceNodeId);
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
                                var unsentMessage = zwavePort.PendingMessages.Find(zm => zm.CallbackId == commandId);
                                byte nodeID = zwavePort.ResendLastMessage(commandId);
                                if (nodeID != 0)
                                {
                                    // Resend timed out
                                    OnControllerEvent(new ControllerEventArgs(nodeID, ControllerStatus.NodeError));
                                    // Check if node supports WakeUp class, and add message to wake up message queue
                                    if (unsentMessage != null)
                                    {
                                        var sleepingNode = devices.Find(n => n.Id == nodeID);
                                        if (sleepingNode != null && sleepingNode.SupportCommandClass(CommandClass.WakeUp))
                                        {
                                            WakeUp.ResendOnWakeUp(sleepingNode, unsentMessage.Message);
                                        }
                                    }
                                }
                            }
                            break;

                        case Function.NodeUpdateInfo:

                            sourceNodeId = args.Message[5];
                            int nifLength = (int)args.Message[6];
                            var znode = devices.Find(n => n.Id == sourceNodeId);
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
                                    gotNodeUpdateInformation(znode);
                                }                                
                            }
                            break;
                        case Function.NodeNeighborUpdateOptions:
                        case Function.NodeNeighborUpdate:

                            string message = args.Message[3] == (byte)Function.NodeNeighborUpdateOptions ? "Options " : "";

                            switch (args.Message[5])
                            {
                                case (byte) NeighborUpdateOptions.NeighborUpdateStared:
                                    Utility.DebugLog(DebugMessageType.Warning, "Node Neighbor Update " + message + "STARTED: " + Utility.ByteArrayToString(args.Message));
                                    // the update started
                                    break;
                                case (byte) NeighborUpdateOptions.NeighborUpdateDone:
                                    Utility.DebugLog(DebugMessageType.Warning, "Node Neighbor Update " + message + "DONE: " + Utility.ByteArrayToString(args.Message));
                                    // We now request the neighbour information from the
                                    // controller and store it in our node object.
                                    // needs to be implemented
//                                    var unsentMessage = zwavePort.PendingMessages.Find(zm => zm.CallbackId == args.Message[4]);
//                                    byte nodeID = zwavePort.ResendLastMessage(args.Message[4]);
                                    if (message.Length > 0)
                                        NodeNeighbors(args.Message[4]);

                                    break;
                                case (byte) NeighborUpdateOptions.NeighborUpdateFailed:
                                    Utility.DebugLog(DebugMessageType.Warning, "Node Neighbor Update " + message + "FAILED: " + Utility.ByteArrayToString(args.Message));
                                    // the update failed
                                    break;
                                default:
                                    Utility.DebugLog(DebugMessageType.Warning, "Unhandled Node Neighbor Update "  + message + "REQUEST " + Utility.ByteArrayToString(args.Message));
                                    break;
                            }
                            break;
                        default:
                            Utility.DebugLog(DebugMessageType.Warning, "Unhandled REQUEST " + Utility.ByteArrayToString(args.Message));
                            break;

                        }

                        break;

                    case MessageType.Response:

                        switch (function)
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
                            bool found = false;
                            for (int by = 0; by < 29; by++)
                            {
                                for (int bi = 0; bi < 8; bi++)
                                {
                                    int result = args.Message[4 + by] & (0x01 << bi);
//                                    Utility.DebugLog(DebugMessageType.Warning, "GetRoutingInfo " + result);
                                    if (result > 0)
                                    {
                                        Utility.DebugLog(DebugMessageType.Warning, "Reported Node: " + result.ToString());
                                        found = true;
                                    }
                                }
                            }
                            if (!found)
                            {
                                Utility.DebugLog(DebugMessageType.Warning, "No nodes reported.");
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

        private void gotNodeUpdateInformation(ZWaveNode znode)
        {
            // once we get the security command classes we'll issue the same events and call SaveNodesConfig();
            RaiseUpdateParameterEvent(new ZWaveEvent(znode, EventParameter.NodeInfo, Utility.ByteArrayToString(znode.NodeInformationFrame), 0));
            RaiseUpdateParameterEvent(new ZWaveEvent(znode, EventParameter.WakeUpNotify, "1", 0));
            SaveNodesConfig();
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

        private ZWaveNode CreateDevice(byte nodeId, byte genericClass)
        {
            ZWaveNode node;
            switch (genericClass)
            {
                case (byte) GenericType.StaticController:
                    // TODO: this is very untested...
                    node = (ZWaveNode) new Controller(zwavePort);
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

        private List<Type> GetTypesInNamespace(Assembly assembly, string nameSpace)
        {
            var typeList = new List<Type>();
            foreach (Type type in assembly.GetTypes())
            {
                if (type.Namespace != null && type.Namespace.StartsWith(nameSpace))
                    typeList.Add(type);
            }
            //return assembly.GetTypes().Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToArray();
            return typeList;
        }

        private void LoadNodesConfig()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "zwavenodes.xml");
            try
            {
                var serializer = new XmlSerializer(nodesConfig.GetType());
                var reader = new StreamReader(configPath);
                nodesConfig = (List<ZWaveNodeConfig>)serializer.Deserialize(reader);
                foreach (ZWaveNodeConfig node in nodesConfig) {
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
                    nodesConfig.Add(new ZWaveNodeConfig()
                    {
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

        #region Events Handling

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
                ZWaveNode node = (ZWaveNode) sender;
                if (eventData.Parameter == EventParameter.SecurityDecriptedMessage && eventData.Value is byte[])
                {
                    node.MessageRequestHandler((byte[])eventData.Value);
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
                    gotNodeUpdateInformation(node);
                }
            }
            // Route node event
            RaiseUpdateParameterEvent(eventData);
        }

        #endregion

        #endregion Private members
    }
}
