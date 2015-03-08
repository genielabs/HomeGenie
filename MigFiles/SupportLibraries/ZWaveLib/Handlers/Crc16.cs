using System;
using System.Linq;
using ZWaveLib.Values;

namespace ZWaveLib
{
    public static class Crc16
    {
        public static ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;
            byte cmdType = message[8];

            switch (cmdType) {
            case 0x01:
                return GetCrc16EncapEvent(node, message);
                break;
            }
            return null;
        }

        private static ZWaveEvent GetCrc16EncapEvent(ZWaveNode node, byte[] message)
        {
            // calculate CRC
            byte[] messageCrc = new byte[2];
            Array.Copy(message, message.Length - 2 - 1, messageCrc, 0, 2);
            byte[] toCheck = new byte[message.Length - 7 - 2 - 1];
            Array.Copy(message, 7, toCheck, 0, message.Length - 7 - 2 - 1);

            short crcToCheck = CalculateCrcCcit(toCheck);
            byte[] x = Int16ToBytes(crcToCheck);
            if (!x.SequenceEqual(messageCrc)) {
                Console.WriteLine("\nZWaveLib: bad CRC in message {0}. CRC is {1} but should be {2}", 
                    Utility.ByteArrayToString(message), Utility.ByteArrayToString(x), Utility.ByteArrayToString(messageCrc));
                return null;
            }

            byte[] encapsulatedMessage = new byte[message.Length - 9 - 2 - 1];
            Array.Copy(message, 9, encapsulatedMessage, 0, message.Length - 9 - 2 - 1);

            return ProcessEncapsulatedMessage(node, encapsulatedMessage);
        }

        private static ZWaveEvent ProcessEncapsulatedMessage(ZWaveNode node, byte[] encapMessage)
        {
            Console.WriteLine("\nZWaveLib: CRC16 encapsulated message: {0}", Utility.ByteArrayToString(encapMessage));

            // TODO: properly handle encapsulated message

            byte cmdClass = encapMessage[0];
            byte cmdType = encapMessage[1];

            ZWaveEvent nodeEvent = null;
            if (cmdClass == (byte)CommandClass.SensorBinary && cmdType == (byte)Command.SensorBinaryReport)
            {
                nodeEvent = new ZWaveEvent(node, EventParameter.Generic, encapMessage[2], 0);
            }
            return nodeEvent;
        }

        private static byte[] Int16ToBytes(Int16 value)
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
            
        private static short CalculateCrcCcit(byte[] args) {
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
    }
}