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

    public enum DebugMessageType
    {
        Information,
        Warning,
        Error
    }

    public class Utility
    {
        
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

        public static byte[] AppendByteToArray(byte[] byteArray, byte data)
        {
            int pos = -1;
            if (byteArray != null) pos = Array.IndexOf(byteArray, data);
            if (pos == -1)
            {
                if (byteArray != null)
                {
                    Array.Resize(ref byteArray, byteArray.Length + 1);
                }
                else
                {
                    byteArray = new byte[1];
                }
                byteArray[byteArray.Length - 1] = data;
            }
            return byteArray;
        }

        public static void DebugLog(DebugMessageType dtype, string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            if (dtype == DebugMessageType.Warning)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
            }
            else if (dtype == DebugMessageType.Error)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
            }
            //Console.Write("[" + DateTime.Now.ToString("HH:mm:ss.ffffff") + "] ");
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

    }
}

