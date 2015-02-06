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
 *     Author: Generoso Martello <gene@homegenie.it> 12-11-2014
 *     Project Homepage: http://homegenie.it
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Dynamic;
using ZWaveLib.Devices.Values;

namespace ZWaveLib.Devices.ProductHandlers.Generic
{
    public class Thermostat : Sensor
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

        public override bool HandleBasicReport(byte[] message)
        {
            bool handled = false;
            byte cmdClass = message[7];
            byte cmdType = message[8];
            switch (cmdClass)
            {
            case (byte)CommandClass.ThermostatMode:  
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.THERMOSTAT_MODE, (Thermostat.Mode)message[9]);
                handled = true;
                break;
            case (byte)CommandClass.ThermostatOperatingState:   
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.THERMOSTAT_OPERATING_STATE, message[9]);
                handled = true;
                break;
            case (byte)CommandClass.ThermostatFanMode:  
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.THERMOSTAT_FAN_MODE, message[9]);
                handled = true;
                break;
            case (byte)CommandClass.ThermostatFanState:  
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.THERMOSTAT_FAN_STATE, message[9]);
                handled = true;
                break;
            case (byte)CommandClass.ThermostatHeating:   
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.THERMOSTAT_HEATING, message[9]);
                handled = true;
                break;
            case (byte)CommandClass.ThermostatSetBack:   
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.THERMOSTAT_SETBACK, message[9]);
                handled = true;
                break;
            /*
             * SPI > 01 0C 00 04 00 11 06 43 03 01 2A 02 6C E5
                2014-06-24T22:01:19.8016380-06:00   HomeAutomation.ZWave    17  ZWave Node  Thermostat.SetPoint 1
             */
            case (byte)CommandClass.ThermostatSetPoint:  
                double temp = Utility.ExtractTemperatureFromBytes(message);
                dynamic ptype = new ExpandoObject();
                ptype.Type = (SetPointType)message[9];
                ptype.Value = temp;
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.THERMOSTAT_SETPOINT, ptype);
                handled = true;
                break;
            }

            if (!handled)
                handled = base.HandleBasicReport(message);

            return handled;
        }

        //initial request for thermostat mode, since this doesn't change very often
        public virtual void Thermostat_ModeGet()
        {
            this.nodeHost.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatMode, 
                (byte)Command.BasicGet
            });
        }       

        /*
         * This Sets the mode for the thermostat (cool, heat, off, auto)  Based on int commands
         */ 
        public virtual void Thermostat_ModeSet(Mode mode)
        {
            this.nodeHost.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatMode, 
                (byte)Command.BasicSet, 
                (byte)mode
            });
        }
            
        public virtual void Thermostat_SetPointGet(SetPointType ptype)
        {
            this.nodeHost.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatSetPoint, 
                (byte)Command.ThermostatSetPointGet,
                (byte)ptype
            });
        }
        
        /*
         * I had no idea how this was going to work
         * found this:  http://www.agocontrol.com/forum/index.php?topic=175.10;wap2
         * not sure what it will do with Celsius
         * 
         * Set Thermostat Setpoint (Node=6): 0x01, 0x0c, 0x00, 0x13, 0x06, 0x05, 0x43, 0x01, 0x02, 0x09, 0x10, 0x25, 0xc5, 0x5a
         * 
         *  0x43 - COMMAND_CLASS_THERMOSTAT_SETPOINT
            0x01 - THERMOSTAT_SETPOINT_SET
            0x02 - type (see SetPointType enum)
            0x09 - 3 bit precision, 2 bit scale (0 = C,1=F), 3 bit size -> Fahrenheit, size == 1
            0x10 - value to set == 16 degF
         */
        public virtual void Thermostat_SetPointSet(SetPointType ptype, int temperature)
        {
            this.nodeHost.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatSetPoint, 
                (byte)Command.ThermostatSetPointSet, 
                (byte)ptype,
                0x09,
                (byte)temperature
            });
        }
        
        public virtual void Thermostat_FanStateGet()
        {
            this.nodeHost.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatFanState, 
                (byte)Command.BasicGet
            });
        }

        public virtual void Thermostat_FanModeGet()
        {
            this.nodeHost.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatFanMode, 
                (byte)Command.BasicGet
            });
        }

        public virtual void Thermostat_FanModeSet(FanMode mode)
        {
            this.nodeHost.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatFanMode, 
                (byte)Command.BasicSet, 
                (byte)mode
            });
        }

        public virtual void Thermostat_OperatingStateGet()
        {
            this.nodeHost.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatOperatingState, 
                (byte)Command.BasicGet
            });
        }

        public virtual void Thermostat_OperatingStateReport()
        {
            this.nodeHost.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatOperatingState, 
                (byte)Command.BasicReport
            });
        }
    }
}
