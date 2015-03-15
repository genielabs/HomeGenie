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
 *     Author: Alexandre Schnegg <alexandre.schnegg@gmail.com>
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: http://homegenie.it
 */

using System;
using ZWaveLib.Values;
using System.Collections.Generic;

namespace ZWaveLib.Handlers
{
    public class UserCode : ICommandClass
    {
        public byte GetCommandClassId()
        {
            return 0x63;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;
            byte cmdType = message[1];
            if (cmdType == (byte)Command.UserCodeReport)
            {
                var reportedUserCode = UserCodeValue.Parse(message);
                var userCode = GetUserCodeData(node);
                userCode.TagCode = reportedUserCode.TagCode;
                userCode.UserId = reportedUserCode.UserId;
                userCode.UserIdStatus = reportedUserCode.UserIdStatus;
                nodeEvent = new ZWaveEvent(node, EventParameter.UserCode, reportedUserCode, 0);
            }
            return nodeEvent;
        }

        public static void Set(ZWaveNode node, UserCodeValue newUserCode)
        {
            var userCode = GetUserCodeData(node);
            userCode.TagCode = newUserCode.TagCode;
            userCode.UserId = newUserCode.UserId;
            userCode.UserIdStatus = newUserCode.UserIdStatus;
            List<byte> message = new List<byte>();
            message.Add((byte)CommandClass.UserCode);
            message.Add((byte)Command.UserCodeSet);
            message.Add(userCode.UserId);
            message.Add(userCode.UserIdStatus);
            message.AddRange(userCode.TagCode);
            node.SendRequest(message.ToArray());
        }

        public static UserCodeValue GetUserCode(ZWaveNode node)
        {
            return GetUserCodeData(node);
        }

        private static UserCodeValue GetUserCodeData(ZWaveNode node)
        {
            if (!node.Data.ContainsKey("UserCode"))
            {
                node.Data.Add("UserCode", new UserCodeValue());
            }
            return (UserCodeValue)node.Data["UserCode"];
        }

    }
}