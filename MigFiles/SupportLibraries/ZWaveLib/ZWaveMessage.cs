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
using System.Text;

namespace ZWaveLib
{

    public enum MessageHeader : byte
    {
        SOF = 0x01,
        ACK = 0x06,
        NAK = 0x15,
        CAN = 0x18
    }

    public enum MessageType : byte
    {
        Request = 0x00,
        Response = 0x01,
        None = 0xFF
    }

    public class ZWaveMessageReceivedEventArgs
    {
        public byte[] Message;

        public ZWaveMessageReceivedEventArgs(byte[] msg)
        {
            Message = msg;
        }
    }

    public class ZWaveMessage
    {
        public const int ResendMaxAttempts = 3;
        public byte CallbackId;
        public ZWaveNode Node;
        public byte[] Message;
        public DateTime Timestamp = DateTime.UtcNow;
        public int ResendCount = 0;

        public static byte[] CreateRequest(byte nodeId, byte[] request)
        {
            byte[] header = new byte[] {
                (byte)MessageHeader.SOF, /* Start Of Frame */
                (byte)(request.Length + 7) /*packet len */,
                (byte)MessageType.Request, /* Type of message */
                (byte)Function.SendData /* func send data */,
                nodeId,
                (byte)(request.Length)
            };
            byte[] footer = new byte[] { 0x01 | 0x04, 0x00, 0x00 };
            byte[] message = new byte[header.Length + request.Length + footer.Length];// { 0x01 /* Start Of Frame */, 0x09 /*packet len */, 0x00 /* type req/res */, 0x13 /* func send data */, this.NodeId, 0x02, 0x31, 0x04, 0x01 | 0x04, 0x00, 0x00 };

            System.Array.Copy(header, 0, message, 0, header.Length);
            System.Array.Copy(request, 0, message, header.Length, request.Length);
            System.Array.Copy(footer, 0, message, message.Length - footer.Length, footer.Length);

            return message;
        }

    }

}
