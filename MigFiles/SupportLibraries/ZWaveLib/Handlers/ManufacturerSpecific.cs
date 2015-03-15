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

namespace ZWaveLib.Handlers
{
    
    public class ManufacturerSpecificInfo
    {
        public string ManufacturerId { get; set; }
        public string TypeId { get; set; }
        public string ProductId { get; set; }
    }

    public class ManufacturerSpecific : ICommandClass
    {
        public byte GetCommandClassId()
        {
            return 0x72;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;
            byte cmdType = message[1];

            if (message.Length > 7)
            {
                byte[] manufacturerId = new byte[2] { message[2], message[3] };
                byte[] typeId = new byte[2] { message[4], message[5] };
                byte[] productId = new byte[2] { message[6], message[7] };

                var manufacturerSpecs = new ManufacturerSpecificInfo() {
                    TypeId = Utility.ByteArrayToString(typeId).Replace(" ", ""),
                    ProductId = Utility.ByteArrayToString(productId).Replace(" ", ""),
                    ManufacturerId = Utility.ByteArrayToString(manufacturerId).Replace(" ", "")
                };

                nodeEvent = new ZWaveEvent(node, EventParameter.ManufacturerSpecific, manufacturerSpecs, 0);
            }

            return nodeEvent;
        }

        public static void Get(ZWaveNode node)
        {
            byte[] message = new byte[] {
                (byte)MessageHeader.SOF, /* Start Of Frame */
                0x09 /*packet len */,
                (byte)MessageType.Request, /* Type of message */
                0x13 /* func send data */,
                node.NodeId,
                0x02,
                (byte)CommandClass.ManufacturerSpecific,
                (byte)Command.ManufacturerSpecificGet,
                0x05 /* report ?!? */,
                0x01 | 0x04,
                0x00
            }; 
            node.SendMessage(message);
        }

    }
}

