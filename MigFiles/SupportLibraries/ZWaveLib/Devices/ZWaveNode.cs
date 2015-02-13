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
using ZWaveLib.Devices.ProductHandlers.Generic;

namespace ZWaveLib.Devices
{
    public class UpdateNodeParameterEventArgs
    {
        public int NodeId { get; internal set; }
        public int ParameterId { get; internal set; }
        public ParameterEvent ParameterEvent { get; internal set; }
        public object Value { get; internal set; }
    }

    public class ManufacturerSpecific
    {
        public string ManufacturerId { get; set; }
        public string TypeId { get; set; }
        public string ProductId { get; set; }
    }

    public class ManufacturerSpecificResponseEventArg
    {
        public int NodeId { get; internal set; }
        public ManufacturerSpecific ManufacturerSpecific;
    }

    public class ZWaveNode
    {
        
        #region Private fields

        internal ZWavePort zwavePort;
        private Dictionary<byte, int> nodeConfigParamsLength = new Dictionary<byte, int>();
        private List<byte[]> wakeUpResendQueue = new List<byte[]>();

        #endregion Private fields

        #region Public fields

        public byte NodeId { get; protected set; }
        public string ManufacturerId { get; protected set; }
        public string TypeId { get; protected set; }
        public string ProductId { get; protected set; }
        public byte BasicClass { get; internal set; }
        public byte GenericClass { get; internal set; }
        public byte SpecificClass { get; internal set; }
        public byte[] NodeInformationFrame { get; internal set; }

        public IZWaveDeviceHandler DeviceHandler = null;

        public delegate void UpdateNodeParameterEventHandler(object sender, UpdateNodeParameterEventArgs upargs);
        public event UpdateNodeParameterEventHandler UpdateNodeParameter;

        public delegate void ManufacturerSpecificResponseEventHandler(object sender, ManufacturerSpecificResponseEventArg mfargs);
        public virtual event ManufacturerSpecificResponseEventHandler ManufacturerSpecificResponse;

        #endregion Public fields

        #region Lifecycle

        public ZWaveNode(byte nodeId, ZWavePort zport)
        {
            this.NodeId = nodeId;
            this.zwavePort = zport;
        }

        public ZWaveNode(byte nodeId, ZWavePort zp, byte genericType)
        {
            this.NodeId = nodeId;
            this.zwavePort = zp;
            this.GenericClass = genericType;
        }

        #endregion Lifecycle

        #region Public members

        public virtual bool MessageRequestHandler(byte[] receivedMessage)
        {
            //Console.WriteLine("\n   _z_ [" + this.NodeId + "]  " + (this.DeviceHandler != null ? this.DeviceHandler.ToString() : "!" + this.GenericClass.ToString()));
            //Console.WriteLine("   >>> " + zp.ByteArrayToString(receivedMessage) + "\n");
            //
            bool handled = false;
            int messageLength = receivedMessage.Length;
            //
            if (this.DeviceHandler != null && this.DeviceHandler.HandleRawMessageRequest(receivedMessage))
            {
                handled = true;
            }
            //
            // Only generic command classes are handled directly in this function
            // other device specific commands are handled by corresponding DeviceHandler
            //
            if (!handled && messageLength > 8)
            {

                //byte commandLength = receivedMessage[6];
                byte commandClass = receivedMessage[7];
                byte commandType = receivedMessage[8]; // is this the Payload length in bytes? or is it the command type?
                //
                switch (commandClass)
                {

                case (byte)CommandClass.Basic:
                case (byte)CommandClass.Alarm:
                case (byte)CommandClass.SensorBinary:
                case (byte)CommandClass.SensorAlarm:
                case (byte)CommandClass.SensorMultilevel:
                case (byte)CommandClass.SwitchBinary:
                case (byte)CommandClass.SwitchMultilevel:
                case (byte)CommandClass.SceneActivation:
                case (byte)CommandClass.ThermostatMode:
                case (byte)CommandClass.ThermostatFanMode:
                case (byte)CommandClass.ThermostatFanState:
                case (byte)CommandClass.ThermostatHeating:
                case (byte)CommandClass.ThermostatOperatingState:
                case (byte)CommandClass.ThermostatSetBack: 
                case (byte)CommandClass.ThermostatSetPoint:
                case (byte)CommandClass.Meter:

                    if (this.DeviceHandler != null)
                    {
                        handled = this.DeviceHandler.HandleBasicReport(receivedMessage);
                    }

                    break;
                    
                case (byte)CommandClass.MultiInstance:
                
                    if (messageLength > 10)
                    {
                        if (this.DeviceHandler != null)
                        {
                            handled = this.DeviceHandler.HandleMultiInstanceReport(receivedMessage);
                        }
                    }

                    break;

                case (byte)CommandClass.Configuration:

                    if (messageLength > 11 && commandType == (byte)Command.ConfigurationReport) // CONFIGURATION PARAMETER REPORT  0x06
                    {
                        byte paramId = receivedMessage[9];
                        byte paramLength = receivedMessage[10];
                        //
                        if (!nodeConfigParamsLength.ContainsKey(paramId))
                        {
                            nodeConfigParamsLength.Add(paramId, paramLength);
                        }
                        else
                        {
                            // this shouldn't change on read... but you never know! =)
                            nodeConfigParamsLength[paramId] = paramLength;
                        }
                        //
                        byte[] bval = new byte[4];
                        // extract bytes value
                        Array.Copy(receivedMessage, 11, bval, 4 - (int)paramLength, (int)paramLength);
                        uint paramval = bval[0];
                        Array.Reverse(bval);
                        paramval = BitConverter.ToUInt32(bval, 0);
                        // convert it to uint
                        //
                        RaiseUpdateParameterEvent(this, paramId, ParameterEvent.Configuration, paramval);
                        //
                        handled = true;
                    }

                    break;

                case (byte)CommandClass.Association:

                    if (messageLength > 12 && commandType == (byte)Command.AssociationReport) // ASSOCIATION REPORT 0x03
                    {
                        byte groupId = receivedMessage[9];
                        byte maxAssociations = receivedMessage[10];
                        byte numAssociations = receivedMessage[11]; // it is always zero ?!?
                        string assocNodes = "";
                        if (receivedMessage.Length > 13)
                        {
                            for (int a = 12; a < receivedMessage.Length - 1; a++)
                            {
                                assocNodes += receivedMessage[a] + ",";
                            }
                        }
                        assocNodes = assocNodes.TrimEnd(',');
                        //
                        //_raiseUpdateParameterEvent(this, 0, ParameterType.PARAMETER_ASSOC, groupid);
                        RaiseUpdateParameterEvent(this, 1, ParameterEvent.Association, maxAssociations);
                        RaiseUpdateParameterEvent(this, 2, ParameterEvent.Association, numAssociations);
                        RaiseUpdateParameterEvent(this, 3, ParameterEvent.Association, groupId + ":" + assocNodes);
                        //
                        handled = true;
                    }

                    break;

                case (byte)CommandClass.WakeUp:

                    if (messageLength > 11 && commandType == (byte)Command.WakeUpIntervalReport) // WAKE UP REPORT 0x06
                    {
                        uint interval = ((uint)receivedMessage[9]) << 16;
                        interval |= (((uint)receivedMessage[10]) << 8);
                        interval |= (uint)receivedMessage[11];
                        //
                        RaiseUpdateParameterEvent(this, 0, ParameterEvent.WakeUpInterval, interval);
                        //
                        handled = true;
                    }
                    else if (messageLength > 7 && commandType == (byte)Command.WakeUpNotification) // AWAKE NOTIFICATION 0x07
                    {
                        // Resend queued messages while node was asleep
                        for (int m = 0; m < wakeUpResendQueue.Count; m++)
                        {
                            SendMessage(wakeUpResendQueue[m]);
                        }
                        wakeUpResendQueue.Clear();
                        //
                        RaiseUpdateParameterEvent(this, 0, ParameterEvent.WakeUpNotify, 1);
                        //
                        handled = true;
                    }

                    break;

                case (byte)CommandClass.Battery:

                    if (messageLength > 7 && /*command_length == (byte)Command.COMMAND_BASIC_REPORT && */ commandType == 0x03) // Battery Report
                    {
                        RaiseUpdateParameterEvent(this, 0, ParameterEvent.Battery, receivedMessage[9]);
                        //
                        handled = true;
                    }

                    break;

                case (byte)CommandClass.Hail:

                    this.Basic_Get();
                    handled = true;

                    break;

                case (byte)CommandClass.ManufacturerSpecific:

                    if (messageLength > 14)
                    {
                        byte[] manufacturerId = new byte[2] { receivedMessage[9], receivedMessage[10] };
                        byte[] typeId = new byte[2] { receivedMessage[11], receivedMessage[12] };
                        byte[] productId = new byte[2] { receivedMessage[13], receivedMessage[14] };

                        this.ManufacturerId = zwavePort.ByteArrayToString(manufacturerId).Replace(" ", "");
                        this.TypeId = zwavePort.ByteArrayToString(typeId).Replace(" ", "");
                        this.ProductId = zwavePort.ByteArrayToString(productId).Replace(" ", "");

                        var manufacturerSpecs = new ManufacturerSpecific() {
                            TypeId = zwavePort.ByteArrayToString(typeId).Replace(" ", ""),
                            ProductId = zwavePort.ByteArrayToString(productId).Replace(" ", ""),
                            ManufacturerId = zwavePort.ByteArrayToString(manufacturerId).Replace(" ", "")
                        };
                        //
                        if (ManufacturerSpecificResponse != null)
                        {
                            try
                            {
                                ManufacturerSpecificResponse(this, new ManufacturerSpecificResponseEventArg() {
                                    NodeId = this.NodeId,
                                    ManufacturerSpecific = manufacturerSpecs
                                });
                            }
                            catch (Exception ex)
                            {

                                Console.WriteLine("ZWaveLib: Error during ManufacturerSpecificResponse callback, " + ex.Message + "\n" + ex.StackTrace);

                            }
                        }
                        //
                        handled = true;
                        //
                        //Console.WriteLine (" ########################################################################################################### ");
                        //this.SendMessage (new byte[] { 0x01, 0x09, 0x00, 0x13, 0x13, this.NodeId, 0x31, 0x01, 0x25, 0x40, 0xa1 });
                    }

                    break;

                }

            }
            //
            if (!handled && messageLength > 3)
            {
                if (receivedMessage[3] != 0x13)
                {
                    bool log = true;
                    if (messageLength > 7 && /* cmd_class */ receivedMessage[7] == (byte)CommandClass.ManufacturerSpecific)
                        log = false;
                    if (log)
                        Console.WriteLine("ZWaveLib UNHANDLED message: " + zwavePort.ByteArrayToString(receivedMessage));
                }
            }

            return false;
        }

        public void ResendOnWakeUp(byte[] msg)
        {
            int minCommandLength = 8;
            if (msg.Length >= minCommandLength)
            {
                byte[] command = new byte[minCommandLength];
                Array.Copy(msg, 0, command, 0, minCommandLength);
                // discard any message having same header and command (first 8 bytes = header + command class + command)
                for (int i = wakeUpResendQueue.Count - 1; i >= 0; i--)
                {
                    byte[] queuedCommand = new byte[minCommandLength];
                    Array.Copy(wakeUpResendQueue[i], 0, queuedCommand, 0, minCommandLength);
                    if (queuedCommand.SequenceEqual(command))
                    {
                        wakeUpResendQueue.RemoveAt(i);
                    }
                }
                wakeUpResendQueue.Add(msg);
            }
        }

        public void SendRequest(byte[] request)
        {
            SendMessage(ZWaveMessage.CreateRequest(this.NodeId, request));
        }

        #region ZWave Command Class Basic
        /// <summary>
        /// Basic Set
        /// </summary>
        /// <param name="value"></param>
        public void Basic_Set(int value)
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.Basic, 
                (byte)Command.BasicSet, 
                byte.Parse(value.ToString())
            });
        }

        /// <summary>
        /// Basic Get
        /// </summary>
        public void Basic_Get()
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.Basic, 
                (byte)Command.BasicGet 
            });
        }
        #endregion

        #region ZWave Command Class Association

        /// <summary>
        /// Association Set
        /// </summary>
        /// <param name="groupid"></param>
        /// <param name="targetnodeid"></param>
        public void Association_Set(byte groupid, byte targetnodeid)
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.Association, 
                (byte)Command.AssociationSet, 
                groupid, 
                targetnodeid 
            });
        }

        /// <summary>
        /// Association Get
        /// </summary>
        /// <param name="groupid"></param>
        public void Association_Get(byte groupid)
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.Association, 
                (byte)Command.AssociationGet, 
                groupid 
            });
        }

        /// <summary>
        /// Association Remove
        /// </summary>
        /// <param name="groupid"></param>
        /// <param name="targetnodeid"></param>
        public void Association_Remove(byte groupid, byte targetnodeid)
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.Association, 
                (byte)Command.AssociationRemove, 
                groupid, 
                targetnodeid 
            });
        }

        /// <summary>
        /// Association Grouping Get
        /// </summary>
        public void Association_GroupingsGet()
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.Association,
                (byte)Command.AssociationGet 
            });
        }

        #endregion

        #region ZWave Command Class Battery

        public void Battery_Get()
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.Battery, 
                (byte)Command.BatteryGet 
            });
        }

        #endregion

        #region ZWave Command Class Wake Up

        public void WakeUp_Get()
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.WakeUp, 
                (byte)Command.WakeUpIntervalGet 
            });
        }

        public void WakeUp_Set(uint interval)
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.WakeUp, 
                (byte)Command.WakeUpIntervalSet,
                (byte)((interval >> 16) & 0xff),
                (byte)((interval >> 8) & 0xff),
                (byte)((interval) & 0xff),
                0x01
            });
        }

        #endregion

        #region ZWave Command Class Configuration

        public void Configuration_ParameterSet(byte parameter, Int32 paramValue)
        {
            int valueLength = 1;
            if (!nodeConfigParamsLength.ContainsKey(parameter))
            {
                Configuration_ParameterGet(parameter);
                int retries = 0;
                while (!nodeConfigParamsLength.ContainsKey(parameter) && retries++ <= 5)
                {
                    Thread.Sleep(1000);
                }
            }
            if (nodeConfigParamsLength.ContainsKey(parameter))
            {
                valueLength = nodeConfigParamsLength[parameter];
            }
            //Console.WriteLine("GOT Parameter Length: " + valuelen);
            //
            //            byte[] value = new byte[valuelen]; // { (byte)intvalue };//BitConverter.GetBytes(Int32.Parse(intvalue));
            byte[] value32 = BitConverter.GetBytes(paramValue);
            Array.Reverse(value32);
            //int curbyte = valuelen - 1;
            //for (int x = 0; x < value32.Length && curbyte >= 0; x++)
            //{
            //    value[curbyte--] = value32[x];
            //}
            ////if (value32[0] != 0 && valuelen > 1)
            ////{
            ////    value[0] = value32[0];
            ////}
            ////
            //Console.WriteLine("\n\n\nCOMPUTED VALUE: " + zp.ByteArrayToString(value32) + "\n->" + zp.ByteArrayToString(BitConverter.GetBytes(intvalue)) + "\n\n");
            //
            byte[] msg = new byte[4 + valueLength];
            msg[0] = (byte)CommandClass.Configuration;
            msg[1] = (byte)Command.ConfigurationSet;
            msg[2] = parameter;
            msg[3] = (byte)valueLength;
            switch (valueLength)
            {
            case 1:
                Array.Copy(value32, 3, msg, 4, 1);
                break;
            case 2:
                Array.Copy(value32, 2, msg, 4, 2);
                break;
            case 4:
                Array.Copy(value32, 0, msg, 4, 4);
                break;
            }
            this.SendRequest(msg);
        }

        public void Configuration_ParameterGet(byte parameter)
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.Configuration, 
                (byte)Command.ConfigurationGet,
                parameter
            });
        }

        #endregion

        #region ZWave Command Class Manufacturer Specific

        public void ManufacturerSpecific_Get()
        {
            byte[] message = new byte[] {
                (byte)MessageHeader.SOF, /* Start Of Frame */
                0x09 /*packet len */,
                (byte)MessageType.Request, /* Type of message */
                0x13 /* func send data */,
                this.NodeId,
                0x02,
                (byte)CommandClass.ManufacturerSpecific,
                (byte)Command.ManufacturerSpecificGet,
                0x05 /* report ?!? */,
                0x01 | 0x04,
                0x00
            }; 
            SendMessage(message);
        }

        #endregion

        #region ZWave Command Class MultiInstance/Channel

        public void MultiInstance_GetCount(byte command_class) // eg. CommandClass.COMMAND_CLASS_SWITCH_BINARY
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.MultiInstance, 
                (byte)Command.MultiInstanceCountGet, // 0x04 = GET, 0x05 = REPORT
                command_class
            });
        }

        public void MultiInstance_SwitchBinaryGet(byte instance)
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.MultiInstance, 
                0x0d, // ??
                0x00, // ??
                instance,
                (byte)CommandClass.SwitchBinary,
                (byte)Command.MultiInstanceGet
            });
        }

        public void MultiInstance_SwitchBinarySet(byte instance, int value)
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.MultiInstance, 
                0x0d, // 
                0x00, // ??
                instance,
                (byte)CommandClass.SwitchBinary,
                (byte)Command.MultiInstanceSet,
                byte.Parse(value.ToString())
            });
        }

        public void MultiInstance_SwitchMultiLevelGet(byte instance)
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.MultiInstance, 
                0x0d, // ??
                0x00, // ??
                instance,
                (byte)CommandClass.SwitchMultilevel,
                (byte)Command.MultiInstanceGet
            });
        }

        public void MultiInstance_SwitchMultiLevelSet(byte instance, int value)
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.MultiInstance, 
                0x0d, // 
                0x00, // ??
                instance,
                (byte)CommandClass.SwitchMultilevel,
                (byte)Command.MultiInstanceSet,
                byte.Parse(value.ToString())
            });
        }

        public void MultiInstance_SensorBinaryGet(byte instance)
        {
            // 0x01, 0x0C, 0x00, 0x13, node, 0x05, 0x60, 0x06,       0x01, 0x31, 0x04, 0x05, 0x03, 0x00
            this.SendRequest(new byte[] { 
                (byte)CommandClass.MultiInstance, 
                0x06, // ??
                instance,
                (byte)CommandClass.SensorBinary,
                0x04 //
            });
        }

        public void MultiInstance_SensorMultiLevelGet(byte instance)
        {
            // 0x01, 0x0C, 0x00, 0x13, node, 0x05, 0x60, 0x06,       0x01, 0x31, 0x04, 0x05, 0x03, 0x00
            this.SendRequest(new byte[] { 
                (byte)CommandClass.MultiInstance, 
                0x06, // ??
                instance,
                (byte)CommandClass.SensorMultilevel,
                0x04 //
            });
        }

        #endregion

        #region ZWave Command Class Meter

        public void Meter_Get(byte scaleType)
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.Meter, 
                (byte)Command.MeterGet,
                scaleType
            });
        }

        public void Meter_SupportedGet()
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.Meter, 
                (byte)Command.MeterSupportedGet
            });
        }

        public void Meter_Reset()
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.Meter, 
                (byte)Command.MeterReset
            });
        }

        #endregion

        #region ZWave Command Class Sensor Binary

        public void SensorBinary_Get()
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.SensorBinary, 
                (byte)Command.SensorBinaryGet
            });
        }

        #endregion

        #region ZWave Command Class Sensor Multilevel

        public void SensorMultiLevel_Get()
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.SensorMultilevel, 
                (byte)Command.SensorMultilevelGet
            });
        }

        #endregion

        #endregion Public members

        #region Private members

        internal byte SendMessage(byte[] message, bool disableCallback = false)
        {
            var msg = new ZWaveMessage() { Node = this, Message = message };
            return zwavePort.SendMessage(msg, disableCallback);
        }

        internal void RaiseUpdateParameterEvent(ZWaveNode node, int pid, ParameterEvent peventtype, object value)
        {
            if (UpdateNodeParameter != null)
            {
                UpdateNodeParameter(node, new UpdateNodeParameterEventArgs() {
                    NodeId = (int)node.NodeId,
                    ParameterId = pid,
                    ParameterEvent = peventtype,
                    Value = value
                });
            }
        }

        #endregion Private members

    }
}
