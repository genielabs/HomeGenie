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
 *     Author: Alexander Sidorenko <sidorlutiy@gmail.com>
 *     Project Homepage: http://homegenie.it
 */

using System;
using ZWaveLib.Devices;
using ZWaveLib.Devices.ProductHandlers.Generic;

namespace ZWaveLib.Devices.ProductHandlers.ZwaveME
{
    public class ZWaveMeThermostat : Thermostat
    {
        // Z-Wave.Me thermostat
        // 0115:0024:0001
        public override bool CanHandleProduct(ManufacturerSpecific productspecs)
        {
            return (productspecs.ManufacturerId == "0115" && productspecs.TypeId == "0024" && productspecs.ProductId == "0001");
        }

        /*
         * Set temperature in Celcius.
         */
        public override void Thermostat_SetPointSet(SetPointType ptype, int temperature)
        {
            this.nodeHost.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatSetPoint, 
                (byte)Command.ThermostatSetPointSet, 
                (byte)ptype,
                0x01,
                (byte)temperature
            });
        } 
    }
}