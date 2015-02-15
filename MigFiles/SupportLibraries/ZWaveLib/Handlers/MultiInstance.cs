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
using ZWaveLib.Values;

namespace ZWaveLib.Handlers
{
    public static class MultiInstance
    {

        public static ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;

            byte cmdClass = message[7];
            byte cmdType = message[8];
            byte instanceCmdClass = message[9];

            switch (cmdType)
            {
            case (byte)Command.MultiInstanceReport:
            case (byte)Command.MultiInstaceV2Encapsulated:

                byte[] instanceMessage;
                byte instanceNumber = message[9];

                // if it's a COMMAND_MULTIINSTANCEV2_ENCAP we shift key and val +1 byte
                if (cmdType == (byte)Command.MultiInstaceV2Encapsulated)
                {
                    instanceCmdClass = message[11];
                    instanceMessage = new byte[message.Length - 4];
                    System.Array.Copy(message, 4, instanceMessage, 0, message.Length - 4);
                }
                else
                {
                    instanceCmdClass = message[10];
                    instanceMessage = new byte[message.Length - 3];
                    System.Array.Copy(message, 3, instanceMessage, 0, message.Length - 3);
                }

                switch (instanceCmdClass)
                {
                case (byte)CommandClass.Basic:
                    nodeEvent = Basic.GetEvent(node, instanceMessage);
                    break;
                case (byte)CommandClass.Alarm:
                    nodeEvent = Alarm.GetEvent(node, instanceMessage);
                    break;
                case (byte)CommandClass.SensorAlarm:
                    nodeEvent = SensorAlarm.GetEvent(node, instanceMessage);
                    break;
                case (byte)CommandClass.SceneActivation:
                    nodeEvent = SceneActivation.GetEvent(node, instanceMessage);
                    break;
                case (byte)CommandClass.SwitchBinary:
                    nodeEvent = SwitchBinary.GetEvent(node, instanceMessage);
                    if (nodeEvent != null)
                    {
                        node.RaiseUpdateParameterEvent(instanceNumber, ParameterEvent.MultiinstanceSwitchBinary, nodeEvent.Value);
                    }
                    break;
                case (byte)CommandClass.SwitchMultilevel:
                    nodeEvent = SwitchMultilevel.GetEvent(node, instanceMessage);
                    if (nodeEvent != null)
                    {
                        node.RaiseUpdateParameterEvent(instanceNumber, ParameterEvent.MultiinstanceSwitchMultilevel, nodeEvent.Value);
                    }
                    break;
                case (byte)CommandClass.SensorBinary:
                    nodeEvent = SensorBinary.GetEvent(node, instanceMessage);
                    if (nodeEvent != null)
                    {
                        node.RaiseUpdateParameterEvent(instanceNumber, ParameterEvent.MultiinstanceSensorBinary, nodeEvent.Value);
                    }
                    break;
                case (byte)CommandClass.SensorMultilevel:
                    nodeEvent = SensorMultilevel.GetEvent(node, instanceMessage);
                    if (nodeEvent != null)
                    {
                        node.RaiseUpdateParameterEvent(instanceNumber, ParameterEvent.MultiinstanceSensorMultilevel, nodeEvent.Value);
                    }
                    break;
                case (byte)CommandClass.Meter:
                    nodeEvent = Meter.GetEvent(node, instanceMessage);
                    break;
                }

                if (nodeEvent != null)
                {
                    nodeEvent.Instance = (int)message[9];
                }

                break;

            case (byte)Command.MultiInstanceCountReport:
                byte instanceCount = message[10];
                switch (instanceCmdClass)
                {
                case (byte)CommandClass.SwitchBinary:
                    nodeEvent = new ZWaveEvent(node, ParameterEvent.MultiinstanceSwitchBinaryCount, instanceCount, 0);
                    break;
                case (byte)CommandClass.SwitchMultilevel:
                    nodeEvent = new ZWaveEvent(node, ParameterEvent.MultiinstanceSwitchMultilevelCount, instanceCount, 0);
                    break;
                case (byte)CommandClass.SensorBinary:
                    nodeEvent = new ZWaveEvent(node, ParameterEvent.MultiinstanceSensorBinaryCount, instanceCount, 0);
                    break;
                case (byte)CommandClass.SensorMultilevel:
                    nodeEvent = new ZWaveEvent(node, ParameterEvent.MultiinstanceSensorMultilevelCount, instanceCount, 0);
                    break;
                }
                break;

            }

            return nodeEvent;
        }

        public static void GetCount(ZWaveNode node, byte commandClass)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.MultiInstance, 
                (byte)Command.MultiInstanceCountGet,
                commandClass
            });
        }

        public static void SwitchBinaryGet(ZWaveNode node, byte instance)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.MultiInstance, 
                0x0d, // ?? (MultiInstaceV2Encapsulated ??)
                0x00, // ??
                instance,
                (byte)CommandClass.SwitchBinary,
                (byte)Command.MultiInstanceGet
            });
        }

        public static void SwitchBinarySet(ZWaveNode node, byte instance, int value)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.MultiInstance, 
                0x0d, //  ?? (MultiInstaceV2Encapsulated ??)
                0x00, // ??
                instance,
                (byte)CommandClass.SwitchBinary,
                (byte)Command.MultiInstanceSet,
                byte.Parse(value.ToString())
            });
        }

        public static void SwitchMultiLevelGet(ZWaveNode node, byte instance)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.MultiInstance, 
                0x0d, // ?? (MultiInstaceV2Encapsulated ??)
                0x00, // ??
                instance,
                (byte)CommandClass.SwitchMultilevel,
                (byte)Command.MultiInstanceGet
            });
        }

        public static void SwitchMultiLevelSet(ZWaveNode node, byte instance, int value)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.MultiInstance, 
                0x0d, // ?? (MultiInstaceV2Encapsulated ??)
                0x00, // ??
                instance,
                (byte)CommandClass.SwitchMultilevel,
                (byte)Command.MultiInstanceSet,
                byte.Parse(value.ToString())
            });
        }

        public static void SensorBinaryGet(ZWaveNode node, byte instance)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.MultiInstance, 
                0x06, // ??
                instance,
                (byte)CommandClass.SensorBinary,
                0x04 //
            });
        }

        public static void SensorMultiLevelGet(ZWaveNode node, byte instance)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.MultiInstance, 
                0x06, // ??
                instance,
                (byte)CommandClass.SensorMultilevel,
                0x04 //
            });
        }
    }
}

