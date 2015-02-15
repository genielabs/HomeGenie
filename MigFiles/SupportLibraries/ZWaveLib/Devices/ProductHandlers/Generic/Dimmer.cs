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
    public class Dimmer : Switch
    {
        /*
        public override bool HandleMultiLevelReport(byte[] message)
        {
            bool handled = base.HandleMultiLevelReport(message);
            if (!handled)
            {
                byte command_class = message[7];
                byte command_type = message[8];
                if (command_class == (byte)CommandClass.COMMAND_CLASS_SWITCH_MULTILEVEL && command_type == (byte)Command.COMMAND_BASIC_REPORT) // 0x03
                {
                    Level = message[9];
                    _nodehost._raiseUpdateParameterEvent(_nodehost, 0, ParameterType.PARAMETER_BASIC, (double)message[9]);
                    handled = true;
                }
            }
            return handled;
        }
        */

        public override void On()
        {
            Level = 0x63; // On
        }

        public override void Off()
        {
            Level = 0x00; // Off
        }

        public override int Level
        {
            get
            {
                return (int)levelValue;
            }
            set
            {
                levelValue = value;
                if (levelValue > 0x63) levelValue = 0x63; // 0 to 99 for dimmer type 
                Handlers.Basic.Set(nodeHost, (int)levelValue);
            }
        }

    }
}
