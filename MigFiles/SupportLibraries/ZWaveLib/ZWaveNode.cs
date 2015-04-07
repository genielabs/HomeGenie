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
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.Linq;
using System.Diagnostics;

using ZWaveLib.CommandClasses;
using ZWaveLib.Values;

namespace ZWaveLib
{

    public class SupportedCommandClasses : IEquatable<SupportedCommandClasses>
    {
        public byte cclass { get; set; }
        public bool secure { get; set; }
        public bool afterMark { get; set; }
        private string name;
        public bool supported = false;
        public int instance = 1;
        public override string ToString()
        {
            if (Enum.IsDefined(typeof(CommandClass), (byte)cclass))
            {
                name = ((CommandClass)(byte)cclass).ToString();
                supported = true;
            }

            return " Class: ( " + (secure ? "Secured  " : "Unsecured") + " )" + " - " + (supported ? name : Utility.ByteArrayToString(new byte[] { (byte)cclass })) + (afterMark ? " - After Mark" : "") + (!supported ? " - UNSUPPORTED" : "");
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            SupportedCommandClasses objAsCC = obj as SupportedCommandClasses;
            if (objAsCC == null)
                return false;
            else
                return Equals(objAsCC);
        }

        public override int GetHashCode()
        {
            return cclass;
        }

        public bool Equals(SupportedCommandClasses obj)
        {
            if (obj == null)
                return false;
            return (this.cclass.Equals(obj.cclass));
        }
    }

    public class ManufacturerSpecificResponseEventArg
    {
        public int NodeId { get; internal set; }
        public ManufacturerSpecificInfo ManufacturerSpecific;
    }
    
    public class SecutiryPayload
    {
        public byte[] message;
        public int length;
        public int part;
    }

    public class ZWaveNode
    {
        List<SupportedCommandClasses> supportedClasses = new List<SupportedCommandClasses>();

        #region Private fields

        internal ZWavePort zwavePort;
        internal Controller pController;

        #endregion Private fields

        #region Public fields

        public SecurityHandler security = new SecurityHandler();

        public byte Id { get; protected set; }
        public string ManufacturerId { get; protected set; }
        public string TypeId { get; protected set; }
        public string ProductId { get; protected set; }
        public byte BasicClass { get; internal set; }
        public byte GenericClass { get; internal set; }
        public byte SpecificClass { get; internal set; }
        public byte[] NodeInformationFrame { get; internal set; }
        public byte[] SecuredNodeInformationFrame { get; internal set; }

        public Dictionary<string, object> Data = new Dictionary<string, object>();

        public delegate void ParameterChangedEventHandler(object sender, ZWaveEvent eventData);
        public event ParameterChangedEventHandler ParameterChanged;

        public delegate void ManufacturerSpecificResponseEventHandler(object sender, ManufacturerSpecificResponseEventArg mfargs);
        public virtual event ManufacturerSpecificResponseEventHandler ManufacturerSpecificResponse;

        #endregion Public fields

        #region Lifecycle

        public ZWaveNode(byte nodeId, ZWavePort zport)
        {
            this.Id = nodeId;
            this.zwavePort = zport;
        }

        public ZWaveNode(byte nodeId, ZWavePort zp, byte genericType)
        {
            this.Id = nodeId;
            this.zwavePort = zp;
            this.GenericClass = genericType;
        }

        #endregion Lifecycle

        #region Public members

        //this is designed to be called during NodeAdd ONLY 
        public bool HandleSecureCommandClasses(byte[] nodeInfo)
        {
            bool foundSecure = false;
            foreach (byte b in nodeInfo)
            {
                // if we are security then we need to initalize
                if (b == (byte) CommandClass.Security) {
                    security.adding_node = true;
                    var cc = CommandClassFactory.GetCommandClass(b);
                    cc.GetEvent(this, null);
                    foundSecure = true;
                }
            }

            return foundSecure;
        }

        public virtual bool MessageRequestHandler(Controller ctrl, byte[] receivedMessage)
        {
            //Console.WriteLine("\n   _z_ [" + this.NodeId + "]  " + (this.DeviceHandler != null ? this.DeviceHandler.ToString() : "!" + this.GenericClass.ToString()));
            //Console.WriteLine("   >>> " + zp.ByteArrayToString(receivedMessage) + "\n");

			// saving a reference to the controller
            pController = ctrl;

            ZWaveEvent messageEvent = null;
            int messageLength = receivedMessage.Length;

            if (messageLength > 8)
            {
                byte commandLength = receivedMessage[6];
                byte commandClass = receivedMessage[7];
                var cc = CommandClassFactory.GetCommandClass(commandClass);
                byte[] message = new byte[commandLength];
                Array.Copy(receivedMessage, 7, message, 0, commandLength);
                messageEvent = cc.GetEvent(this, message);
            }

            if (messageEvent != null)
            {
                if (messageEvent.Parameter == EventParameter.ManufacturerSpecific)
                {
                    var specs = (ManufacturerSpecificInfo)messageEvent.Value;
                    this.ManufacturerId = specs.ManufacturerId;
                    this.TypeId = specs.TypeId;
                    this.ProductId = specs.ProductId;
                    if (ManufacturerSpecificResponse != null)
                    {
                        try
                        {
                            ManufacturerSpecificResponse(this, new ManufacturerSpecificResponseEventArg()
                            {
                                NodeId = this.Id,
                                ManufacturerSpecific = specs
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("ZWaveLib: Error during ManufacturerSpecificResponse callback, " +
                                              ex.Message + "\n" + ex.StackTrace);
                        }
                    }
                }
                else if (messageEvent.Node.GenericClass == (byte)GenericType.EntryControl)
                { 
                    // this is an event for DoorLock and needs special handling
                    if (messageEvent.Parameter == EventParameter.AlarmGeneric)
                    {
                        int value = System.Convert.ToInt16(messageEvent.Value);
                        messageEvent.Parameter = EventParameter.DoorLockStatus;
                        if (value == 1)
                        {
                            messageEvent.Value = "Locked";
                        }
                        else if (value == 2)
                        {
                            messageEvent.Value = "Unlocked";
                        }
                        else if (value == 5)
                        {
                            messageEvent.Value = "Locked from outside";
                        }
                        else if (value == 6)
                        {
                            messageEvent.Value = "Unlocked by user " + System.Convert.ToInt32(receivedMessage[16].ToString("X2"), 16);
                        }
                        else if (value == 16)
                        {
                            messageEvent.Value = "Unatuthorized unlock attempted";
                        }

                    }
                }

                this.RaiseUpdateParameterEvent(messageEvent);
            }
            else if (messageLength > 3)
            {
                if (receivedMessage[3] != 0x13)
                {
                    bool log = true;
                    // do not log an error message for ManufacturerSpecific and Security CommandClass
                    if (messageLength > 7 && /* cmd_class */ (receivedMessage[7] == (byte)CommandClass.ManufacturerSpecific || receivedMessage[7] == (byte) CommandClass.Security))
                        log = false;
                    if (log)
                        Console.WriteLine("ZWaveLib UNHANDLED message: " + Utility.ByteArrayToString(receivedMessage));
                }
            }

            return false;
        }

        public bool HandleNodeUpdate(byte[] msg)
        {
            bool foundSecure = false;

            Console.WriteLine("  Optional command classes for node " + this.Id + ":");
            int start = 7;

            byte[] arr = new byte[msg[6]];
            Array.Copy(msg, start, arr, 0, msg[6]);

            foundSecure = BuildSupportedList(arr, false);

            // we are checking for security
            if(foundSecure)
            {
                security.sendSupportedGet(this);
            }

            return foundSecure;
        }

        public bool SetSecuredClasses(byte[] msg)
        {

            Console.WriteLine("  Secured command classes for node " + this.Id + ":");

            int start = 3;

            byte[] arr = new byte[msg.Length - start];
            Array.Copy(msg, start, arr, 0, msg.Length - start);

            BuildSupportedList(arr, true);


            pController.SaveNodesConfig();

            RaiseUpdateParameterEvent(new ZWaveEvent(this, EventParameter.NodeInfo, Utility.ByteArrayToString(this.NodeInformationFrame), 0));
            RaiseUpdateParameterEvent(new ZWaveEvent(this, EventParameter.WakeUpNotify, "1", 0));

            return true;
        }

        public bool BuildSupportedList(byte[] nodesInfo, bool secured) {

            bool afterMark = false;
            bool foundSecure = false;

            if (nodesInfo == null)
                return false;

            foreach(byte nodeInfo in nodesInfo){

                if (nodeInfo == (byte)0xEF)
                {
                    // COMMAND_CLASS_MARK.
                    // Marks the end of the list of supported command classes.  The remaining classes
                    // are those that can be controlled by the device.  These classes are created
                    // without values.  Messages received cause notification events instead.
                    afterMark = true;
                    continue;
                }

                var cc = CommandClassFactory.GetCommandClass((byte)nodeInfo);

                if (cc == null)
                {
                    Console.WriteLine(nodeInfo.ToString("X2") + " - We don't NOT supporte this CommandClass");
                }
                else
                {

                    SupportedCommandClasses scc = supportedClasses.Find(x => x.cclass == (byte)nodeInfo);

                    if (scc == null)
                    {
                        scc = new SupportedCommandClasses { cclass = (byte)nodeInfo, secure = secured, afterMark = afterMark };
                        supportedClasses.Add(scc);
                        Console.WriteLine("Added " + scc);
                    }
                    else
                    {
                        scc.secure = true;
                        Console.WriteLine("Updated " + scc);
                    }
                }

                if (nodeInfo == (byte)CommandClass.Security)
                {
                    foundSecure = true;
                }

                this.NodeInformationFrame = addElementToArray(this.NodeInformationFrame, nodeInfo);

                if (secured) {
                    this.SecuredNodeInformationFrame = addElementToArray(this.SecuredNodeInformationFrame, nodeInfo);
                }
            }

            return foundSecure;    
    
        }

        private byte[] addElementToArray(byte[] nodesInfo, byte nodeInfo) {
            int pos = -1;

            if (nodesInfo != null)
                pos = Array.IndexOf(nodesInfo, nodeInfo);

            if (pos == -1)
            {
                if (nodesInfo != null)
                {
                    Array.Resize(ref nodesInfo, nodesInfo.Length + 1);
                }
                else
                {
                    nodesInfo = new byte[1];
                }
                nodesInfo[nodesInfo.Length - 1] = nodeInfo;
            }

            return nodesInfo;
        }

        public bool SupportCommandClass(CommandClass c)
        {
            bool isSupported = false;
            if (this.NodeInformationFrame != null)
            {
                isSupported = (Array.IndexOf(this.NodeInformationFrame, (byte)c) >= 0);
            }
            return isSupported;
        }

        public void SendRequest(byte[] request)
        {
            // lookup request[0] in supportedClasses to see if we need to encrypt the message or NOT
            // we don't encrypt the message if it's sent by the Security Class
            byte cmd = request[0];
            SupportedCommandClasses scc = supportedClasses.Find(x => x.cclass == cmd);
            byte[] msg = ZWaveMessage.CreateRequest(this.Id, request);

            if (scc != null && scc.secure && cmd != (byte)CommandClass.Security)
            {
                security.encryptAndSend(this, msg);
            }
            else
            {
                SendMessage(msg);
            }
        }

        #endregion Public members

        #region Private members

        internal byte SendMessage(byte[] message, bool disableCallback = false)
        {
            var msg = new ZWaveMessage() { Node = this, Message = message };
            return zwavePort.SendMessage(msg, disableCallback);
        }
        
        internal void RaiseUpdateParameterEvent(ZWaveEvent zevent) //int pid, EventParameter peventtype, object value)
        {
            if (ParameterChanged != null)
            {
                ParameterChanged(this, zevent);
            }
        }

        #endregion Private members

    }
}
