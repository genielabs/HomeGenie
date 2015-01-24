﻿/*
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
using ZWaveLib.Devices.Values;

namespace ZWaveLib.Devices.ProductHandlers.Generic
{
    public class Meter : IZWaveDeviceHandler
    {
        protected ZWaveNode nodeHost;

        public void SetNodeHost(ZWaveNode node)
        {
            nodeHost = node;
            nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.METER_WATT, 0);
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
            //                       ^^        |  |
            //                                 +--|------> 0x31 Command Class Meter
            //                                    +------> 0x02 Meter Report
            bool processed = false;
            //
            byte commandClass = message[7];
            byte commandType = message[8];
            //
            if (commandClass == (byte)CommandClass.Meter && commandType == (byte)Command.MeterReport)
            {
                // TODO: should check meter report type (Electric, Gas, Water) and value precision scale
                // TODO: the code below parse always as Electric type
                EnergyValue energy = EnergyValue.Parse(message);
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, energy.EventType, energy.Value);
                processed = true;
            }
            return processed;
        }

    }
}
