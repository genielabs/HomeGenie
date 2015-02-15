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
using ZWaveLib.Values;
using System.Dynamic;
using System.Collections.Generic;

namespace ZWaveLib.Handlers
{
    public static class Thermostat
    {
        public enum Mode
        {
            Off = 0x00,
            Heat = 0x01,
            Cool = 0x02,
            Auto = 0x03,
            AuxHeat = 0x04,
            Resume = 0x05,
            FanOnly = 0x06,
            Furnace = 0x07,
            DryAir = 0x08,
            MoistAir = 0x09,
            AutoChangeover = 0x0A,
            HeatEconomy = 0x0B,
            CoolEconomy = 0x0C,
            Away = 0x0D
        }

        public enum OperatingState
        {
            Idle = 0x00,
            Heating = 0x01,
            Cooling = 0x02,
            FanOnly = 0x03,
            PendingHeat = 0x04,
            PendingCool = 0x05,
            VentEconomizer = 0x06,
            State07 = 0x07,
            State08 = 0x08,
            State09 = 0x09,
            State10 = 0x0A,
            State11 = 0x0B,
            State12 = 0x0C,
            State13 = 0x0D,
            State14 = 0x0E,
            State15 = 0x0F
        }

        public enum FanMode
        {
            AutoLow = 0x00,
            OnLow = 0x01,
            AutoHigh = 0x02,
            OnHigh = 0x03,
            Unknown4 = 0x04,
            Unknown5 = 0x05,
            Circulate = 0x06
        }

        public enum FanState
        {
            Idle = 0x00,
            Running = 0x01,
            RunningHigh = 0x02,
            State03 = 0x03,
            State04 = 0x04,
            State05 = 0x05,
            State06 = 0x06,
            State07 = 0x07,
            State08 = 0x08,
            State09 = 0x09,
            State10 = 0x0A,
            State11 = 0x0B,
            State12 = 0x0C,
            State13 = 0x0D,
            State14 = 0x0E,
            State15 = 0x0F
        }

        public enum SetPointType
        {
            Unused = 0x00,
            Heating = 0x01,
            Cooling = 0x02,
            Unused03 = 0x03,
            Unused04 = 0x04,
            Unused05 = 0x05,
            Unused06 = 0x06,
            Furnace = 0x07,
            DryAir = 0x08,
            MoistAir = 0x09,
            AutoChangeover = 0x0A,
            HeatingEconomy = 0x0B,
            CoolingEconomy = 0x0C,
            HeatingAway = 0x0D
        }

        public static ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;
            byte cmdClass = message[7];
            byte cmdType = message[8];
            switch (cmdClass)
            {
            case (byte)CommandClass.ThermostatMode:
                nodeEvent = new ZWaveEvent(node, ParameterEvent.ThermostatMode, (Thermostat.Mode)message[9], 0);
                break;
            case (byte)CommandClass.ThermostatOperatingState:   
                nodeEvent = new ZWaveEvent(node, ParameterEvent.ThermostatOperatingState, message[9], 0);
                break;
            case (byte)CommandClass.ThermostatFanMode:  
                nodeEvent = new ZWaveEvent(node, ParameterEvent.ThermostatFanMode, message[9], 0);
                break;
            case (byte)CommandClass.ThermostatFanState:  
                nodeEvent = new ZWaveEvent(node, ParameterEvent.ThermostatFanState, message[9], 0);
                break;
            case (byte)CommandClass.ThermostatHeating:   
                nodeEvent = new ZWaveEvent(node, ParameterEvent.ThermostatHeating, message[9], 0);
                break;
            case (byte)CommandClass.ThermostatSetBack:
                nodeEvent = new ZWaveEvent(node, ParameterEvent.ThermostatSetBack, message[9], 0);
                break;
            case (byte)CommandClass.ThermostatSetPoint:
                ZWaveValue zvalue = SensorValue.ExtractTemperatureFromBytes(message);
                var setPoint = GetSetPointData(node);
                setPoint.Precision = zvalue.Precision;
                setPoint.Scale = zvalue.Scale;
                setPoint.Size = zvalue.Size;
                setPoint.Value = zvalue.Value;
                dynamic ptype = new ExpandoObject();
                ptype.Type = (SetPointType)message[9];
                // convert from Fahrenheit to Celsius if needed
                ptype.Value = (zvalue.Scale == (int)ZWaveTemperatureScaleType.Fahrenheit ? SensorValue.FahrenheitToCelsius(zvalue.Value) : zvalue.Value);
                nodeEvent = new ZWaveEvent(node, ParameterEvent.ThermostatSetPoint, ptype, 0);
                break;
            }
            return nodeEvent;
        }

        
        //initial request for thermostat mode, since this doesn't change very often
        public static void GetMode(ZWaveNode node)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatMode, 
                (byte)Command.BasicGet
            });
        }       

        /*
         * This Sets the mode for the thermostat (cool, heat, off, auto)  Based on int commands
         */ 
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
            var setPoint = GetSetPointData(node);
            message.AddRange(Utility.GetValueBytes(temperature, setPoint.Precision, setPoint.Scale, setPoint.Size));
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

        public static void GetOperatingState(ZWaveNode node)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatOperatingState, 
                (byte)Command.BasicGet
            });
        }
        
        private static ZWaveValue GetSetPointData(ZWaveNode node)
        {
            if (!node.Data.ContainsKey("SetPoint"))
            {
                node.Data.Add("SetPoint", new ZWaveValue());
            }
            return (ZWaveValue)node.Data["SetPoint"];
        }
    }
}

