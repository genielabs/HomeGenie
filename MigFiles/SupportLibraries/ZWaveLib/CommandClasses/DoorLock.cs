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

namespace ZWaveLib.CommandClasses
{
    public class DoorLock : ICommandClass
    {
        public CommandClass GetClassId()
        {
            return CommandClass.DoorLock;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;
            byte cmdType = message[1];

            if (cmdType == (byte)Command.DoorLock_Report) {
                int lockState;

                if (message[2] == 0xFF)
                    lockState = 6;
                else
                    lockState = System.Convert.ToInt32(message[2].ToString("X2"));

                if (lockState > 6) {
                    lockState = 7;
                }

                string resp;

                if (lockState == 0) {
                    resp = "Unlocked";
                }
                else if (lockState == 6)
                {
                    resp = "Locked";
                }
                else {
                    resp = "Unknown";
                }

                var messageEvent = new ZWaveEvent(node, EventParameter.DoorLockStatus, resp, 0);
                node.RaiseUpdateParameterEvent(messageEvent);

            }else if (cmdType == (byte) Command.DoorLock_Configuration_Report) {
                node.SendRequest(new byte[] { 
                        0x63, 
                        0x04
                });

            }

            /*
            if (cmdType == (byte)Command.BasicReport || cmdType == (byte)Command.BasicSet)
            {
                int levelValue = (int)message[2];
                nodeEvent = new ZWaveEvent(node, EventParameter.Level, (double)levelValue, 0);
            }
            */
            return nodeEvent;
        }

        public static void Set(ZWaveNode node, int value)
        {

            node.SendRequest(new byte[] { 
                (byte)CommandClass.DoorLock, 
                (byte)0x01, //DoorLockCmd_Set
                byte.Parse(value.ToString())
            });
            
//            messageEvent = new ZWaveEvent(node, EventParameter.Level, System.Convert.ToDouble(value), 0);
//            node.RaiseUpdateParameterEvent(messageEvent);
        }

        public static void Get(ZWaveNode node)
        {

            node.SendRequest(new byte[] { 
                (byte)CommandClass.DoorLock, 
                (byte)0x02, //DoorLockCmd_Get
            });

//            var messageEvent = new ZWaveEvent(node, EventParameter.DoorLockStatus, "---", 0);
//            node.RaiseUpdateParameterEvent(messageEvent);

        }

        public static void Unlock(ZWaveNode node)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.Basic, 
                (byte)Command.BasicGet 
            });
        }
    }
}
