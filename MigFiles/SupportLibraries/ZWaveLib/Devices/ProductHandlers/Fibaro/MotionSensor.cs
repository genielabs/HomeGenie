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

using ZWaveLib.Devices.ProductHandlers.Generic;
using ZWaveLib.Devices.Values;

namespace ZWaveLib.Devices.ProductHandlers.Fibaro
{
    // Fibaro System Motion Sensor
    // 010F:0800:1001
    public class MotionSensor : Sensor
    {

        public override bool CanHandleProduct(ManufacturerSpecific productspecs)
        {
            return (productspecs.ManufacturerId == "010F" && productspecs.TypeId == "0800" && productspecs.ProductId == "1001");
        }

        public override bool HandleBasicReport(byte[] message)
        {
            bool handled = false;
            //
            //byte cmdLength = message[6];
            byte cmdClass = message[7];
            byte cmdType = message[8];
            //
            if (cmdClass == (byte)CommandClass.SensorMultilevel && cmdType == (byte)Command.SensorMultilevelReport)
            {
                SensorValue sensorval = SensorValue.Parse(message);
                if (sensorval.Parameter == ZWaveSensorParameter.LUMINANCE)
                {
                    sensorval.Value = BitConverter.ToUInt16(new byte[2] { message[12], message[11] }, 0);
                    nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, sensorval.EventType, sensorval.Value);
                    handled = true;
                }
            }
            //
            // if not handled, fallback to Generic Sensor
            if (!handled)
            {
                handled = base.HandleBasicReport(message);
            }
            //
            return handled;
        }

    }
}
