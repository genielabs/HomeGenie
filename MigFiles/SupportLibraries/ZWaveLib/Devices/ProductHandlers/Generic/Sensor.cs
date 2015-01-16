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
        internal ZWaveNode nodeHost = null;

        public void SetNodeHost(ZWaveNode node)
        {
            this.nodeHost = node;
        }

        public virtual bool CanHandleProduct(ManufacturerSpecific productspecs)
        {
            return false; // generic types must return false here
        }

        public virtual bool HandleRawMessageRequest(byte[] message)
        {
            return false;
        }

        public virtual bool HandleBasicReport(byte[] message)
        {
            bool handled = false;
            //
            //byte cmdLength = message[6];
            byte cmdClass = message[7];
            byte cmdType = message[8];
            //
            if (cmdClass == (byte)CommandClass.BASIC && (cmdType == (byte)Command.BASIC_REPORT || cmdType == (byte)Command.BASIC_SET))
            {
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.BASIC, (double)message[9]);
                handled = true;
            }
            else if (cmdClass == (byte)CommandClass.SCENE_ACTIVATION && cmdType == (byte)Command.SCENE_ACTIVATION_SET)
            {
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.GENERIC, (double)message[9]);
                handled = true;
            }
            else if (cmdClass == (byte)CommandClass.SENSOR_BINARY && cmdType == (byte)Command.SENSOR_BINARY_REPORT)
            {
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.GENERIC, message[9]);
                handled = true;
            }
            else if (cmdClass == (byte)CommandClass.SENSOR_MULTILEVEL && cmdType == (byte)Command.SENSOR_MULTILEVEL_REPORT)
            {
                var sensorValue = Sensor.ParseSensorValue(message);
                if (sensorValue.Parameter == ZWaveSensorParameter.UNKNOWN)
                {
                    byte key = message[9];
                    nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.GENERIC, sensorValue.Value);
                    Console.WriteLine("\nUNHANDLED SENSOR PARAMETER TYPE => " + key + "\n");
                }
                else
                {
                    nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, sensorValue.EventType, sensorValue.Value);
                    handled = true;
                }
            }
            else if ((cmdClass == (byte)CommandClass.SENSOR_ALARM && cmdType == (byte)Command.SENSOR_ALARM_REPORT) || (cmdClass == (byte)CommandClass.ALARM && cmdType == (byte)Command.ALARM_REPORT))
            {
                var sensorAlarmValue = Sensor.ParseSensorAlarmValue(message);
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, sensorAlarmValue.EventType, sensorAlarmValue.Value);
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
            //byte cmdLength = message[6];
            byte cmdClass = message[7];
            byte cmdType = message[8];
            //
            if (cmdClass == (byte)CommandClass.METER && cmdType == (byte)Command.METER_REPORT)
            {
                //UNHANDLED: 01 14 00 04 08 04 0E 32 02 21 74 00 00 1E BB 00 00 00 00 00 00 2D
                //           01 14 00 04 00 0A 0E 32 02 21 64 00 00 0C 06 00 00 00 00 00 00 94
                //
                // TODO: should check meter report type (Electric, Gas, Water) and value precision scale
                // TODO: the code below parse always as Electric type 
                double wattsRead = ((double)int.Parse(
                                       message[12].ToString("X2") + message[13].ToString("X2") + message[14].ToString("X2"),
                                       System.Globalization.NumberStyles.HexNumber
                                   )) / 1000D;
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.WATTS, wattsRead);
                processed = true;
            }
            else if (cmdClass == (byte)CommandClass.MULTIINSTANCE)
            {
                //01 0D 00 04 00 2F 07 60 0D 01 00 25 03 FF 6B
                //                     mi ?  in    sb rp vl
                byte instance = message[9];
                byte reportType = message[10];
                //
                //SPI > 01 0F 00 04 00 32 09 60 06 03 31 05 01 2A 02 E4 53
                if (true) // TODO: check against proper command classes SENSOR_BINARY, SENSOR_MULTILEVEL, ...
                {
                    var paramType = ParameterType.MULTIINSTANCE_SENSOR_BINARY;
                    if (reportType == (byte)CommandClass.SENSOR_MULTILEVEL)
                    {
                        paramType = ParameterType.MULTIINSTANCE_SENSOR_MULTILEVEL;
                    }
                    // we assume its a COMMAND_MULTIINSTANCE_REPORT
                    byte key = message[12];
                    double val = ExtractValueFromBytes(message, 14);

                    // if it's a COMMAND_MULTIINSTANCEV2_ENCAP we shift key and val +1 byte
                    if (cmdType == (byte)Command.MULTIINSTANCEV2_ENCAP)
                    {
                        key = message[13];
                        val = ExtractValueFromBytes(message, 15);
                    }
                    //
                    if (key == (byte)ZWaveSensorParameter.TEMPERATURE && message.Length > 16)
                    {
                        if (cmdType == (byte)Command.MULTIINSTANCEV2_ENCAP && message.Length > 18)
                        {
                            val = BitConverter.ToUInt16(new byte[2] { message[18], message[17] }, 0) / 100D;
                        }
                        else
                        {
                            val = ExtractTemperatureFromBytes(message);
                        }
                        //
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, instance, paramType, val);
                        nodeHost.RaiseUpdateParameterEvent(
                            nodeHost,
                            key,
                            ParameterType.TEMPERATURE,
                            val
                        );
                        processed = true;
                    }
                    else if (key == (byte)ZWaveSensorParameter.GENERAL_PURPOSE_VALUE)
                    {
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, instance, paramType, (double)val);
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, key, ParameterType.GENERIC, val);
                        processed = true;
                    }
                    else if (key == (byte)ZWaveSensorParameter.LUMINANCE)
                    {
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, instance, paramType, (double)val);
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, key, ParameterType.LUMINANCE, val);
                        processed = true;
                    }
                    else if (key == (byte)ZWaveSensorParameter.RELATIVE_HUMIDITY)
                    {
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, instance, paramType, (double)val);
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, key, ParameterType.HUMIDITY, val);
                        processed = true;
                    }
                    else if (key == (byte)ZWaveSensorParameter.POWER)
                    {
                        double energy = 0;

                        if (cmdType == (byte)Command.MULTIINSTANCEV2_ENCAP && message.Length > 18)
                        {
                            var e = ((UInt32)message[15]) * 256 * 256 * 256 + ((UInt32)message[16]) * 256 * 256 + ((UInt32)message[17]) * 256 + ((UInt32)message[18]);
                            energy = ((double)e) / 1000.0;
                        }
                        else if (cmdType == (byte)Command.MULTIINSTANCE_REPORT)
                        {
                            var e = ((UInt32)message[14]) * 256 * 256 * 256 + ((UInt32)message[15]) * 256 * 256 + ((UInt32)message[16]) * 256 + ((UInt32)message[17]);
                            energy = ((double)e) / 1000.0;
                        }

                        nodeHost.RaiseUpdateParameterEvent(
                            nodeHost,
                            instance,
                            ParameterType.MULTIINSTANCE_SENSOR_MULTILEVEL,
                            (double)energy
                        );

                        processed = true;
                    }
                    else
                    {
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, instance, paramType, (double)val);
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, key, ParameterType.GENERIC, val);
                        Console.WriteLine("\nUNHANDLED SENSOR PARAMETER TYPE => " + key + "\n");
                    }
                }
            }
            return processed;
        }


        public class SensorValue
        {
            public ParameterType EventType = ParameterType.GENERIC;
            public ZWaveSensorParameter Parameter = ZWaveSensorParameter.UNKNOWN;
            public double Value = 0d;
        }

        public class SensorAlarmValue
        {
            public ParameterType EventType = ParameterType.GENERIC;
            public ZWaveSensorAlarmParameter Parameter = ZWaveSensorAlarmParameter.GENERIC;
            public byte Value = 0x00;
        }

        public static SensorAlarmValue ParseSensorAlarmValue(byte[] message)
        {
            SensorAlarmValue sensorValue = new SensorAlarmValue();
            //
            byte cmdClass = message[7];
            //
            sensorValue.Parameter = ZWaveSensorAlarmParameter.GENERIC;
            sensorValue.Value = message[10]; // CommandClass.COMMAND_CLASS_ALARM
            //
            if (cmdClass == (byte)CommandClass.SENSOR_ALARM)
            {
                sensorValue.Parameter = (ZWaveSensorAlarmParameter)Enum.Parse(
                    typeof(ZWaveSensorAlarmParameter),
                    message[10].ToString()
                );
                sensorValue.Value = message[11];
            }
            //
            switch (sensorValue.Parameter)
            {
            case ZWaveSensorAlarmParameter.CARBONDIOXIDE:
                sensorValue.EventType = ParameterType.ALARM_CARBONDIOXIDE;
                break;
            case ZWaveSensorAlarmParameter.CARBONMONOXIDE:
                sensorValue.EventType = ParameterType.ALARM_CARBONMONOXIDE;
                break;
            case ZWaveSensorAlarmParameter.SMOKE:
                sensorValue.EventType = ParameterType.ALARM_SMOKE;
                break;
            case ZWaveSensorAlarmParameter.HEAT:
                sensorValue.EventType = ParameterType.ALARM_HEAT;
                break;
            case ZWaveSensorAlarmParameter.FLOOD:
                sensorValue.EventType = ParameterType.ALARM_FLOOD;
                break;
            //case ZWaveSensorAlarmParameter.GENERIC:
            default:
                sensorValue.EventType = ParameterType.ALARM_GENERIC;
                break;
            }
            //
            return sensorValue;
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

            SensorValue sensorValue = new SensorValue();
            //
            byte key = message[9];
            double val = ExtractValueFromBytes(message, 11);
            //
            if (key == (byte)ZWaveSensorParameter.TEMPERATURE)
            {
                sensorValue.Parameter = ZWaveSensorParameter.TEMPERATURE;
                sensorValue.Value = ExtractTemperatureFromBytes(message);
                sensorValue.EventType = ParameterType.TEMPERATURE;
            }
            else if (key == (byte)ZWaveSensorParameter.GENERAL_PURPOSE_VALUE)
            {
                sensorValue.Parameter = ZWaveSensorParameter.GENERAL_PURPOSE_VALUE;
                sensorValue.Value = val;
                sensorValue.EventType = ParameterType.GENERIC;
            }
            else if (key == (byte)ZWaveSensorParameter.LUMINANCE)
            {
                sensorValue.Parameter = ZWaveSensorParameter.LUMINANCE;
                sensorValue.Value = val;
                sensorValue.EventType = ParameterType.LUMINANCE;
            }
            else if (key == (byte)ZWaveSensorParameter.RELATIVE_HUMIDITY)
            {
                sensorValue.Parameter = ZWaveSensorParameter.RELATIVE_HUMIDITY;
                sensorValue.Value = val;
                sensorValue.EventType = ParameterType.HUMIDITY;
            }
            else if (key == (byte)ZWaveSensorParameter.POWER)
            {
                // TODO: this might be very buggy.... to be completed
                sensorValue.Parameter = ZWaveSensorParameter.POWER;
                //sensorValue.Value = BitConverter.ToUInt16(new byte[2] { message[12], message[11] }, 0) / 10D;
                sensorValue.Value = ((double)int.Parse(
                    message[12].ToString("X2") + message[13].ToString("X2") + message[14].ToString("X2"),
                    System.Globalization.NumberStyles.HexNumber
                    )) / 1000D;
                sensorValue.EventType = ParameterType.POWER;
            }
            else
            {
                sensorValue.Value = val;
            }
            //
            return sensorValue;
        }


   
        public static double ExtractTemperatureFromBytes(byte[] message)
        {
            double temperature = 0;

            byte[] tmp = new byte[4];
            System.Array.Copy(message, message.Length - 4, tmp, 0, 4);
            message = tmp;

            byte precisionScaleSize = message[0];
            // precisionScaleSize = 0x2A = Fahrenheit
            // precisionScaleSize = 0x22 = Celius

            temperature = ExtractValueFromBytes(message, 1);

            // convert from Fahrenheit to Celsius
            if (precisionScaleSize != 0x22) temperature = ((5.0 / 9.0) * (temperature - 32.0));

            return temperature;
        }

        // adapted from: 
        // https://github.com/dcuddeback/open-zwave/blob/master/cpp/src/command_classes/CommandClass.cpp#L289
        public static double ExtractValueFromBytes(byte[] message, int valueOffset)
        {
            double result = 0;
            try
            {
                byte sizeMask = 0x07, 
                    scaleMask = 0x18, scaleShift = 0x03, 
                    precisionMask = 0xe0, precisionShift = 0x05;
                //
                byte size = (byte)(message[valueOffset-1] & sizeMask);
                byte precision = (byte)((message[valueOffset-1] & precisionMask) >> precisionShift);
                //
                int value = 0;
                byte i;
                for( i=0; i<size; ++i )
                {
                    value <<= 8;
                    value |= (int)message[i+(int)valueOffset];
                }
                // Deal with sign extension. All values are signed
                if( (message[valueOffset] & 0x80) > 0 )
                {
                    // MSB is signed
                    if( size == 1 )
                    {
                        value = (int)((uint)value | 0xffffff00);
                    }
                    else if( size == 2 )
                    {
                        value = (int)((uint)value | 0xffff0000);
                    }
                }
                //
                result = ((double)value / (precision == 0 ? 1 : Math.Pow(10D, precision) ));
            } catch {
            }
            return result;
        }


    }
}
