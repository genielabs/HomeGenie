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

namespace ZWaveLib.Devices.Values
{
    public class Utility
    {
        
        public static double ExtractTemperatureFromBytes(byte[] message)
        {
            double temperature = 0;
            int scale = 0;
            byte[] tmp = new byte[4];
            System.Array.Copy(message, message.Length - 4, tmp, 0, 4);
            message = tmp;

            temperature = ExtractValueFromBytes(message, 1, out scale);

            // TODO: should use "scale" value returned from ExtractValueFromBytes
            // 0x2A = Fahrenheit
            // 0x22 = Celius
            byte precisionScaleSize = message[0];

            // convert from Fahrenheit to Celsius
            if (precisionScaleSize != 0x22) temperature = ((5.0 / 9.0) * (temperature - 32.0));

            return temperature;
        }

        // adapted from: 
        // https://github.com/dcuddeback/open-zwave/blob/master/cpp/src/command_classes/CommandClass.cpp#L289
        public static double ExtractValueFromBytes(byte[] message, int valueOffset, out int scale)
        {
            double result = 0;
            scale = 0;
            try
            {
                byte sizeMask = 0x07, 
                scaleMask = 0x18, scaleShift = 0x03, 
                precisionMask = 0xe0, precisionShift = 0x05;
                //
                byte size = (byte)(message[valueOffset-1] & sizeMask);
                byte precision = (byte)((message[valueOffset-1] & precisionMask) >> precisionShift);
                scale = (int)((message[valueOffset-1] & scaleMask) >> scaleShift);
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
                result = ((double)value / (precision == 0 ? 1 : Math.Pow(10D, precision) ));
            } catch {
                // TODO: report/handle exception
            }
            return result;
        }

    }
}

