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

    public enum ControllerStatus
    {
        NODE_ADDED = 0x00,
        NODE_REMOVED = 0x01,
        DISCOVERY_START = 0xDD,
        NODE_UPDATED = 0xEE,
        NODE_ERROR = 0xFE,
        DISCOVERY_END = 0xFF
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

        public enum Command
        {
            CMD_NONE = 0x00,
            CMD_DISCOVERY_NODES = 0x02,
            CMD_APPLICATION_COMMAND = 0x04,
            CMD_CONTROLLER_SOFT_RESET = 0x08,
            CMD_SEND_DATA = 0x13,
            CMD_GET_NODE_PROTOCOL_INFO = 0x41,
            CMD_CONTROLLER_SET_DEFAULT = 0x42, // hard reset
            CMD_NODE_UPDATE_INFO = 0x49,
            CMD_NODE_ADD = 0x4A,
            CMD_NODE_REMOVE = 0x4B,
            CMD_REQUEST_NODE_INFO = 0x60
        }

        #endregion Public fields

        #region Private fields

        private List<ZWaveNode> devices = new List<ZWaveNode>();
        private byte nodeOperationIdCheck = 0;
        private byte currentCommandTargetNode = 0;
        private byte[] lastMessage = null;
        private DateTime lastMessageTimestamp = DateTime.UtcNow;
        private ManualResetEvent nodeCapabilityAck = new ManualResetEvent(false);

        #endregion Private fields

        #region Lifecycle

        public Controller(ZWavePort zwavePort) : base(1, zwavePort)
        {
            zwavePort.ZWaveMessageReceived += new ZWavePort.ZWaveMessageReceivedEvent((
                object sender,
                ZWaveMessageReceivedEventArgs args
            ) =>
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
            OnControllerEvent(new ControllerEventArgs(0x00, ControllerStatus.DISCOVERY_START)); // Send event
            zwavePort.Discovery();
        }

        public void SoftReset()
        {
            byte[] message = new byte[] {
                (byte)ZWaveMessageHeader.SOF, /* Start Of Frame */
                0x04, /* packet length */
                (byte)ZWaveMessageType.REQUEST, /* Type of message */
                (byte)Command.CMD_CONTROLLER_SOFT_RESET,
                0xff, /* nodeid */
                0x00
            };
            SendMessage(message, false);
        }

        public void HardReset()
        {
            byte[] message = new byte[] {
                (byte)ZWaveMessageHeader.SOF, /* Start Of Frame */
                0x04, /* packet length */
                (byte)ZWaveMessageType.REQUEST, /* Type of message */
                (byte)Command.CMD_CONTROLLER_SET_DEFAULT,
                0xff, /* nodeid */
                0x00
            };
            SendMessage(message, true);
        }

        public byte BeginNodeAdd()
        {
            byte[] header = new byte[] {
                (byte)ZWaveMessageHeader.SOF, /* Start Of Frame */
                0x05, /*packet len */
                (byte)ZWaveMessageType.REQUEST, /* Type of message */
                0x4a
            };
            byte[] footer = new byte[] { 0x03 | 0x80, 0x00, 0x00 };
            byte[] message = new byte[header.Length + footer.Length];

            System.Array.Copy(header, 0, message, 0, header.Length);
            System.Array.Copy(footer, 0, message, message.Length - footer.Length, footer.Length);

            byte callbackId = SendMessage(message);

            return callbackId;
        }

        public byte StopNodeAdd()
        {
            byte[] header = new byte[] {
                (byte)ZWaveMessageHeader.SOF, /* Start Of Frame */
                0x05 /*packet len */,
                (byte)ZWaveMessageType.REQUEST, /* Type of message */
                0x4a
            };
            byte[] footer = new byte[] { 0x05, 0x00, 0x00 };
            byte[] message = new byte[header.Length + footer.Length];

            System.Array.Copy(header, 0, message, 0, header.Length);
            System.Array.Copy(footer, 0, message, message.Length - footer.Length, footer.Length);

            return SendMessage(message);
        }

        public byte BeginNodeRemove()
        {

            byte[] header = new byte[] {
                (byte)ZWaveMessageHeader.SOF, /* Start Of Frame */
                0x05 /*packet len */,
                (byte)ZWaveMessageType.REQUEST, /* Type of message */
                0x4b
            };
            byte[] footer = new byte[] { 0x01 | 0x80, 0x00, 0x00 };
            byte[] message = new byte[header.Length + footer.Length];

            System.Array.Copy(header, 0, message, 0, header.Length);
            System.Array.Copy(footer, 0, message, message.Length - footer.Length, footer.Length);

            return SendMessage(message);
        }

        public byte StopNodeRemove()
        {
            byte[] header = new byte[] {
                (byte)ZWaveMessageHeader.SOF, /* Start Of Frame */
                0x05 /*packet len */,
                (byte)ZWaveMessageType.REQUEST, /* Type of message */
                0x4b
            };
            byte[] footer = new byte[] { 0x05, 0x00, 0x00 };
            byte[] message = new byte[header.Length + footer.Length];

            System.Array.Copy(header, 0, message, 0, header.Length);
            System.Array.Copy(footer, 0, message, message.Length - footer.Length, footer.Length);

            return SendMessage(message);
        }

        public void GetNodeInformationFrame(byte nodeId)
        {
            byte[] message = new byte[] {
                (byte)ZWaveMessageHeader.SOF, /* Start Of Frame */
                0x04,
                (byte)ZWaveMessageType.REQUEST, /* Type of message */
                (byte)Command.CMD_REQUEST_NODE_INFO,
                nodeId,
                0x00
            };
            SendMessage(message, true);
        }

        #endregion Public members

        #region Private members

        private void GetNodeCapabilities(byte nodeId)
        {
            lock (this)
            {
                nodeCapabilityAck.Reset();
                currentCommandTargetNode = nodeId;
                byte[] message = new byte[] {
                    (byte)ZWaveMessageHeader.SOF, /* Start Of Frame */
                    0x04,
                    (byte)ZWaveMessageType.REQUEST, /* Type of message */
                    (byte)Command.CMD_GET_NODE_PROTOCOL_INFO,
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
                ZWaveMessageHeader zwaveHeader = (ZWaveMessageHeader)args.Message[0];
                switch (zwaveHeader)
                {
                case ZWaveMessageHeader.CAN:
                    zwavePort.SendAck();
                    // RESEND
                    //Console.WriteLine("ZWaveLib: received CAN, resending last message");
                    //zp.ResendLastMessage();
                    break;

                case ZWaveMessageHeader.ACK:
                    zwavePort.SendAck();
                    break;

                case ZWaveMessageHeader.SOF: // start of zwave frame
                    //
                    // parse frame headers
                    //
                    //int msgLength = (int)args.Message[1];
                    var msgType = (ZWaveMessageType)args.Message[2];
                    var cmdClass = (args.Message.Length > 3 ? (Command)args.Message[3] : 0);
                    byte sourceNodeId = 0;
                    //
                    switch (msgType)
                    {
                    case ZWaveMessageType.REQUEST:
                        zwavePort.SendAck();

                        if (devices.Count == 0) break;

                        switch (cmdClass)
                        {
                        case Command.CMD_NONE:
                            break;

                        case Command.CMD_NODE_ADD:

                            //callbackId = args.Message[ 4 ];
                            if (args.Message[5] == 0x03) // ADD_NODE_STATUS_ADDING_SLAVE
                            {
                                //
                                //Console.WriteLine("\n\nADDING NODE SLAVE {0}\n     ->   ", zp.ByteArrayToString(args.Message));
                                //
                                // example response from HSM-100 3in1 sensor 
                                // 01 15 00 4A 32 03 2E 0E 04 21 01 60 31 70 84 85 80 72 77 86 EF 20 79
                                //                         bt gt st c1 c2 c3 c4 c5 c6 c7 c8 c9 --------
                                // node supported classes (c1, c2.... cn)
                                //
                                nodeOperationIdCheck = args.Message[6];
                                CreateDevice(nodeOperationIdCheck, 0x00);
                            }
                            else if (args.Message[5] == 0x05) // ADD_NODE_STATUS_PROTOCOL_DONE
                            {
                                if (nodeOperationIdCheck == args.Message[6])
                                {
                                    //Console.WriteLine("\n\nADDING NODE PROTOCOL DONE {0} {1}\n\n", args.Message[6], callbackid);
                                    Thread.Sleep(500);
                                    GetNodeCapabilities(args.Message[6]);
                                }
                                OnControllerEvent(new ControllerEventArgs(
                                    0x00,
                                    ControllerStatus.DISCOVERY_END
                                )); // Send event
                            }
                            else if (args.Message[5] == 0x07) // ADD_NODE_STATUS_ADDING_FAIL
                            {
                                //Console.WriteLine("\n\nADDING NODE FAIL {0}\n\n", args.Message[6]);
                            }
                            break;

                        case Command.CMD_NODE_REMOVE:

                            if (args.Message[5] == 0x03) // REMOVE_NODE_STATUS_REMOVING_SLAVE
                            {
                                //Console.WriteLine("\n\nREMOVING NODE SLAVE {0}\n\n", args.Message[6]);
                                nodeOperationIdCheck = args.Message[6];
                            }
                            else if (args.Message[5] == 0x06) // REMOVE_NODE_STATUS_REMOVING_DONE
                            {
                                if (nodeOperationIdCheck == args.Message[6])
                                {
                                    //Console.WriteLine("\n\nREMOVING NODE DONE {0} {1}\n\n", args.Message[6], callbackid);
                                    RemoveDevice(args.Message[6]);
                                }
                                OnControllerEvent(new ControllerEventArgs(
                                    0x00,
                                    ControllerStatus.DISCOVERY_END
                                )); // Send event
                            }
                            else if (args.Message[5] == 0x07) // REMOVE_NODE_STATUS_REMOVING_FAIL
                            {
                                //Console.WriteLine("\n\nREMOVING NODE FAIL {0}\n\n", args.Message[6]);
                            }
                            break;

                        case Command.CMD_APPLICATION_COMMAND:

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

                        case Command.CMD_SEND_DATA:

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
                                    OnControllerEvent(new ControllerEventArgs(nodeID, ControllerStatus.NODE_ERROR));
                                }
                            }
                            break;

                        case Command.CMD_NODE_UPDATE_INFO:

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
                                    ParameterType.PARAMETER_NODE_INFO,
                                    zwavePort.ByteArrayToString(nodeInfo)
                                );
                                RaiseUpdateParameterEvent(
                                    znode,
                                    0,
                                    ParameterType.PARAMETER_WAKEUP_NOTIFY,
                                    "1"
                                );
                            }
                            break;

                        default:
                            Console.WriteLine("\nUNHANDLED Z-Wave REQUEST\n     " + zwavePort.ByteArrayToString(args.Message) + "\n");
                            break;

                        }

                        break;

                    case ZWaveMessageType.RESPONSE:

                        switch (cmdClass)
                        {
                        case Command.CMD_DISCOVERY_NODES:
                            MessageResponseNodeBitMaskHandler(args.Message);
                            break;
                        case Command.CMD_GET_NODE_PROTOCOL_INFO:
                            MessageResponseNodeCapabilityHandler(args.Message);
                            break;
                        case Command.CMD_REQUEST_NODE_INFO:
                            // TODO: shall we do something here?
                            break;
                        case Command.CMD_SEND_DATA:
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
                    if (node == null)
                    {
                        //Console.WriteLine("Z-Wave Adding node " + currentCommandTargetNode + " Class[ Basic=" + receivedMessage[7].ToString("X2") + " Generic=" + ((GenericType)receivedMessage[8]).ToString() + " Specific=" + receivedMessage[9].ToString("X2") + " ]");
                        devices.Add(CreateDevice(currentCommandTargetNode, receivedMessage[8]));
                    }
                    //
                    node.BasicClass = receivedMessage[7];
                    node.GenericClass = receivedMessage[8];
                    node.SpecificClass = receivedMessage[9];
                    node.SetGenericHandler();
                    //
                    if (node.NodeId != 1)
                    {
                        //Console.WriteLine("Z-Wave Updating node " + node.NodeId + " Class[ Basic=" + receivedMessage[7].ToString("X2") + " Generic=" + ((GenericType)receivedMessage[8]).ToString() + " Specific=" + receivedMessage[9].ToString("X2") + " ]");
                    }
                }
                catch
                {
                    //Console.WriteLine("Z-Wave ERROR adding node " + node.NodeId + " : " + e.Message + "\n" + e.StackTrace);
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
                    OnControllerEvent(new ControllerEventArgs(i, ControllerStatus.NODE_UPDATED)); // Send event
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
                    OnControllerEvent(new ControllerEventArgs(0x00, ControllerStatus.DISCOVERY_END)); // Send event
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
            OnControllerEvent(new ControllerEventArgs(nodeId, ControllerStatus.NODE_REMOVED)); // Send event
        }

        private ZWaveNode CreateDevice(byte nodeId, byte genericClass)
        {
            string className = "ZWaveLib.Devices.";
            switch (genericClass)
            {
            case 0x02:
                className += "Controller";
                this.NodeId = nodeId;
                return (ZWaveNode)this;
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
            //znode.ManufacturerSpecific_Get();
            //
            OnControllerEvent(new ControllerEventArgs(nodeId, ControllerStatus.NODE_ADDED)); // Send event
            //
            return znode;
        }

        private void znode_ManufacturerSpecificResponse(object sender, ManufacturerSpecificResponseEventArg mfargs)
        {
            RaiseUpdateParameterEvent(
                (ZWaveNode)sender,
                0,
                ParameterType.PARAMETER_ZWAVE_MANUFACTURER_SPECIFIC,
                mfargs.ManufacturerSpecific
            );
            if (this.ManufacturerSpecificResponse != null)
            {
                // route nodes events
                ManufacturerSpecificResponse(sender, mfargs);
            }
        }

        private void znode_UpdateNodeParameter(object sender, UpdateNodeParameterEventArgs upargs)
        {
            RaiseUpdateParameterEvent((ZWaveNode)sender, upargs.ParameterId, upargs.ParameterType, upargs.Value);
        }

        #endregion Private members
    }
}
