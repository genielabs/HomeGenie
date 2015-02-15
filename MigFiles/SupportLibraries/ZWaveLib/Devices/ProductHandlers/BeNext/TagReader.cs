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
 *     Author: Alexandre Schnegg <alexandre.schnegg@gmail.com>
 *     Project Homepage: http://homegenie.it
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZWaveLib.Devices.Values;

namespace ZWaveLib.Devices.ProductHandlers.BeNext
{
    public class TagReader : Generic.Switch
    {
        internal UserCodeValue userCodeValue;
        public override bool CanHandleProduct(ManufacturerSpecific productspecs)
        {
            return (productspecs.ManufacturerId == "008A" && productspecs.TypeId == "0007" && (productspecs.ProductId == "0100" ||productspecs.ProductId == "0101"));
        }

        public override bool HandleBasicReport(byte[] message)
        {
            bool handled = false;
            byte cmdClass = message[7];
            byte cmdType = message[8];
            //
            levelValue = (int)message[9];
            //
            if (cmdClass == (byte)CommandClass.UserCode && cmdType == (byte)Command.UserCodeReport)
            {
                userCodeValue=UserCodeValue.Parse(message);
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterEvent.UserCode, userCodeValue);
                handled = true;
            }
            else
            {
                handled = base.HandleBasicReport(message);
            }
            return handled;
        }

        public UserCodeValue UserCode
        {
            get
            {
                return userCodeValue;
            }
            set
            {
                userCodeValue = value;
                nodeHost.SendRequest(userCodeValue.GetMessage());
            }
        }

    }
}
