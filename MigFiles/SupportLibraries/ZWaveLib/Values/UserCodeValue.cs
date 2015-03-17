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
using System.Collections;

namespace ZWaveLib.Values
{
    public class UserCodeValue
    {
        public byte UserId;
        public byte UserIdStatus;
        public byte[] TagCode = new byte[10];

        public UserCodeValue(byte userId, byte userIdStatus, byte[] tagCode)
        {
            this.UserId = userId;
            this.UserIdStatus = userIdStatus;
            tagCode.CopyTo(this.TagCode, 0);
        }

        public UserCodeValue()
        {
            UserId = 0;
            UserIdStatus = 0;
            TagCode = null;
        }

        public static UserCodeValue Parse(byte[] message)
        {
            byte cmdClass = message[7];
            byte cmdType = message[8];
            UserCodeValue userCode = new UserCodeValue();

            if (cmdClass == (byte)CommandClassType.UserCode && ((byte)CommandType.UserCodeSet == cmdType || (byte)CommandType.UserCodeReport == cmdType))
            {
                userCode.UserId = message[9];
                userCode.UserIdStatus = message[10];
                userCode.TagCode = new byte[10];
                for (int i = 0; i < 10; i++)
                {
                    userCode.TagCode[i] = message[11 + i];
                }
            }
            return userCode;
        }

        public string TagCodeToHexString()
        {
            return Utility.ByteArrayToHexString(TagCode);
        }
    }
}