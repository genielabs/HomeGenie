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

namespace ZWaveLib.Handlers
{
    public class SwitchMultilevel : ICommandClass
    {
        public byte GetCommandClassId()
        {
            return 0x26;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;
            byte cmdType = message[1];
            if (cmdType == (byte)Command.SwitchMultilevelReport || cmdType == (byte)Command.SwitchMultilevelSet) // some devices use this instead of report
            {
                int levelValue = (int)message[2];
                nodeEvent = new ZWaveEvent(node, EventParameter.Level, (double)levelValue, 0);
            }
            return nodeEvent;
        }
        
        public static void Set(ZWaveNode node, int value)
        {
            // same as basic class
            Basic.Set(node, value);
        }

        public static void Get(ZWaveNode node)
        {
            // same as basic class
            Basic.Get(node);
        }
    }
}

