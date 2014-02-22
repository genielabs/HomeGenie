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

namespace ZWaveLib.Devices.ProductHandlers.Generic
{
    public class Sensor : IZWaveDeviceHandler
    {
        internal ZWaveNode _nodehost = null;
        private double temperature = 0;
        private double luminance = 0;
        private double humidity = 0;
        private double generic = 0;
		private double power = 0;

        public void SetNodeHost(ZWaveNode node)
        {
            this._nodehost = node;
        }

        public virtual bool CanHandleProduct(ManufacturerSpecific productspecs)
        {
            return false; // generic types must return false here
        }

        public virtual bool HandleRawMessageRequest(byte[] message)
        {
            return false;
        }
        //
        // 01 0D 00 04 00 1C 07 9C 02 00 05 FF 00 00 89
        //  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
        //  0  1  2  3  4  5  6  7  8  9 10 11 12 13 14
        //
        // 01 0F 00 04 00 18 09 71 05 07 00 00 FF 07 02 00
        // 01 0F 00 04 00 18 09 71 05 07 FF 00 FF 07 02 00
        //
        public virtual bool HandleBasicReport(byte[] message)
        {
            bool handled = false;
            //
            byte cmd_length = message[6];
            byte cmd_class = message[7];
            byte cmd_type = message[8];
            //
			if (cmd_class == (byte)CommandClass.COMMAND_CLASS_BASIC && (cmd_type == 0x03 || cmd_type == 0x01)) {
				_nodehost._raiseUpdateParameterEvent (_nodehost, 0, ParameterType.PARAMETER_BASIC, (double)message [9]);
				handled = true;
			}
            else if (cmd_class == (byte)CommandClass.COMMAND_CLASS_SCENE_ACTIVATION && cmd_type == 0x01)
            {
                _nodehost._raiseUpdateParameterEvent(_nodehost, 0, ParameterType.PARAMETER_GENERIC, (double)message[9]);
                handled = true;
            }
            else if (cmd_class == (byte)CommandClass.COMMAND_CLASS_SENSOR_BINARY && cmd_type == 0x03)
            {
                _nodehost._raiseUpdateParameterEvent(_nodehost, 0, ParameterType.PARAMETER_GENERIC, message[9]);
                handled = true;
            }
            else if (cmd_class == (byte)CommandClass.COMMAND_CLASS_SENSOR_MULTILEVEL && cmd_type == 0x05)
			{
                SensorValue sensorval = Sensor.ParseSensorValue(message);
                if (sensorval.Parameter == ZWaveSensorParameter.UNKNOWN)
                {
                    byte key = message[9];
                    _nodehost._raiseUpdateParameterEvent(_nodehost, 0, ParameterType.PARAMETER_GENERIC, sensorval.Value);
                    Console.WriteLine("\nUNHANDLED SENSOR PARAMETER TYPE => " + key + "\n");
                }
                else
                {
                    _nodehost._raiseUpdateParameterEvent(_nodehost, 0, sensorval.EventType, sensorval.Value);
                    handled = true;
                }
			}
			else if ((cmd_class == (byte)CommandClass.COMMAND_CLASS_SENSOR_ALARM && cmd_type == 0x02) || (cmd_class == (byte)CommandClass.COMMAND_CLASS_ALARM && cmd_type == 0x05))
            {
                SensorAlarmValue sensoralarmval = Sensor.ParseSensorAlarmValue(message);
                _nodehost._raiseUpdateParameterEvent(_nodehost, 0, sensoralarmval.EventType, sensoralarmval.Value);
                handled = true;
            }
            return handled;
        }

        public virtual bool HandleMultiInstanceReport(byte[] message)
        {
            if (message.Length <= 14) return false; // we need at least 15 bytes long message for further processing
            //
            bool processed = false;
            //
            byte cmd_length = message[6];
            byte cmd_class = message[7];
            byte cmd_type = message[8];
            //
            if (cmd_class == (byte)CommandClass.COMMAND_CLASS_METER)
            {
                //UNHANDLED: 01 14 00 04 08 04 0E 32 02 21 74 00 00 1E BB 00 00 00 00 00 00 2D
                //           01 14 00 04 00 0A 0E 32 02 21 64 00 00 0C 06 00 00 00 00 00 00 94
                //
                if (message.Length > 14 && message[4] == 0x00)
                {
                    // CLASS METER
                    //
                    double watts_read = ((double)int.Parse(message[12].ToString("X2") + message[13].ToString("X2") + message[14].ToString("X2"), System.Globalization.NumberStyles.HexNumber)) / 1000D;
                    _nodehost._raiseUpdateParameterEvent(_nodehost, 0, ParameterType.PARAMETER_WATTS, watts_read);
                    //
                    Logger.Log(LogLevel.REPORT, " * Received METER report from node " + _nodehost.NodeId); // + " (" + _nodehost.Description + ")");
                    Logger.Log(LogLevel.REPORT, " * " + _nodehost.NodeId + ">   kW " + Math.Round(watts_read, 3) /*+ "    Counter kW " + Math.Round(meter_count, 10)*/ );
                    //
                    processed = true;
                }
                else if (message.Length > 14 && message[4] == 0x08)
                {
                    //TODO: complete here...
                    processed = true;
                }
            }
            else if (cmd_class == (byte)CommandClass.COMMAND_CLASS_MULTIINSTANCE)
            {
                //01 0D 00 04 00 2F 07 60 0D 01 00 25 03 FF 6B
                //                     mi ?  in    sb rp vl
                byte instance = message[9];
                byte report_type = message[10];
                byte cmd = message[11];
                byte type = message[12];
                //
                //SPI > 01 0F 00 04 00 32 09 60 06 03 31 05 01 2A 02 E4 53
                if (true) // TODO: check against proper command classes SENSOR_BINARY, SENSOR_MULTILEVEL, ...
                {
                    ParameterType paramtype = ParameterType.PARAMETER_MULTIINSTANCE_SENSOR_BINARY;
                    if (report_type == (byte)CommandClass.COMMAND_CLASS_SENSOR_MULTILEVEL)
                    {
                        paramtype = ParameterType.PARAMETER_MULTIINSTANCE_SENSOR_MULTILEVEL;
                    }
					// we assume its a COMMAND_MULTIINSTANCE_REPORT
                    byte key = message[12];
                    byte val = message[14];
					
					// if it's a COMMAND_MULTIINSTANCEV2_ENCAP we shift key and val +1 byte
					if (cmd_type == (byte)Command.COMMAND_MULTIINSTANCEV2_ENCAP)
					{
						key = message[13];
						val = message[15];
					}
                    //
                    //double val = (double)int.Parse(message[14].ToString("X2"), System.Globalization.NumberStyles.HexNumber) / 1000D;
                    //
                    if (key == (byte)ZWaveSensorParameter.TEMPERATURE)
                    {
						if (cmd_type == (byte)Command.COMMAND_MULTIINSTANCEV2_ENCAP && message.Length > 18)
						{
							temperature = BitConverter.ToUInt16(new byte[2] {message[18] , message[17] }, 0) / 100D;
						}
						else
						{
                        	temperature = ExtractTemperatureFromBytes(message);
						}
                        //
                        //if (temperature <= 212) // this fixes a bug with my HSM-100 sensor
                        {
                            _nodehost._raiseUpdateParameterEvent(_nodehost, instance, paramtype, temperature);
                            _nodehost._raiseUpdateParameterEvent(_nodehost, key, ParameterType.PARAMETER_TEMPERATURE, temperature);
                        }
                        //
                        processed = true;
                    }
                    else if (key == (byte)ZWaveSensorParameter.GENERAL_PURPOSE_VALUE)
                    {
                        generic = val;
                        //
                        _nodehost._raiseUpdateParameterEvent(_nodehost, instance, paramtype, (double)val);
                        _nodehost._raiseUpdateParameterEvent(_nodehost, key, ParameterType.PARAMETER_GENERIC, humidity);
                        //
                        processed = true;
                    }
                    else if (key == (byte)ZWaveSensorParameter.LUMINANCE)
                    {
                        luminance = val;
                        //
                        _nodehost._raiseUpdateParameterEvent(_nodehost, instance, paramtype, (double)val);
                        _nodehost._raiseUpdateParameterEvent(_nodehost, key, ParameterType.PARAMETER_LUMINANCE, luminance);
                        //
                        processed = true;
                    }
                    else if (key == (byte)ZWaveSensorParameter.RELATIVE_HUMIDITY)
                    {
                        humidity = val;
                        //
                        _nodehost._raiseUpdateParameterEvent(_nodehost, instance, paramtype, (double)val);
                        _nodehost._raiseUpdateParameterEvent(_nodehost, key, ParameterType.PARAMETER_HUMIDITY, humidity);
                        //
                        processed = true;
                    }
                    else if (key == (byte)ZWaveSensorParameter.POWER)
                    {
                        double energy = 0;

                        if (cmd_type == (byte)Command.COMMAND_MULTIINSTANCEV2_ENCAP && message.Length > 18)
                        {
                            var e = ((UInt32)message[15]) * 256 * 256 * 256 + ((UInt32)message[16]) * 256 * 256 + ((UInt32)message[17]) * 256 + ((UInt32)message[18]);
                            energy = ((double)e) / 1000.0;
                        }
                        else if (cmd_type == (byte)Command.COMMAND_MULTIINSTANCE_REPORT)
                        {
                            var e = ((UInt32)message[14]) * 256 * 256 * 256 + ((UInt32)message[15]) * 256 * 256 + ((UInt32)message[16]) * 256 + ((UInt32)message[17]);
                            energy = ((double)e) / 1000.0;
                        }

                        _nodehost._raiseUpdateParameterEvent(_nodehost, instance, ParameterType.PARAMETER_MULTIINSTANCE_SENSOR_MULTILEVEL, (double)energy);

                        processed = true;
                    }
                    else
                    {
                        _nodehost._raiseUpdateParameterEvent(_nodehost, instance, paramtype, (double)val);
                        _nodehost._raiseUpdateParameterEvent(_nodehost, key, ParameterType.PARAMETER_GENERIC, val);
                        Console.WriteLine("\nUNHANDLED SENSOR PARAMETER TYPE => " + key + "\n");
                    }
                }
            }
            return processed; 
        }


        public class SensorValue
        {
            public ParameterType EventType = ParameterType.PARAMETER_GENERIC;
            public ZWaveSensorParameter Parameter = ZWaveSensorParameter.UNKNOWN;
            public double Value = 0d;
        }
        public class SensorAlarmValue
        {
            public ParameterType EventType = ParameterType.PARAMETER_GENERIC;
            public ZWaveSensorAlarmParameter Parameter = ZWaveSensorAlarmParameter.GENERIC;
            public byte Value = 0x00;
        }

        public static SensorAlarmValue ParseSensorAlarmValue(byte[] message)
        {
            SensorAlarmValue sensorval = new SensorAlarmValue();
            //
            byte cmd_class = message[7];
            //
            sensorval.Parameter = ZWaveSensorAlarmParameter.GENERIC;
            sensorval.Value = message[10]; // CommandClass.COMMAND_CLASS_ALARM
            //
            if (cmd_class == (byte)CommandClass.COMMAND_CLASS_SENSOR_ALARM)
            {
                sensorval.Parameter = (ZWaveSensorAlarmParameter)Enum.Parse(typeof(ZWaveSensorAlarmParameter), message[10].ToString());
                sensorval.Value = message[11];
            }
            //
            switch (sensorval.Parameter)
            {
                case ZWaveSensorAlarmParameter.CARBONDIOXIDE:
                    sensorval.EventType = ParameterType.PARAMETER_ALARM_CARBONDIOXIDE;
                    break;
                case ZWaveSensorAlarmParameter.CARBONMONOXIDE:
                    sensorval.EventType = ParameterType.PARAMETER_ALARM_CARBONMONOXIDE;
                    break;
                case ZWaveSensorAlarmParameter.SMOKE:
                    sensorval.EventType = ParameterType.PARAMETER_ALARM_SMOKE;
                    break;
                case ZWaveSensorAlarmParameter.HEAT:
                    sensorval.EventType = ParameterType.PARAMETER_ALARM_HEAT;
                    break;
                case ZWaveSensorAlarmParameter.FLOOD:
                    sensorval.EventType = ParameterType.PARAMETER_ALARM_FLOOD;
                    break;
                //case ZWaveSensorAlarmParameter.GENERIC:
                default:
                    sensorval.EventType = ParameterType.PARAMETER_ALARM_GENERIC;
                    break;
            }
            //
            return sensorval;
        }

        public static SensorValue ParseSensorValue(byte[] message)
        {
            // ...
            /*
             * 
    SPI > 01 0C 00 04 00 16 06 31 05 03 0A 00 43 99
    SPO < 06
    ZWaveLib UNHANDLED message: 01 0C 00 04 00 16 06 31 05 03 0A 00 43 99
    SPI > 01 0C 00 04 00 16 06 31 05 05 01 27 00 F0
    SPO < 06
    ZWaveLib UNHANDLED message: 01 0C 00 04 00 16 06 31 05 05 01 27 00 F0
    SPI > 01 0C 00 04 00 16 06 31 05 01 2A 03 3B C0
    SPO < 06
    ZWaveLib UNHANDLED message: 01 0C 00 04 00 16 06 31 05 01 2A 03 3B C0					 
             * 
            */

            SensorValue sensorval = new SensorValue();
            //
            byte key = message[9];
            byte val = message[11];
            //
            //double val = (double)int.Parse(message[14].ToString("X2"), System.Globalization.NumberStyles.HexNumber) / 1000D;
            //
            if (key == (byte)ZWaveSensorParameter.TEMPERATURE)
            {
                sensorval.Parameter = ZWaveSensorParameter.TEMPERATURE;
                sensorval.Value = ExtractTemperatureFromBytes(message);
                sensorval.EventType = ParameterType.PARAMETER_TEMPERATURE;
            }
            else if (key == (byte)ZWaveSensorParameter.GENERAL_PURPOSE_VALUE)
            {
                sensorval.Parameter = ZWaveSensorParameter.GENERAL_PURPOSE_VALUE;
                sensorval.Value = val;
                sensorval.EventType = ParameterType.PARAMETER_GENERIC;
            }
            else if (key == (byte)ZWaveSensorParameter.LUMINANCE)
            {
                sensorval.Parameter = ZWaveSensorParameter.LUMINANCE;
                sensorval.Value = val;
                sensorval.EventType = ParameterType.PARAMETER_LUMINANCE;
            }
            else if (key == (byte)ZWaveSensorParameter.RELATIVE_HUMIDITY)
            {
                sensorval.Parameter = ZWaveSensorParameter.RELATIVE_HUMIDITY;
                sensorval.Value = val;
                sensorval.EventType = ParameterType.PARAMETER_HUMIDITY;
            }
            else if (key == (byte)ZWaveSensorParameter.POWER)
            {
                sensorval.Parameter = ZWaveSensorParameter.POWER;
                sensorval.Value = BitConverter.ToUInt16(new byte[2] { message[12], message[11] }, 0) / 10D;
                sensorval.EventType = ParameterType.PARAMETER_WATTS;
            }
            else
            {
                sensorval.Value = val;
            }
            //
            return sensorval;
        }


        // code from http://sourceforge.net/p/homegenie/discussion/general/thread/3d98093f/#677f
        public static double ExtractTemperatureFromBytes(byte[] message)
        {
            double temperature = 0; // (double)int.Parse(message[12].ToString("X2") + message[13].ToString("X2") + message[14].ToString("X2"), System.Globalization.NumberStyles.HexNumber) / 1000D;

            byte[] tmp = new byte[4];
            System.Array.Copy(message, message.Length - 4, tmp, 0, 4);
            message = tmp;

            byte PrecisionScaleSize = message[0];

            temperature = ((double)(((((int)message[1]) << 8)) | ((int)message[2]))) / 10;

            // F to C
            if (PrecisionScaleSize != 0x22)
                temperature = ((5.0 / 9.0) * (temperature - 32.0));

            return temperature;
        }


    }
}
