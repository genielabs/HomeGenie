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
 *     Project Homepage: http://homegenie.it
 */

using System;
using System.Collections;

namespace ZWaveLib.Devices.Values
{
    public class UserCodeValue
    {
        public byte userId;
        public byte userIdStatus;
        public byte[] tagCode=new byte[10];

        public UserCodeValue(byte userId,byte userIdStatus, byte[] tagCode)
        {
            this.userId=userId;
            this.userIdStatus=userIdStatus;
            tagCode.CopyTo(this.tagCode,0);
        }

        public UserCodeValue()
        {
            userId = 0;
            userIdStatus = 0;
            tagCode = null;
        }
        public static UserCodeValue Parse(byte[] message)
        {
            byte cmdClass = message[7];
            byte cmdType = message[8];
            UserCodeValue userCode = new UserCodeValue();

            if (cmdClass == (byte)CommandClass.UserCode && ((byte)Command.UserCodeSet==cmdType || (byte)Command.UserCodeReport==cmdType))
            {
                userCode.userId = message[9];
                userCode.userIdStatus = message[10];
                userCode.tagCode = new byte[10];
                for (int i = 0; i < 10; i++)
                {
                    userCode.tagCode[i] = message[11 + i];
                }
            }
            return userCode;
        }

        public byte[] GetMessage()
        {
            ArrayList tempMessage=new ArrayList();
            tempMessage.Add((byte)CommandClass.UserCode);
            tempMessage.Add((byte)Command.UserCodeSet);
            tempMessage.Add(userId);
            tempMessage.Add(userIdStatus);
            tempMessage.AddRange(tagCode);
            return (byte[])tempMessage.ToArray(typeof(byte));
        }

        public string TagCodeToHexString()
        {
            return Utility.ByteArrayToHexString(tagCode);
        }
    }
}

