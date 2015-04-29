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
 *     Author: https://github.com/snagytx
 *     Project Homepage: http://homegenie.it
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZWaveLib
{
    public enum SecurityCommand
    {
        SupportedGet = 0x02,
        SupportedReport = 0x03,
        SchemeGet = 0x04,
        SchemeReport = 0x05,
        NetworkKeySet = 0x06,
        NetworkKeyVerify = 0x07,
        SchemeInherit = 0x08,
        NonceGet = 0x40,
        NonceReport = 0x80,
        MessageEncap = 0x81,
        MessageEncapNonceGet = 0xc1
    }


    public enum SecurityScheme : byte
    {
        SchemeZero = 0x00,
        SchemeOne = 0x01,
        SchemeReserved1 = 0x02,
        SchemeReserved2 = 0x04,
        SchemeReserved3 = 0x08,
        SchemeReserved4 = 0x10,
        SchemeReserved5 = 0x20,
        SchemeReserved6 = 0x40,
        SchemeReserved7 = 0x80
    }
}
