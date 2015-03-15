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
using System.Collections.Generic;
using System.Linq;

namespace ZWaveLib.Handlers
{
    public class WakeUp : ICommandClass
    {
        public byte GetCommandClassId()
        {
            return 0x84;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;
            byte cmdType = message[1];
            switch (cmdType)
            {
            case (byte)Command.WakeUpIntervalReport:
                if (message.Length > 4)
                {
                    uint interval = ((uint)message[2]) << 16;
                    interval |= (((uint)message[3]) << 8);
                    interval |= (uint)message[4];
                    nodeEvent = new ZWaveEvent(node, EventParameter.WakeUpInterval, interval, 0);
                }
                break;
            case (byte)Command.WakeUpNotification:
                    // Resend queued messages while node was asleep
                var wakeUpResendQueue = GetResendQueueData(node);
                for (int m = 0; m < wakeUpResendQueue.Count; m++)
                {
                    node.SendMessage(wakeUpResendQueue[m]);
                }
                wakeUpResendQueue.Clear();
                nodeEvent = new ZWaveEvent(node, EventParameter.WakeUpNotify, 1, 0);
                break;
            }
            return nodeEvent;
        }

        public static void Get(ZWaveNode node)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.WakeUp, 
                (byte)Command.WakeUpIntervalGet 
            });
        }

        public static void Set(ZWaveNode node, uint interval)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.WakeUp, 
                (byte)Command.WakeUpIntervalSet,
                (byte)((interval >> 16) & 0xff),
                (byte)((interval >> 8) & 0xff),
                (byte)((interval) & 0xff),
                0x01
            });
        }

        public static void ResendOnWakeUp(ZWaveNode node, byte[] msg)
        {
            int minCommandLength = 8;
            if (msg.Length >= minCommandLength)
            {
                byte[] command = new byte[minCommandLength];
                Array.Copy(msg, 0, command, 0, minCommandLength);
                // discard any message having same header and command (first 8 bytes = header + command class + command)
                var wakeUpResendQueue = GetResendQueueData(node);
                for (int i = wakeUpResendQueue.Count - 1; i >= 0; i--)
                {
                    byte[] queuedCommand = new byte[minCommandLength];
                    Array.Copy(wakeUpResendQueue[i], 0, queuedCommand, 0, minCommandLength);
                    if (queuedCommand.SequenceEqual(command))
                    {
                        wakeUpResendQueue.RemoveAt(i);
                    }
                }
                wakeUpResendQueue.Add(msg);
            }
        }

        private static List<byte[]> GetResendQueueData(ZWaveNode node)
        {
            if (!node.Data.ContainsKey("WakeUpResendQueue"))
            {
                node.Data.Add("WakeUpResendQueue", new List<byte[]>());
            }
            return (List<byte[]>)node.Data["WakeUpResendQueue"];
        }
    }
}

