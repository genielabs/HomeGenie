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

    public enum ParameterType
    {
        BASIC,
        MANUFACTURER_SPECIFIC,
        GENERIC,
        WATTS,
        POWER,
        TEMPERATURE,
        HUMIDITY,
        LUMINANCE,
        MOTION,
        ALARM_DOORWINDOW,
        ALARM_GENERIC,
        ALARM_SMOKE,
        ALARM_CARBONMONOXIDE,
        ALARM_CARBONDIOXIDE,
        ALARM_HEAT,
        ALARM_FLOOD,
        ALARM_TAMPERED,
        CONFIGURATION,
        WAKEUP_INTERVAL,
        WAKEUP_NOTIFY,
        ASSOCIATION,
        BATTERY,
        NODE_INFO,
        MULTIINSTANCE_SWITCH_BINARY_COUNT,
        MULTIINSTANCE_SWITCH_BINARY,
        MULTIINSTANCE_SWITCH_MULTILEVEL_COUNT,
        MULTIINSTANCE_SWITCH_MULTILEVEL,
        MULTIINSTANCE_SENSOR_BINARY_COUNT,
        MULTIINSTANCE_SENSOR_BINARY,
        MULTIINSTANCE_SENSOR_MULTILEVEL_COUNT,
        MULTIINSTANCE_SENSOR_MULTILEVEL,
        THERMOSTAT_FAN_MODE,
        THERMOSTAT_FAN_STATE,
        THERMOSTAT_HEATING,
        THERMOSTAT_MODE,
        THERMOSTAT_OPERATING_STATE,
        THERMOSTAT_SETBACK,
        THERMOSTAT_SETPOINT
    }

    public enum ZWaveSensorAlarmParameter
    {
        GENERIC = 0,
        SMOKE,
        CARBONMONOXIDE,
        CARBONDIOXIDE,
        HEAT,
        FLOOD
    }

    public enum ZWaveSensorParameter
    {
        UNKNOWN = -1,
        TEMPERATURE = 1,
        GENERAL_PURPOSE_VALUE = 2,
        LUMINANCE = 3,
        POWER = 4,
        RELATIVE_HUMIDITY = 5,
        VELOCITY = 6,
        DIRECTION = 7,
        ATMOSPHERIC_PRESSURE = 8,
        BAROMETRIC_PRESSURE = 9,
        SOLAR_RADIATION = 10,
        DEW_POINT = 11,
        RAIN_RATE = 12,
        TIDE_LEVEL = 13,
        WEIGHT = 14,
        VOLTAGE = 15,
        CURRENT = 16,
        CO2_LEVEL = 17,
        AIR_FLOW = 18,
        TANK_CAPACITY = 19,
        DISTANCE = 20,
        ANGLE_POSITION = 21
    }

    public class UpdateNodeParameterEventArgs
    {
        public int NodeId { get; internal set; }

        public int ParameterId { get; internal set; }

        public ParameterType ParameterType { get; internal set; }

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
        #region Public fields

        public byte NodeId { get; protected set; }
        public string ManufacturerId { get; protected set; }
        public string TypeId { get; protected set; }
        public string ProductId { get; protected set; }
        public byte BasicClass { get; internal set; }
        public byte GenericClass { get; internal set; }
        public byte SpecificClass { get; internal set; }
        public IZWaveDeviceHandler DeviceHandler = null;

        public delegate void UpdateNodeParameterEventHandler(object sender, UpdateNodeParameterEventArgs upargs);
        public event UpdateNodeParameterEventHandler UpdateNodeParameter;

        public delegate void ManufacturerSpecificResponseEventHandler(object sender, ManufacturerSpecificResponseEventArg mfargs);
        public virtual event ManufacturerSpecificResponseEventHandler ManufacturerSpecificResponse;

        #endregion Public fields

        #region Private fields

        internal ZWavePort zwavePort;
        private Dictionary<byte, int> nodeConfigParamsLength = new Dictionary<byte, int>();

        #endregion Private fields

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

                case (byte)CommandClass.BASIC:
                case (byte)CommandClass.ALARM:
                case (byte)CommandClass.SENSOR_BINARY:
                case (byte)CommandClass.SENSOR_ALARM:
                case (byte)CommandClass.SENSOR_MULTILEVEL:
                case (byte)CommandClass.SWITCH_BINARY:
                case (byte)CommandClass.SWITCH_MULTILEVEL:
                case (byte)CommandClass.SCENE_ACTIVATION:
                case (byte)CommandClass.THERMOSTAT_MODE:
                case (byte)CommandClass.THERMOSTAT_FAN_MODE:
                case (byte)CommandClass.THERMOSTAT_FAN_STATE:
                case (byte)CommandClass.THERMOSTAT_HEATING:
                case (byte)CommandClass.THERMOSTAT_OPERATING_STATE:
                case (byte)CommandClass.THERMOSTAT_SETBACK: 
                case (byte)CommandClass.THERMOSTAT_SETPOINT: 

                    if (this.DeviceHandler != null)
                    {
                        handled = this.DeviceHandler.HandleBasicReport(receivedMessage);
                    }

                    break;
                    
                case (byte)CommandClass.MULTIINSTANCE:
                case (byte)CommandClass.METER:

                    if (messageLength > 10)
                    {
                        if (this.DeviceHandler != null)
                        {
                            handled = this.DeviceHandler.HandleMultiInstanceReport(receivedMessage);
                        }
                    }

                    break;

                case (byte)CommandClass.CONFIGURATION:

                    if (messageLength > 11 && commandType == (byte)Command.CONFIGURATION_REPORT) // CONFIGURATION PARAMETER REPORT  0x06
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
                        RaiseUpdateParameterEvent(this, paramId, ParameterType.CONFIGURATION, paramval);
                        //
                        handled = true;
                    }

                    break;

                case (byte)CommandClass.ASSOCIATION:

                    if (messageLength > 12 && commandType == (byte)Command.ASSOCIATION_REPORT) // ASSOCIATION REPORT 0x03
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
                        RaiseUpdateParameterEvent(this, 1, ParameterType.ASSOCIATION, maxAssociations);
                        RaiseUpdateParameterEvent(this, 2, ParameterType.ASSOCIATION, numAssociations);
                        RaiseUpdateParameterEvent(
                            this,
                            3,
                            ParameterType.ASSOCIATION,
                            groupId + ":" + assocNodes
                        );
                        //
                        handled = true;
                    }

                    break;

                case (byte)CommandClass.WAKE_UP:

                    if (messageLength > 11 && commandType == (byte)Command.WAKEUP_INTERVAL_REPORT) // WAKE UP REPORT 0x06
                    {
                        uint interval = ((uint)receivedMessage[9]) << 16;
                        interval |= (((uint)receivedMessage[10]) << 8);
                        interval |= (uint)receivedMessage[11];
                        //
                        RaiseUpdateParameterEvent(this, 0, ParameterType.WAKEUP_INTERVAL, interval);
                        //
                        handled = true;
                    }
                        // 0x01, 0x08, 0x00, 0x04, 0x00, 0x06, 0x02, 0x84, 0x07, 0x74
                        else if (messageLength > 7 && commandType == (byte)Command.WAKEUP_NOTIFICATION) // AWAKE NOTIFICATION 0x07
                    {
                        RaiseUpdateParameterEvent(this, 0, ParameterType.WAKEUP_NOTIFY, 1);
                        //
                        handled = true;
                    }

                    break;

                case (byte)CommandClass.BATTERY:

                    if (messageLength > 7 && /*command_length == (byte)Command.COMMAND_BASIC_REPORT && */ commandType == 0x03) // Battery Report
                    {
                        RaiseUpdateParameterEvent(this, 0, ParameterType.BATTERY, receivedMessage[9]);
                        //
                        handled = true;
                    }

                    break;

                case (byte)CommandClass.HAIL:

                    this.Basic_Get();
                    handled = true;

                    break;

                case (byte)CommandClass.MANUFACTURER_SPECIFIC:

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
                        CheckDeviceHandler(manufacturerSpecs);
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
                    if (messageLength > 7 && /* cmd_class */ receivedMessage[7] == (byte)CommandClass.MANUFACTURER_SPECIFIC) log = false;
                    if (log) Console.WriteLine("ZWaveLib UNHANDLED message: " + zwavePort.ByteArrayToString(receivedMessage));
                }
            }

            return false;
        }
        
        public void SetGenericHandler()
        {
            if (this.DeviceHandler == null)
            {
                //No specific devicehandler could be found. Use a generic handler
                IZWaveDeviceHandler deviceHandler = null;
                switch (this.GenericClass)
                {
                    case 0x00:
                    // need to query node capabilities
                    byte[] message = new byte[] {
                        0x01,
                        0x04,
                        0x00,
                        (byte)Controller.Command.CMD_GET_NODE_PROTOCOL_INFO,
                        this.NodeId,
                        0x00
                    };
                    SendMessage(message);
                    break;
                    case (byte)ZWaveLib.GenericType.SWITCH_BINARY:
                    deviceHandler = new ProductHandlers.Generic.Switch();
                    break;
                    case (byte)ZWaveLib.GenericType.SWITCH_MULTILEVEL: // eg. dimmer
                    deviceHandler = new ProductHandlers.Generic.Dimmer();
                    break;
                    case (byte)ZWaveLib.GenericType.THERMOSTAT:
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
                    this.DeviceHandler = deviceHandler;
                    this.DeviceHandler.SetNodeHost(this);
                }
            }
        }
        
        public void SetDeviceHandlerFromName(string fullName)
        {
            var type = Assembly.GetExecutingAssembly().GetType(fullName); // full name - i.e. with namespace (perhaps concatenate)
            try
            {
                var deviceHandler = (IZWaveDeviceHandler)Activator.CreateInstance(type);
                //
                this.DeviceHandler = deviceHandler;
                this.DeviceHandler.SetNodeHost(this);
            }
            catch
            {
                // TODO: add error logging 
            }
        }

        public void SendRequest(byte[] msg)
        {
            byte[] header = new byte[] {
                (byte)ZWaveMessageHeader.SOF, /* Start Of Frame */
                (byte)(msg.Length + 7) /*packet len */,
                (byte)ZWaveMessageType.REQUEST, /* Type of message */
                0x13 /* func send data */,
                this.NodeId,
                (byte)(msg.Length)
            };
            byte[] footer = new byte[] { 0x01 | 0x04, 0x00, 0x00 };
            byte[] message = new byte[header.Length + msg.Length + footer.Length];// { 0x01 /* Start Of Frame */, 0x09 /*packet len */, 0x00 /* type req/res */, 0x13 /* func send data */, this.NodeId, 0x02, 0x31, 0x04, 0x01 | 0x04, 0x00, 0x00 };

            System.Array.Copy(header, 0, message, 0, header.Length);
            System.Array.Copy(msg, 0, message, header.Length, msg.Length);
            System.Array.Copy(footer, 0, message, message.Length - footer.Length, footer.Length);

            SendMessage(message);
        }


        
        #region ZWave Command Class Basic

        /// <summary>
        /// Basic Set
        /// </summary>
        /// <param name="value"></param>
        public void Basic_Set(int value)
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.BASIC, 
                (byte)Command.BASIC_SET, 
                byte.Parse(value.ToString())
            });
        }

        /// <summary>
        /// Basic Get
        /// </summary>
        public void Basic_Get()
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.BASIC, 
                (byte)Command.BASIC_GET 
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
                (byte)CommandClass.ASSOCIATION, 
                (byte)Command.ASSOCIATION_SET, 
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
                (byte)CommandClass.ASSOCIATION, 
                (byte)Command.ASSOCIATION_GET, 
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
                (byte)CommandClass.ASSOCIATION, 
                (byte)Command.ASSOCIATION_REMOVE, 
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
                (byte)CommandClass.ASSOCIATION,
                (byte)Command.ASSOCIATION_GET 
            });
        }

        #endregion

        #region ZWave Command Class Battery

        public void Battery_Get()
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.BATTERY, 
                (byte)Command.BATTERY_GET 
            });
        }

        #endregion

        #region ZWave Command Class Wake Up

        public void WakeUp_Get()
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.WAKE_UP, 
                (byte)Command.WAKEUP_INTERVAL_GET 
            });
        }

        public void WakeUp_Set(uint interval)
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.WAKE_UP, 
                (byte)Command.WAKEUP_INTERVAL_SET,
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
            msg[0] = (byte)CommandClass.CONFIGURATION;
            msg[1] = (byte)Command.CONFIGURATION_SET;
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
                (byte)CommandClass.CONFIGURATION, 
                (byte)Command.CONFIGURATION_GET,
                parameter
            });
        }

        #endregion

        #region ZWave Command Class Manufacturer Specific

        public void ManufacturerSpecific_Get()
        {
            byte[] message = new byte[] {
                (byte)ZWaveMessageHeader.SOF, /* Start Of Frame */
                0x09 /*packet len */,
                (byte)ZWaveMessageType.REQUEST, /* Type of message */
                0x13 /* func send data */,
                this.NodeId,
                0x02,
                (byte)CommandClass.MANUFACTURER_SPECIFIC,
                (byte)Command.MANUFACTURERSPECIFIC_GET,
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
                (byte)CommandClass.MULTIINSTANCE, 
                (byte)Command.MULTIINSTANCE_COUNT_GET, // 0x04 = GET, 0x05 = REPORT
                command_class
            });
        }

        public void MultiInstance_SwitchBinaryGet(byte instance)
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.MULTIINSTANCE, 
                0x0d, // ??
                0x00, // ??
                instance,
                (byte)CommandClass.SWITCH_BINARY,
                (byte)Command.MULTIINSTANCE_GET
            });
        }

        public void MultiInstance_SwitchBinarySet(byte instance, int value)
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.MULTIINSTANCE, 
                0x0d, // 
                0x00, // ??
                instance,
                (byte)CommandClass.SWITCH_BINARY,
                (byte)Command.MULTIINSTANCE_SET,
                byte.Parse(value.ToString())
            });
        }

        public void MultiInstance_SwitchMultiLevelGet(byte instance)
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.MULTIINSTANCE, 
                0x0d, // ??
                0x00, // ??
                instance,
                (byte)CommandClass.SWITCH_MULTILEVEL,
                (byte)Command.MULTIINSTANCE_GET
            });
        }

        public void MultiInstance_SwitchMultiLevelSet(byte instance, int value)
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.MULTIINSTANCE, 
                0x0d, // 
                0x00, // ??
                instance,
                (byte)CommandClass.SWITCH_MULTILEVEL,
                (byte)Command.MULTIINSTANCE_SET,
                byte.Parse(value.ToString())
            });
        }

        public void MultiInstance_SensorBinaryGet(byte instance)
        {
            // 0x01, 0x0C, 0x00, 0x13, node, 0x05, 0x60, 0x06,       0x01, 0x31, 0x04, 0x05, 0x03, 0x00
            this.SendRequest(new byte[] { 
                (byte)CommandClass.MULTIINSTANCE, 
                0x06, // ??
                instance,
                (byte)CommandClass.SENSOR_BINARY,
                0x04 //
            });
        }

        public void MultiInstance_SensorMultiLevelGet(byte instance)
        {
            // 0x01, 0x0C, 0x00, 0x13, node, 0x05, 0x60, 0x06,       0x01, 0x31, 0x04, 0x05, 0x03, 0x00
            this.SendRequest(new byte[] { 
                (byte)CommandClass.MULTIINSTANCE, 
                0x06, // ??
                instance,
                (byte)CommandClass.SENSOR_MULTILEVEL,
                0x04 //
            });
        }

        #endregion

        #region ZWave Command Class Meter

        public virtual void Meter_Get()
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.METER, 
                (byte)Command.METER_GET
            });
        }
        public virtual void Meter_SupportedGet()
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.METER, 
                (byte)Command.METER_SUPPORTED_GET
            });
        }
        public virtual void Meter_Reset()
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.METER, 
                (byte)Command.METER_RESET
            });
        }

        #endregion
        
        #region ZWave Command Class Sensor Binary

        public virtual void SensorBinary_Get()
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.SENSOR_BINARY, 
                (byte)Command.SENSOR_BINARY_GET
            });
        }

        #endregion

        #region ZWave Command Class Sensor Multilevel

        public virtual void SensorMultiLevel_Get()
        {
            this.SendRequest(new byte[] { 
                (byte)CommandClass.SENSOR_MULTILEVEL, 
                (byte)Command.SENSOR_MULTILEVEL_GET
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

        internal void RaiseUpdateParameterEvent(
            ZWaveNode node,
            int pid,
            ParameterType peventtype,
            object value
            )
        {
            if (UpdateNodeParameter != null)
            {
                UpdateNodeParameter(
                    node,
                    new UpdateNodeParameterEventArgs() {
                    NodeId = (int)node.NodeId,
                    ParameterId = pid,
                    ParameterType = peventtype,
                    Value = value
                }
                );
            }
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

        private void CheckDeviceHandler(ManufacturerSpecific manufacturerspecs)
        {
            //if (this.DeviceHandler == null)
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
                            this.DeviceHandler = deviceHandler;
                            this.DeviceHandler.SetNodeHost(this);
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

        #endregion Private members

    }
}
