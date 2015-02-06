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
using ZWaveLib.Devices.Values;

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
            if (cmdClass == (byte)CommandClass.Basic && (cmdType == (byte)Command.BasicReport || cmdType == (byte)Command.BasicSet))
            {
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.LEVEL, (double)message[9]);
                handled = true;
            }
            else if (cmdClass == (byte)CommandClass.SceneActivation && cmdType == (byte)Command.SceneActivationSet)
            {
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.GENERIC, (double)message[9]);
                handled = true;
            }
            else if (cmdClass == (byte)CommandClass.SensorBinary && cmdType == (byte)Command.SensorBinaryReport)
            {
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.GENERIC, message[9]);
                handled = true;
            }
            else if (cmdClass == (byte)CommandClass.SensorMultilevel && cmdType == (byte)Command.SensorMultilevelReport)
            {
                var sensor = SensorValue.Parse(message);
                if (sensor.Parameter == ZWaveSensorParameter.UNKNOWN)
                {
                    byte key = message[9];
                    nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.GENERIC, sensor.Value);
                    Console.WriteLine("\nUNHANDLED SENSOR PARAMETER TYPE => " + key + "\n");
                }
                else
                {
                    nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, sensor.EventType, sensor.Value);
                    handled = true;
                }
            }
            else if ((cmdClass == (byte)CommandClass.SensorAlarm && cmdType == (byte)Command.SensorAlarmReport) || (cmdClass == (byte)CommandClass.Alarm && cmdType == (byte)Command.AlarmReport))
            {
                var alarm = AlarmValue.Parse(message);
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, alarm.EventType, alarm.Value);
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
            if (cmdClass == (byte)CommandClass.Meter && cmdType == (byte)Command.MeterReport)
            {
                // TODO: should check meter report type (Electric, Gas, Water) and value precision / scale
                // TODO: the code below parse always as Electric type 
                EnergyValue energy = EnergyValue.Parse(message);
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, energy.EventType, energy.Value);
                processed = true;
            }
            else if (cmdClass == (byte)CommandClass.MultiInstance)
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
                    if (reportType == (byte)CommandClass.SensorMultilevel)
                    {
                        paramType = ParameterType.MULTIINSTANCE_SENSOR_MULTILEVEL;
                    }
                    // we assume its a COMMAND_MULTIINSTANCE_REPORT
                    byte key = message[12];
                    ZWaveValue zvalue = Utility.ExtractValueFromBytes(message, 14);

                    // if it's a COMMAND_MULTIINSTANCEV2_ENCAP we shift key and val +1 byte
                    if (cmdType == (byte)Command.MultiInstaceV2Encapsulated)
                    {
                        key = message[13];
                        zvalue = Utility.ExtractValueFromBytes(message, 15);
                    }
                    //
                    if (key == (byte)ZWaveSensorParameter.TEMPERATURE && message.Length > 16)
                    {
                        if (cmdType == (byte)Command.MultiInstaceV2Encapsulated && message.Length > 18)
                        {
                            zvalue.Value = BitConverter.ToUInt16(new byte[2] { message[18], message[17] }, 0) / 100D;
                        }
                        else
                        {
                            zvalue.Value = Utility.ExtractTemperatureFromBytes(message);
                        }
                        //
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, instance, paramType, zvalue);
                        nodeHost.RaiseUpdateParameterEvent(
                            nodeHost,
                            key,
                            ParameterType.SENSOR_TEMPERATURE,
                            zvalue
                        );
                        processed = true;
                    }
                    else if (key == (byte)ZWaveSensorParameter.GENERAL_PURPOSE_VALUE)
                    {
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, instance, paramType, zvalue.Value);
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, key, ParameterType.GENERIC, zvalue);
                        processed = true;
                    }
                    else if (key == (byte)ZWaveSensorParameter.LUMINANCE)
                    {
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, instance, paramType, zvalue.Value);
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, key, ParameterType.SENSOR_LUMINANCE, zvalue);
                        processed = true;
                    }
                    else if (key == (byte)ZWaveSensorParameter.RELATIVE_HUMIDITY)
                    {
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, instance, paramType, zvalue.Value);
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, key, ParameterType.SENSOR_HUMIDITY, zvalue);
                        processed = true;
                    }
                    else if (key == (byte)ZWaveSensorParameter.POWER)
                    {
                        // TODO: verify if it's possible to use EnergyValue class
                        double energy = 0;

                        if (cmdType == (byte)Command.MultiInstaceV2Encapsulated && message.Length > 18)
                        {
                            var e = ((UInt32)message[15]) * 256 * 256 * 256 + ((UInt32)message[16]) * 256 * 256 + ((UInt32)message[17]) * 256 + ((UInt32)message[18]);
                            energy = ((double)e) / 1000.0;
                        }
                        else if (cmdType == (byte)Command.MultiInstanceReport)
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
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, instance, paramType, zvalue.Value);
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, key, ParameterType.GENERIC, zvalue);
                        Console.WriteLine("\nUNHANDLED SENSOR PARAMETER TYPE => " + key + "\n");
                    }
                }
            }
            return processed;
        }   

    }
}
