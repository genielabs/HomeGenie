// /*
//     This file is part of HomeGenie Project source code.
//
//     HomeGenie is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     HomeGenie is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with HomeGenie.  If not, see <http://www.gnu.org/licenses/>.  
// */
//
// /*
//  *     Author: Generoso Martello <gene@homegenie.it>
//  *     Project Homepage: http://homegenie.it
//  */
//
//
using System;
using ZWaveLib.Devices;

namespace ZWaveLib.Handlers
{
    public class SwitchMultilevel
    {
        public static ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;
            var cmdClass = (CommandClass)message[7];
            byte cmdType = message[8];
            switch (cmdType)
            {
            case (byte)Command.SwitchMultilevelReport:
            case (byte)Command.SwitchMultilevelSet: // some devices use this instead of report
                int levelValue = (int)message[9];
                nodeEvent = new ZWaveEvent(node, ParameterEvent.Level, (double)levelValue, 0);
                break;
            }
            return nodeEvent;
        }
        
        public static void SetValue(ZWaveNode node, int value)
        {
            // same as basic class
            Basic.SetValue(node, value);
        }

        public static void GetValue(ZWaveNode node)
        {
            // same as basic class
            Basic.GetValue(node);
        }
    }
}

