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
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.Linq;
using ZWaveLib.CommandClasses;
using ZWaveLib.Values;

namespace ZWaveLib
{

    public class ManufacturerSpecificResponseEventArg
    {
        public int NodeId { get; internal set; }
        public ManufacturerSpecificInfo ManufacturerSpecific;
    }

    public class ZWaveNode
    {
        
        #region Private fields

        internal ZWavePort zwavePort;

        #endregion Private fields

        #region Public fields

        public byte Id { get; protected set; }
        public string ManufacturerId { get; protected set; }
        public string TypeId { get; protected set; }
        public string ProductId { get; protected set; }
        public byte BasicClass { get; internal set; }
        public byte GenericClass { get; internal set; }
        public byte SpecificClass { get; internal set; }
        public byte[] NodeInformationFrame { get; internal set; }

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

        public virtual bool MessageRequestHandler(byte[] receivedMessage)
        {
            //Console.WriteLine("\n   _z_ [" + this.NodeId + "]  " + (this.DeviceHandler != null ? this.DeviceHandler.ToString() : "!" + this.GenericClass.ToString()));
            //Console.WriteLine("   >>> " + zp.ByteArrayToString(receivedMessage) + "\n");

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
                this.RaiseUpdateParameterEvent(messageEvent);
            }
            else if (messageLength > 3)
            {
                if (receivedMessage[3] != 0x13)
                {
                    bool log = true;
                    if (messageLength > 7 && /* cmd_class */ receivedMessage[7] == (byte)CommandClass.ManufacturerSpecific)
                        log = false;
                    if (log)
                        Console.WriteLine("ZWaveLib UNHANDLED message: " + Utility.ByteArrayToString(receivedMessage));
                }
            }

            return false;
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
            SendMessage(ZWaveMessage.CreateRequest(this.Id, request));
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
