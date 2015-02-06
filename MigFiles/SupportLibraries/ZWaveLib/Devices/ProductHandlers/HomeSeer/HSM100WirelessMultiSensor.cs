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
using System.Text;

namespace ZWaveLib.Devices.ProductHandlers.HomeSeer
{
    public class HSM100WirelessMultiSensor : Generic.Sensor
    {

        public override bool CanHandleProduct(ManufacturerSpecific productspecs)
        {
            return (productspecs.ManufacturerId == "001E" && productspecs.TypeId == "0002" && productspecs.ProductId == "0001");
        }

        public override bool HandleBasicReport(byte[] message)
        {
            if (message[8] == 0x01) // && message[9] != 0x00) // MOTION ON
            {
                // Motion Detected
                //  
                // message[9] == 0xFF (or > 0x00)             --> MOTION ON
                // message[9] == 0x00                         --> MOTION OFF
                //
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.SENSOR_MOTION, (double)message[9]);
                return true;
            }
            else if (message[8] == 0x03)
            {
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.ALARM_GENERIC, message[9]);
                return true;
            }
            //else if (message[8] == 0x01 && message[9] == 0x00) // MOTION OFF
            //{
            //    mynode._raiseUpdateParameterEvent(mynode, 0, ParameterType.PARAMETER_MOTION, 0);
            //}
            return false;
        }

    }
}
