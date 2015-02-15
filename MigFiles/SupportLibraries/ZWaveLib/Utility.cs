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

namespace ZWaveLib
{
    public class ZWaveValue
    {
        public double Value;
        public int Scale;
        public int Precision;
        public int Size = 1;
    }

    public class Utility
    {
        private static byte sizeMask = 0x07, 
            scaleMask = 0x18, scaleShift = 0x03, 
            precisionMask = 0xe0, precisionShift = 0x05;

        public static byte GetPrecisionScaleSize(int precision, int scale, int size)
        {
            return (byte)((precision << precisionShift) | (scale << scaleShift) | size);
        }

        public static byte[] GetValueBytes(double v, int precision, int scale, int size)
        {
            List<byte> valueBytes = new List<byte>();
            valueBytes.Add(Utility.GetPrecisionScaleSize(precision, scale, size));
            int intValue = (int)(v * Math.Pow(10D, precision));
            int shift = (size - 1) << 3;
            for(int i = size; i > 0; --i, shift -= 8)
            {
                valueBytes.Add((byte)(intValue >> shift));
            }
            return valueBytes.ToArray();
        }

        // adapted from: 
        // https://github.com/dcuddeback/open-zwave/blob/master/cpp/src/command_classes/CommandClass.cpp#L289
        public static ZWaveValue ExtractValueFromBytes(byte[] message, int valueOffset)
        {
            ZWaveValue result = new ZWaveValue();
            try
            {
                byte size = (byte)(message[valueOffset-1] & sizeMask);
                byte precision = (byte)((message[valueOffset-1] & precisionMask) >> precisionShift);
                int scale = (int)((message[valueOffset-1] & scaleMask) >> scaleShift);
                //
                result.Size = size;
                result.Precision = precision;
                result.Scale = scale;
                //
                int value = 0;
                byte i;
                for( i=0; i<size; ++i )
                {
                    value <<= 8;
                    value |= (int)message[i+(int)valueOffset];
                }
                // Deal with sign extension. All values are signed
                if( (message[valueOffset] & 0x80) > 0 )
                {
                    // MSB is signed
                    if( size == 1 )
                    {
                        value = (int)((uint)value | 0xffffff00);
                    }
                    else if( size == 2 )
                    {
                        value = (int)((uint)value | 0xffff0000);
                    }
                }
                //
                result.Value = ((double)value / (precision == 0 ? 1 : Math.Pow(10D, precision) ));
            } catch {
                // TODO: report/handle exception
            }
            return result;
        }
        
        public static String ByteArrayToString(byte[] message)
        {
            String returnValue = String.Empty;
            foreach (byte b in message)
            {
                returnValue += b.ToString("X2") + " ";
            }
            return returnValue.Trim();
        }
                
        //from
        //http://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa
        public static string ByteArrayToHexString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex.Replace("-", "");
        }
        //from
        //http://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa
        public static byte[] HexStringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

    }
}
