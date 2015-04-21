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

namespace ZWaveLib.CommandClasses
{
    public class SensorMultilevel : ICommandClass
    {
        public CommandClass GetClassId()
        {
            return CommandClass.SensorMultilevel;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;
            byte cmdType = message[1];
            if (cmdType == (byte)Command.SensorMultilevelReport)
            {
                var sensor = SensorValue.Parse(message);
                if (sensor.Parameter == ZWaveSensorParameter.Unknown)
                {
                    byte key = message[2];
                    nodeEvent = new ZWaveEvent(node, EventParameter.Generic, sensor.Value, 0);
                    Utility.DebugLog(DebugMessageType.Warning, "Unhandled sensor parameter type: " + key);
                }
                else
                {
                    nodeEvent = new ZWaveEvent(node, sensor.EventType, sensor.Value, 0);
                }
            }
            return nodeEvent;
        }
        
        public static void Get(ZWaveNode node)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.SensorMultilevel, 
                (byte)Command.SensorMultilevelGet 
            });
        }

    }
}
