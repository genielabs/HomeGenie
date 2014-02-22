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

namespace ZWaveLib.Devices.ProductHandlers.Generic
{
    public class Meter : IZWaveDeviceHandler
    {
        protected ZWaveNode _nodehost;

        public void SetNodeHost(ZWaveNode node)
        {
            _nodehost = node;
            _nodehost._raiseUpdateParameterEvent(_nodehost, 0, ParameterType.PARAMETER_WATTS, 0);
        }



        public virtual bool CanHandleProduct(ManufacturerSpecific productspecs)
        {
            return false; // generic types must return false here
        }


        public virtual bool HandleRawMessageRequest(byte[] message)
        {
            return false;
        }


        public virtual bool HandleBasicReport(byte[] message)
        {
            return false;
        }


        public virtual bool HandleMultiInstanceReport(byte[] message)
        {
            //UNHANDLED: 01 14 00 04 08 04 0E 32 02 21 74 00 00 1E BB 00 00 00 00 00 00 2D
            //                       ^^
            bool processed = false;
            //
            byte command_class = message[7];
            //
            if (command_class == (byte)CommandClass.COMMAND_CLASS_METER)
            {
                if (message.Length > 14 && message[4] == 0x00)
                {
                    // CLASS METER
                    //
                    double watts_read = ((double)int.Parse(message[12].ToString("X2") + message[13].ToString("X2") + message[14].ToString("X2"), System.Globalization.NumberStyles.HexNumber)) / 1000D;
                    _nodehost._raiseUpdateParameterEvent(_nodehost, 0, ParameterType.PARAMETER_WATTS, watts_read);
                    //
                    Logger.Log(LogLevel.REPORT, " * Received METER report from node " + _nodehost.NodeId); // + " (" + _nodehost.Description + ")");
                    Logger.Log(LogLevel.REPORT, " * " + _nodehost.NodeId + ">   kW " + Math.Round(watts_read, 3) /*+ "    Counter kW " + Math.Round(meter_count, 10)*/ );
                    //
                    processed = true;
                }
                else if (message.Length > 14 && message[4] == 0x08)
                {
                    //TODO: complete here...
                    processed = true;
                }
            }

            return processed;
        }

    }
}
