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
        BASIC_SET = 0x01,
        BASIC_GET = 0x02,
        BASIC_REPORT = 0x03,
        //
        BATTERY_GET = 0x02,
        BATTERY_REPORT = 0x03,
        //
        METER_GET = 0x01,
        METER_REPORT = 0x02,
        METER_SUPPORTED_GET = 0x03,
        METER_SUPPORTED_REPORT = 0x04,
        METER_RESET = 0x05,
        //
        SENSOR_BINARY_GET = 0x02,
        SENSOR_BINARY_REPORT = 0x03,
        //
        SENSOR_MULTILEVEL_GET = 0x04,
        SENSOR_MULTILEVEL_REPORT = 0x05,
        //
        SENSOR_ALARM_GET = 0x01,
        SENSOR_ALARM_REPORT = 0x02,
        SENSOR_ALARM_SUPPORTED_GET = 0x03,
        SENSOR_ALARM_SUPPORTED_REPORT = 0x04,
        //
        ALARM_GET = 0x04,
        ALARM_REPORT = 0x05,
        //
        MULTIINSTANCE_SET = 0x01,
        MULTIINSTANCE_GET = 0x02,
        MULTIINSTANCE_COUNT_GET = 0x04,
        MULTIINSTANCE_COUNT_REPORT = 0x05,
        MULTIINSTANCE_REPORT = 0x06,
        //
        MULTIINSTANCEV2_ENCAP = 0x0D,
        //
        ASSOCIATION_SET = 0x01,
        ASSOCIATION_GET = 0x02,
        ASSOCIATION_REPORT = 0x03,
        ASSOCIATION_REMOVE = 0x04,
        //
        CONFIGURATION_SET = 0x04,
        CONFIGURATION_GET = 0x05,
        CONFIGURATION_REPORT = 0x06,
        //
        MANUFACTURERSPECIFIC_GET = 0x04,
        //
        WAKEUP_INTERVAL_SET = 0x04,
        WAKEUP_INTERVAL_GET = 0x05,
        WAKEUP_INTERVAL_REPORT = 0x06,
        WAKEUP_NOTIFICATION = 0x07,
        WAKEUP_INTERVAL_CAPABILITIES_GET = 0x09,
        WAKEUP_INTERVAL_CAPABILITIES_REPORT = 0x0A,
        //
        THERMOSTAT_SETPOINT_SET = 0x01,
        THERMOSTAT_SETPOINT_GET = 0x02,
        THERMOSTAT_SETPOINT_REPORT = 0x03,
        THERMOSTAT_SETPOINT_SUPPORTED_GET = 0x04,
        THERMOSTAT_SETPOINT_SUPPORTED_REPORT = 0x05,
        //
        SCENE_ACTIVATION_SET = 0x01
    }

    public enum CommandClass : byte
    {
        BASIC = 0x20,
        //
        SWITCH_BINARY = 0x25,
        SWITCH_MULTILEVEL = 0x26,
        SWITCH_ALL = 0x27,
        //
        SCENE_ACTIVATION = 0x2B,
        //
        SENSOR_BINARY = 0x30,
        SENSOR_MULTILEVEL = 0x31,
        //
        METER = 0x32,
        //
        MULTIINSTANCE = 0x60,
        CONFIGURATION = 0x70,
        ALARM = 0x71,
        MANUFACTURER_SPECIFIC = 0x72,
        NODE_NAMING = 0x77,
        //
        BATTERY = 0x80,
        HAIL = 0x82,
        WAKE_UP = 0x84,
        ASSOCIATION = 0x85,
        VERSION = 0x86,
        //
        SENSOR_ALARM = 0x9C,
        SILENCE_ALARM = 0x9D,
        //
        THERMOSTAT_FAN_MODE = 0x44,	 
        THERMOSTAT_FAN_STATE = 0x45,	 
        THERMOSTAT_HEATING = 0x38,
        THERMOSTAT_MODE = 0x40,
        THERMOSTAT_OPERATING_STATE =  0x42,	 
        THERMOSTAT_SETBACK = 0x47,	 
        THERMOSTAT_SETPOINT = 0x43	 
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

