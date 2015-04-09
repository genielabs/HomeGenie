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

using ZWaveLib.Values;

namespace ZWaveLib.CommandClasses
{
    public class Alarm : ICommandClass
    {
        public CommandClass GetClassId()
        {
            return CommandClass.Alarm;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;
            byte cmdType = message[1];
            if (cmdType == (byte)Command.AlarmReport)
            {
                var alarm = AlarmValue.Parse(message);
                // Translate generic alarm into specific Door Lock event values if node is an entry control type device
                if (node.GenericClass == (byte)GenericType.EntryControl && alarm.EventType == EventParameter.AlarmGeneric)
                {
                    int value = System.Convert.ToInt16(alarm.Value);
                    alarm.EventType = EventParameter.DoorLockStatus;
                    if (value == 1)
                    {
                        alarm.Text = "Locked";
                    }
                    else if (value == 2)
                    {
                        alarm.Text = "Unlocked";
                    }
                    else if (value == 5)
                    {
                        alarm.Text = "Locked from outside";
                    }
                    else if (value == 6)
                    {
                        alarm.Text = "Unlocked by user " + System.Convert.ToInt32(message[16].ToString("X2"), 16);
                    }
                    else if (value == 16)
                    {
                        alarm.Text = "Unatuthorized unlock attempted";
                    }
                }

                if (alarm.Text.Length > 0)
                    nodeEvent = new ZWaveEvent(node, alarm.EventType, alarm.Text, 0);
                else
                    nodeEvent = new ZWaveEvent(node, alarm.EventType, alarm.Value, 0);
            }
            return nodeEvent;
        }
    
    }
}

