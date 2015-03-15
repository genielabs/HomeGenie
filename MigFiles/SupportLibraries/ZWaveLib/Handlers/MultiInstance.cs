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
    public class MultiInstance : ICommandClass
    {
        public byte GetCommandClassId()
        {
            return 0x60;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;

            byte cmdClass = message[0];
            byte cmdType = message[1];
            byte instanceCmdClass = message[2];

            switch (cmdType)
            {
                case (byte)Command.MultiInstanceEncapsulated:
                    return HandleMultiInstanceEncapReport(node, message);
                    break;

                //case (byte) Command.MultiInstanceReport:
                case (byte) Command.MultiChannelEncapsulated:
                    return HandleMultiChannelEncapReport(node, message);
                    //byte[] instanceMessage;
                    //byte instanceNumber = message[2];

                    //// if it's a COMMAND_MULTIINSTANCEV2_ENCAP we shift key and val +1 byte
                    //if (cmdType == (byte) Command.MultiInstaceV2Encapsulated)
                    //{
                    //    instanceCmdClass = message[4];
                    //    instanceMessage = new byte[message.Length - 4]; //TODO
                    //    System.Array.Copy(message, 4, instanceMessage, 0, message.Length - 4);
                    //}
                    //else
                    //{
                    //    instanceCmdClass = message[3];
                    //    instanceMessage = new byte[message.Length - 3]; //TODO
                    //    System.Array.Copy(message, 3, instanceMessage, 0, message.Length - 3);
                    //}

                    //switch (instanceCmdClass)
                    //{
                    //    case (byte) CommandClass.Basic:
                    //        nodeEvent = Basic.GetEvent(node, instanceMessage);
                    //        break;
                    //    case (byte) CommandClass.Alarm:
                    //        nodeEvent = Alarm.GetEvent(node, instanceMessage);
                    //        break;
                    //    case (byte) CommandClass.SensorAlarm:
                    //        nodeEvent = SensorAlarm.GetEvent(node, instanceMessage);
                    //        break;
                    //    case (byte) CommandClass.SceneActivation:
                    //        nodeEvent = SceneActivation.GetEvent(node, instanceMessage);
                    //        break;
                    //    case (byte) CommandClass.SwitchBinary:
                    //        nodeEvent = SwitchBinary.GetEvent(node, instanceMessage);
                    //        if (nodeEvent != null)
                    //        {
                    //            node.RaiseUpdateParameterEvent(instanceNumber, EventParameter.MultiinstanceSwitchBinary,
                    //                nodeEvent.Value);
                    //        }
                    //        break;
                    //    case (byte) CommandClass.SwitchMultilevel:
                    //        nodeEvent = SwitchMultilevel.GetEvent(node, instanceMessage);
                    //        if (nodeEvent != null)
                    //        {
                    //            node.RaiseUpdateParameterEvent(instanceNumber,
                    //                EventParameter.MultiinstanceSwitchMultilevel, nodeEvent.Value);
                    //        }
                    //        break;
                    //    case (byte) CommandClass.SensorBinary:
                    //        nodeEvent = SensorBinary.GetEvent(node, instanceMessage);
                    //        if (nodeEvent != null)
                    //        {
                    //            node.RaiseUpdateParameterEvent(instanceNumber, EventParameter.MultiinstanceSensorBinary,
                    //                nodeEvent.Value);
                    //        }
                    //        break;
                    //    case (byte) CommandClass.SensorMultilevel:
                    //        nodeEvent = SensorMultilevel.GetEvent(node, instanceMessage);
                    //        if (nodeEvent != null)
                    //        {
                    //            node.RaiseUpdateParameterEvent(instanceNumber,
                    //                EventParameter.MultiinstanceSensorMultilevel, nodeEvent.Value);
                    //        }
                    //        break;
                    //    case (byte) CommandClass.Meter:
                    //        nodeEvent = Meter.GetEvent(node, instanceMessage);
                    //        break;
                    //}

                    //if (nodeEvent != null)
                    //{
                    //    nodeEvent.Instance = (int) message[2];
                    //}

                    break;

                case (byte) Command.MultiInstanceCountReport:
                    byte instanceCount = message[3];
                    switch (instanceCmdClass)
                    {
                        case (byte) CommandClass.SwitchBinary:
                            nodeEvent = new ZWaveEvent(node, EventParameter.MultiinstanceSwitchBinaryCount,
                                instanceCount, 0);
                            break;
                        case (byte) CommandClass.SwitchMultilevel:
                            nodeEvent = new ZWaveEvent(node, EventParameter.MultiinstanceSwitchMultilevelCount,
                                instanceCount, 0);
                            break;
                        case (byte) CommandClass.SensorBinary:
                            nodeEvent = new ZWaveEvent(node, EventParameter.MultiinstanceSensorBinaryCount,
                                instanceCount, 0);
                            break;
                        case (byte) CommandClass.SensorMultilevel:
                            nodeEvent = new ZWaveEvent(node, EventParameter.MultiinstanceSensorMultilevelCount,
                                instanceCount, 0);
                            break;
                    }
                    break;

            }

            return nodeEvent;
        }

        private ZWaveEvent HandleMultiInstanceEncapReport(ZWaveNode node, byte[] message)
        {
            if (message.Length < 5)
            {
                Console.WriteLine("\nZWaveLib: MultiInstance encapsulated message ERROR: message is too short: {0}",
                    Utility.ByteArrayToString(message));
                return null;
            }

            byte instanceNumber = message[2];
            var instanceCmdClass = message[3];
            var instanceMessage = new byte[message.Length - 3]; //TODO:
            Array.Copy(message, 3, instanceMessage, 0, message.Length - 3);

            Console.WriteLine("\nZWaveLib: MultiInstance encapsulated message: CmdClass: {0}; message: {1}",
                instanceCmdClass,
                Utility.ByteArrayToString(instanceMessage));

            var cc = CommandClassFactory.GetCommandClass(instanceCmdClass);
            if (cc == null)
            {
                Console.WriteLine("\nZWaveLib: Can't find CommandClass handler for command class {0}",
                    instanceCmdClass);
                return null;
            }
            return cc.GetEvent(node, instanceMessage);
        }

        private ZWaveEvent HandleMultiChannelEncapReport(ZWaveNode node, byte[] message)
        {
            if (message.Length < 6)
            {
                Console.WriteLine("\nZWaveLib: MultiChannel encapsulated message ERROR: message is too short: {0}",
                    Utility.ByteArrayToString(message));
                return null;
            }

            var instanceCmdClass = message[4];
            var instanceMessage = new byte[message.Length - 4]; //TODO
            Array.Copy(message, 4, instanceMessage, 0, message.Length - 4);

            Console.WriteLine("\nZWaveLib: MultiChannel encapsulated message: CmdClass: {0}; message: {1}",
                instanceCmdClass,
                Utility.ByteArrayToString(instanceMessage));

            var cc = CommandClassFactory.GetCommandClass(instanceCmdClass);
            if (cc == null)
            {
                Console.WriteLine("\nZWaveLib: Can't find CommandClass handler for command class {0}",
                    instanceCmdClass);
                return null;
            }
            return cc.GetEvent(node, instanceMessage);
        }

        public static void GetCount(ZWaveNode node, byte commandClass)
        {
            node.SendRequest(new byte[]
            {
                (byte) CommandClass.MultiInstance,
                (byte) Command.MultiInstanceCountGet,
                commandClass
            });
        }

        public static void SwitchBinaryGet(ZWaveNode node, byte instance)
        {
            node.SendRequest(new byte[]
            {
                (byte) CommandClass.MultiInstance,
                0x0d, // ?? (MultiInstaceV2Encapsulated ??)
                0x00, // ??
                instance,
                (byte) CommandClass.SwitchBinary,
                (byte) Command.MultiInstanceGet
            });
        }

        public static void SwitchBinarySet(ZWaveNode node, byte instance, int value)
        {
            node.SendRequest(new byte[]
            {
                (byte) CommandClass.MultiInstance,
                0x0d, //  ?? (MultiInstaceV2Encapsulated ??)
                0x00, // ??
                instance,
                (byte) CommandClass.SwitchBinary,
                (byte) Command.MultiInstanceSet,
                byte.Parse(value.ToString())
            });
        }

        public static void SwitchMultiLevelGet(ZWaveNode node, byte instance)
        {
            node.SendRequest(new byte[]
            {
                (byte) CommandClass.MultiInstance,
                0x0d, // ?? (MultiInstaceV2Encapsulated ??)
                0x00, // ??
                instance,
                (byte) CommandClass.SwitchMultilevel,
                (byte) Command.MultiInstanceGet
            });
        }

        public static void SwitchMultiLevelSet(ZWaveNode node, byte instance, int value)
        {
            node.SendRequest(new byte[]
            {
                (byte) CommandClass.MultiInstance,
                0x0d, // ?? (MultiInstaceV2Encapsulated ??)
                0x00, // ??
                instance,
                (byte) CommandClass.SwitchMultilevel,
                (byte) Command.MultiInstanceSet,
                byte.Parse(value.ToString())
            });
        }

        public static void SensorBinaryGet(ZWaveNode node, byte instance)
        {
            node.SendRequest(new byte[]
            {
                (byte) CommandClass.MultiInstance,
                0x06, // ??
                instance,
                (byte) CommandClass.SensorBinary,
                0x04 //
            });
        }

        public static void SensorMultiLevelGet(ZWaveNode node, byte instance)
        {
            node.SendRequest(new byte[]
            {
                (byte) CommandClass.MultiInstance,
                0x06, // ??
                instance,
                (byte) CommandClass.SensorMultilevel,
                0x04 //
            });
        }
    }
}