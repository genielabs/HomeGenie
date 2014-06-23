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
namespace ZWaveLib
{
    public enum Command : byte
    {
        COMMAND_BASIC_SET = 0x01,
        COMMAND_BASIC_GET = 0x02,
        COMMAND_BASIC_REPORT = 0x03,
        //
        COMMAND_MULTIINSTANCE_SET = 0x01,
        COMMAND_MULTIINSTANCE_GET = 0x02,
        COMMAND_MULTIINSTANCE_COUNT_GET = 0x04,
        COMMAND_MULTIINSTANCE_COUNT_REPORT = 0x05,
        COMMAND_MULTIINSTANCE_REPORT = 0x06,
        //
        COMMAND_MULTIINSTANCEV2_ENCAP = 0x0D,
        //
        COMMAND_ASSOCIATION_SET = 0x01,
        COMMAND_ASSOCIATION_GET = 0x02,
        COMMAND_ASSOCIATION_REPORT = 0x03,
        COMMAND_ASSOCIATION_REMOVE = 0x04,
        //
        COMMAND_CONFIG_SET = 0x04,
        COMMAND_CONFIG_GET = 0x05,
        COMMAND_CONFIG_REPORT = 0x06,
        //
        COMMAND_WAKEUP_REPORT = 0x06,
        COMMAND_WAKEUP_NOTIFICATION = 0x07
    }

    public enum CommandClass : byte
    {
        COMMAND_CLASS_BASIC = 0x20,
        //
        COMMAND_CLASS_SWITCH_BINARY = 0x25,
        COMMAND_CLASS_SWITCH_MULTILEVEL = 0x26,
        COMMAND_CLASS_SWITCH_ALL = 0x27,
        //
        COMMAND_CLASS_SCENE_ACTIVATION = 0x2B,
        //
        COMMAND_CLASS_SENSOR_BINARY = 0x30,
        COMMAND_CLASS_SENSOR_MULTILEVEL = 0x31,
        //
        COMMAND_CLASS_METER = 0x32,
        //
        COMMAND_CLASS_MULTIINSTANCE = 0x60,
        COMMAND_CLASS_COONFIGURATION = 0x70,
        COMMAND_CLASS_ALARM = 0x71,
        COMMAND_CLASS_MANUFACTURER_SPECIFIC = 0x72,
        COMMAND_CLASS_NODE_NAMING = 0x77,
        //
        COMMAND_CLASS_BATTERY = 0x80,
        COMMAND_CLASS_HAIL = 0x82,
        COMMAND_CLASS_WAKE_UP = 0x84,
        COMMAND_CLASS_ASSOCIATION = 0x85,
        COMMAND_CLASS_VERSION = 0x86,
        //
        COMMAND_CLASS_SENSOR_ALARM = 0x9C,
        COMMAND_CLASS_SILENCE_ALARM = 0x9D,

	COMMAND_CLASS_THERMOSTAT_FAN_MODE = 0x44,	 
	COMMAND_CLASS_THERMOSTAT_FAN_STATE = 0x45,	 
	COMMAND_CLASS_THERMOSTAT_HEATING = 0x38,
	COMMAND_CLASS_THERMOSTAT_MODE = 0x40,
	COMMAND_CLASS_THERMOSTAT_OPERATING_STATE =  0x42,	 
	COMMAND_CLASS_THERMOSTAT_SETBACK = 0x47,	 
	COMMAND_CLASS_THERMOSTAT_SETPOINT = 0x43	 
		
    }

    public enum GenericType : byte
    {
        GENERIC_CONTROLLER = 0x01,
        STATIC_CONTROLLER = 0x02,
        AV_CONTROL_POINT = 0x03,
        DISPLAY = 0x06,
        GARAGE_DOOR = 0x07,
        THERMOSTAT = 0x08,
        WINDOW_COVERING = 0x09,
        REPEATER_SLAVE = 0x0F,
        SWITCH_BINARY = 0x10,
        SWITCH_MULTILEVEL = 0x11,
        SWITCH_REMOTE = 0x12,
        SWITCH_TOGGLE = 0x13,
        SENSOR_BINARY = 0x20,
        SENSOR_MULTILEVEL = 0x21,
        WATER_CONTROL = 0x22,
        METER_PULSE = 0x30,
        METER = 0x31,
        ENTRY_CONTROL = 0x40,
        SEMI_INTEROPERABLE = 0x50,
        SENSOR_ALARM = 0xA1,
        NON_INTEROPERABLE = 0xFF
    }
}

