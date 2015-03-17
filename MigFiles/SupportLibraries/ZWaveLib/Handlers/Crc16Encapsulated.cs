using System;
using System.Linq;

namespace ZWaveLib.Handlers
{
    public class Crc16Encapsulated : ICommandClass
    {
        public CommandClass GetClassId()
        {
            return CommandClass.Crc16Encapsulated;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent zevent = null;
            byte cmdType = message[1];
            switch (cmdType) {
            case 0x01:
                zevent = GetCrc16EncapEvent(node, message);
                break;
            }
            return zevent;
        }

        #region Private Helpers

        private ZWaveEvent GetCrc16EncapEvent(ZWaveNode node, byte[] message)
        {
            // calculate CRC
            var messageToCheckLength = message.Length - 2;
            byte[] messageCrc = new byte[2];
            Array.Copy(message, messageToCheckLength, messageCrc, 0, 2);
            byte[] toCheck = new byte[messageToCheckLength];
            Array.Copy(message, 0, toCheck, 0, messageToCheckLength);

            short crcToCheck = CalculateCrcCcit(toCheck);
            byte[] x = Int16ToBytes(crcToCheck);
            if (!x.SequenceEqual(messageCrc)) {
                Console.WriteLine("\nZWaveLib: bad CRC in message {0}. CRC is {1} but should be {2}", 
                    Utility.ByteArrayToString(message), Utility.ByteArrayToString(x), Utility.ByteArrayToString(messageCrc));
                return null;
            }

            byte[] encapsulatedMessage = new byte[message.Length - 2 - 2];
            Array.Copy(message, 2, encapsulatedMessage, 0, message.Length - 2 - 2);

            return ProcessEncapsulatedMessage(node, encapsulatedMessage);
        }

        private ZWaveEvent ProcessEncapsulatedMessage(ZWaveNode node, byte[] encapMessage)
        {
            Console.WriteLine("\nZWaveLib: CRC16 encapsulated message: {0}", Utility.ByteArrayToString(encapMessage));

            byte cmdClass = encapMessage[0];

            ZWaveEvent nodeEvent = null;
            var cc = CommandClassFactory.GetCommandClass(cmdClass);
            if (cc == null)
            {
                Console.WriteLine("\nZWaveLib: Can't find CommandClass handler for command class {0}", cmdClass);
                return null;
            }
            nodeEvent = cc.GetEvent(node, encapMessage);
            return nodeEvent;
        }

        private byte[] Int16ToBytes(Int16 value)
        {
            var bytes = BitConverter.GetBytes(value);
            if(BitConverter.IsLittleEndian)
            {
                var t = bytes[0];
                bytes[0] = bytes[1];
                bytes[1] = t;
            }
            return bytes;
        }
            
        private short CalculateCrcCcit(byte[] args) {
            int crc = 0x1D0F;
            int polynomial = 0x1021;
            foreach (byte b in args) {
                for (int i = 0; i < 8; i++) {
                    bool bit = ((b >> (7 - i) & 1) == 1);
                    bool c15 = ((crc >> 15 & 1) == 1);
                    crc <<= 1;
                    // If coefficient of bit and remainder polynomial = 1 xor crc with polynomial
                    if (c15 ^ bit) {
                        crc ^= polynomial;
                    }
                }
            }

            crc &= 0xffff;
            return (short) crc;
        }

        #endregion

    }
}