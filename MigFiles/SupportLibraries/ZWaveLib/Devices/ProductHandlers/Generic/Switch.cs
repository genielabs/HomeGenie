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
using System.Threading;

namespace ZWaveLib.Devices.ProductHandlers.Generic
{
    public class Switch : Sensor
    {
        internal double levelValue = 0;

        public override bool HandleBasicReport(byte[] message)
        {
            bool handled = false;
            byte cmdClass = message[7];
            //
            levelValue = (int)message[9];
            //
            if (cmdClass == (byte)CommandClass.Basic || cmdClass == (byte)CommandClass.SwitchBinary || cmdClass == (byte)CommandClass.SwitchMultilevel)
            {
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.LEVEL, levelValue);
                switch (cmdClass)
                {
                case (byte)CommandClass.SwitchBinary:
                    nodeHost.RaiseUpdateParameterEvent(nodeHost, 1, ParameterType.MULTIINSTANCE_SWITCH_BINARY, (double)levelValue);
                    break;
                case (byte)CommandClass.SwitchMultilevel:
                    nodeHost.RaiseUpdateParameterEvent(nodeHost, 1, ParameterType.MULTIINSTANCE_SWITCH_MULTILEVEL, (double)levelValue);
                    break;
                }
                handled = true;
            }
            else
            {
                handled = base.HandleBasicReport(message);
            }
            return handled;
        }

        public override bool HandleMultiInstanceReport(byte[] message)
        {
            if (message.Length < 12)
                return false; // we need at least 15 bytes long message for further processing
            //
            bool processed = false;
            //
            //byte cmdLength = message[6];
            byte cmdClass = message[7];
            byte cmdType = message[8];
            byte instanceCmdClass = message[9];
            //
            if ((instanceCmdClass == (byte)CommandClass.SwitchBinary || instanceCmdClass == (byte)CommandClass.SwitchMultilevel) && cmdType == (byte)Command.MultiInstanceCountReport)
            {
                // 01 0A 00 04 00 30 04 60 05 25 02 87
                byte inst_count = message[10];
                if (instanceCmdClass == (byte)CommandClass.SwitchBinary)
                {
                    nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.MULTIINSTANCE_SWITCH_BINARY_COUNT, inst_count);
                }
                else
                {
                    nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.MULTIINSTANCE_SWITCH_MULTILEVEL_COUNT, inst_count);
                }
                processed = true;
            }
            else if (cmdClass == (byte)CommandClass.MultiInstance && message.Length > 12)
            {
                //01 0D 00 04 00 2F 07 60 0D 01 00 25 03 FF 6B
                //                     mi ?  in    sb rp vl
                byte instance = message[9];
                byte cmd = message[11];
                byte type = message[12];
                //
                if ((cmd == (byte)CommandClass.SwitchBinary || cmd == (byte)CommandClass.SwitchMultilevel) && type == (byte)Command.BasicReport) // 0x03 ??
                {
                    byte value = message[13];
                    //
                    if (cmd == (byte)CommandClass.SwitchBinary)
                    {
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, instance, ParameterType.MULTIINSTANCE_SWITCH_BINARY, (double)value);
                    }
                    else
                    {
                        nodeHost.RaiseUpdateParameterEvent(nodeHost, instance, ParameterType.MULTIINSTANCE_SWITCH_MULTILEVEL, (double)value);
                    }
                    //_nodehost._raiseUpdateParameterEvent(_nodehost, instance, ParameterType.PARAMETER_BASIC, (double)value);
                    //
                    processed = true;
                }

            }
            //
            if (!processed)
            {
                processed = base.HandleMultiInstanceReport(message);
            }
            return processed;
        }

        public virtual void On()
        {
            Level = 0xFF;
        }

        public virtual void Off()
        {
            Level = 0x00;
        }

        public virtual int Level
        {
            get
            {
                return (int)levelValue;
            }
            set
            {
                levelValue = value;
                nodeHost.Basic_Set((int)levelValue);
            }
        }
    }
}