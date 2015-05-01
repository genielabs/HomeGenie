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
 *     Author: https://github.com/snagytx
 *     Project Homepage: http://homegenie.it
 */
using ZWaveLib.Values;

namespace ZWaveLib.CommandClasses
{
    public class DoorLock : ICommandClass
    {
        public enum Value
        {
            Unsecured = 0x00,
            UnsecuredTimeout = 0x01,
            InsideUnsecured = 0x10,
            InsideUnsecuredTimeout = 0x11,
            OutsideUnsecured = 0x20,
            OutsideUnsecuredTimeout = 0x21,
            Secured = 0xFF
        };

        public enum Alarm
        {
            Locked = 0x01,
            Unlocked = 0x02,
            LockedFromOutside = 0x05,
            UnlockedByUser = 0x06, // with id message[16] <--- TODO: find a way to route this info
            UnatuthorizedUnlock = 0x0F
        };

        public CommandClass GetClassId()
        {
            return CommandClass.DoorLock;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;
            byte cmdType = message[1];
            if (cmdType == (byte)Command.DoorLockReport)
            {
                nodeEvent = new ZWaveEvent(node, EventParameter.DoorLockStatus, message[2], 0);
            }
            return nodeEvent;
        }

        public static void Get(ZWaveNode node)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.DoorLock, 
                (byte)Command.DoorLockGet
            });
        }
        
        public static void Set(ZWaveNode node, Value value)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.DoorLock, 
                (byte)Command.DoorLockSet,
                (byte)value
            });
        }
    }
}
