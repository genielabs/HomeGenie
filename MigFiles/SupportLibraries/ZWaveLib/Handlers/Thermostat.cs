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
 *     Author: Mike Tanana
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: http://homegenie.it
 */

using System;
using ZWaveLib.Values;
using System.Dynamic;
using System.Collections.Generic;

namespace ZWaveLib.Handlers
{
    public static class Thermostat
    {
       

        
        

        // all Thermostat command classes use
        //    0x01 for the "set" command (same as Command.BasicSet)
        //    0x02 for the "get" command (same as Command.BasicGet)
        //    0x03 value for the "report" command (that is the same as Command.BasicReport)
        /*
        public static ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;
            byte cmdClass = message[7];
            byte cmdType = message[8];
            if (cmdType == (byte)Command.BasicReport)
            {
                switch (cmdClass)
                {
                //case (byte)CommandClass.ThermostatMode:
                //    nodeEvent = new ZWaveEvent(node, EventParameter.ThermostatMode, (Thermostat.Mode)message[9], 0);
                //    break;
                //case (byte)CommandClass.ThermostatOperatingState:   
                //    nodeEvent = new ZWaveEvent(node, EventParameter.ThermostatOperatingState, message[9], 0);
                //    break;
                //case (byte)CommandClass.ThermostatFanMode:  
                //    nodeEvent = new ZWaveEvent(node, EventParameter.ThermostatFanMode, message[9], 0);
                //    break;
                //case (byte)CommandClass.ThermostatFanState:  
                //    nodeEvent = new ZWaveEvent(node, EventParameter.ThermostatFanState, message[9], 0);
                //    break;
                //case (byte)CommandClass.ThermostatHeating:   
                //    nodeEvent = new ZWaveEvent(node, EventParameter.ThermostatHeating, message[9], 0);
                //    break;
                //case (byte)CommandClass.ThermostatSetBack:
                //    nodeEvent = new ZWaveEvent(node, EventParameter.ThermostatSetBack, message[9], 0);
                //    break;
                //case (byte)CommandClass.ThermostatSetPoint:
                //    ZWaveValue zvalue = ZWaveValue.ExtractValueFromBytes(message, 11);
                //    var setPoint = GetSetPointData(node);
                //    setPoint.Precision = zvalue.Precision;
                //    setPoint.Scale = zvalue.Scale;
                //    setPoint.Size = zvalue.Size;
                //    setPoint.Value = zvalue.Value;
                //    dynamic ptype = new ExpandoObject();
                //    ptype.Type = (SetPointType)message[9];
                //    // convert from Fahrenheit to Celsius if needed
                //    ptype.Value = (zvalue.Scale == (int)ZWaveTemperatureScaleType.Fahrenheit ? SensorValue.FahrenheitToCelsius(zvalue.Value) : zvalue.Value);
                //    nodeEvent = new ZWaveEvent(node, EventParameter.ThermostatSetPoint, ptype, 0);
                //    break;
                }
            }
            return nodeEvent;
        }
        */

        public static void GetMode(ZWaveNode node)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatMode, 
                (byte)Command.BasicGet
            });
        }       

        public static void SetMode(ZWaveNode node, Mode mode)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatMode, 
                (byte)Command.BasicSet, 
                (byte)mode
            });
        }

        public static void GetSetPoint(ZWaveNode node, SetPointType ptype)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatSetPoint, 
                (byte)Command.ThermostatSetPointGet,
                (byte)ptype
            });
        }

        public static void SetSetPoint(ZWaveNode node, SetPointType ptype, double temperature)
        {
            List<byte> message = new List<byte>();
            message.AddRange(new byte[] { 
                (byte)CommandClass.ThermostatSetPoint, 
                (byte)Command.ThermostatSetPointSet, 
                (byte)ptype
            });
            var setPoint = ThermostatSetPoint.GetSetPointData(node);
            message.AddRange(ZWaveValue.GetValueBytes(temperature, setPoint.Precision, setPoint.Scale, setPoint.Size));
            node.SendRequest(message.ToArray());
        }

        public static void GetFanState(ZWaveNode node)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatFanState, 
                (byte)Command.BasicGet
            });
        }

        public static void GetFanMode(ZWaveNode node)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatFanMode, 
                (byte)Command.BasicGet
            });
        }

        public static void SetFanMode(ZWaveNode node, FanMode mode)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatFanMode, 
                (byte)Command.BasicSet, 
                (byte)mode
            });
        }

    }
}

