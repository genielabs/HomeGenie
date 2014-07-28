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
    public class Thermostat : Sensor
    {

            
        /*
         * 
         * This Sets the mode for the thermostat (cool, heat, off, auto)  Based on int commands
         * 
         **/ 

        public virtual void thermModeSet(int mode)
        {
            this.nodeHost.ZWaveMessage(new byte[] { 
                (byte)CommandClass.COMMAND_CLASS_THERMOSTAT_MODE, 
                (byte)Command.COMMAND_BASIC_SET, 
                (byte)mode
            });
        }
        public virtual void thermFanModeSet(int mode)
        {
            this.nodeHost.ZWaveMessage(new byte[] { 
                (byte)CommandClass.COMMAND_CLASS_THERMOSTAT_FAN_MODE, 
                (byte)Command.COMMAND_BASIC_SET, 
                (byte)mode
            });
        }

        //initial request for thermostat mode, since this doesn't change very often
        public virtual void thermModeGet()
        {
            this.nodeHost.ZWaveMessage(new byte[] { 
                (byte)CommandClass.COMMAND_CLASS_THERMOSTAT_MODE, 
                (byte)Command.COMMAND_BASIC_GET
            });
        }
            
        public virtual void thermSetpointGet()
        {
            this.nodeHost.ZWaveMessage(new byte[] { 
                (byte)CommandClass.COMMAND_CLASS_THERMOSTAT_SETPOINT, 
                (byte)Command.COMMAND_BASIC_GET
            });
        }

        public virtual void thermFanModeGet()
        {
            this.nodeHost.ZWaveMessage(new byte[] { 
                (byte)CommandClass.COMMAND_CLASS_THERMOSTAT_FAN_MODE, 
                (byte)Command.COMMAND_BASIC_GET
            });
        }
        public virtual void thermFanStateGet()
        {
            this.nodeHost.ZWaveMessage(new byte[] { 
                (byte)CommandClass.COMMAND_CLASS_THERMOSTAT_FAN_STATE, 
                (byte)Command.COMMAND_BASIC_GET
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
            0x02 - type
            0x09 - 3 bit precision, 2 bit scale (0 = C,1=F), 3 bit size -> Fahrenheit, size == 1
            0x10 - value to set == 16 degF
         * 
         * */

        public virtual void thermTempSet(double temp)
        {
            int t=(int)temp;
            this.nodeHost.ZWaveMessage(new byte[] { 
                (byte)CommandClass.COMMAND_CLASS_THERMOSTAT_SETPOINT, 
                (byte)Command.COMMAND_BASIC_SET, 
                0x02,
                0x09,
                (byte)t
            });
        }

    }
}
