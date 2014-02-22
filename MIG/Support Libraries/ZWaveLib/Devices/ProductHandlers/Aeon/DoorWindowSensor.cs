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

namespace ZWaveLib.Devices.ProductHandlers.Aeon
{
    class DoorWindowSensor : Generic.Sensor
    {

        public override bool CanHandleProduct(ManufacturerSpecific productspecs)
        {
            return  (productspecs.ManufacturerId == "0086" && productspecs.TypeId == "0002" && productspecs.ProductId == "0004") ||
                    (productspecs.ManufacturerId == "0086" && productspecs.TypeId == "0002" && productspecs.ProductId == "001D");
        }


        public override bool HandleRawMessageRequest(byte[] message)
        {
            byte cmd_length = message[6];
            byte cmd_class = message[7];
            byte cmd_type = message[8];
            //
            if (message.Length > 10 && cmd_length == 0x04 && cmd_class == (byte)CommandClass.COMMAND_CLASS_ALARM && cmd_type == 0x05 && message[9] == 0x00)
            {
                // tampered status
                _nodehost._raiseUpdateParameterEvent(_nodehost, 0, ParameterType.PARAMETER_ALARM_TAMPERED, message[10]);
                return true;
            }
            return false;
        }


        public override bool HandleBasicReport(byte[] message)
        {
            bool handled = false;
            if (message[8] == 0x01)
            {
                // door / window status
                _nodehost._raiseUpdateParameterEvent(_nodehost, 0, ParameterType.PARAMETER_ALARM_DOORWINDOW, message[9]);
                handled = true;
            }
            else
            {
                handled = base.HandleBasicReport(message);
            }
            return handled;
        }



    }
}
