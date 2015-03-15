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
    public enum Function: byte
    {
        None = 0x00,
        DiscoveryNodes = 0x02,
        ApplicationCommand = 0x04,
        ControllerSoftReset = 0x08,
        SendData = 0x13,
        GetNodeProtocolInfo = 0x41,
        ControllerSetDefault = 0x42,
        // hard reset
        NodeUpdateInfo = 0x49,
        NodeAdd = 0x4A,
        NodeRemove = 0x4B,
        RequestNodeInfo = 0x60
    }

    public enum NodeFunctionOption : byte
    {
        AddNodeAny = 0x01,
        AddNodeController = 0x02,
        AddNodeSlave = 0x03,
        AddNodeExisting = 0x04,
        AddNodeStop = 0x05,
        //
        RemoveNodeAny = 0x01,
        RemoveNodeController = 0x02,
        RemoveNodeSlave = 0x03,
        RemoveNodeStop = 0x05
    }

    public enum NodeFunctionStatus : byte
    {
        AddNodeLearnReady = 0x01,
        AddNodeNodeFound = 0x02,
        AddNodeAddingSlave = 0x03,
        AddNodeAddingController = 0x04,
        AddNodeProtocolDone = 0x05,
        AddNodeDone = 0x06,
        AddNodeFailed = 0x07,
        //
        RemoveNodeLearnReady = 0x01,
        RemoveNodeNodeFound = 0x02,
        RemoveNodeRemovingSlave = 0x03,
        RemoveNodeRemovingController = 0x04,
        RemoveNodeDone = 0x06,
        RemoveNodeFailed = 0x07
    }

    public enum GenericType : byte
    {
        None = 0x00,
        GenericController = 0x01,
        StaticController = 0x02,
        AvControlPoint = 0x03,
        Display = 0x06,
        GarageDoor = 0x07,
        Thermostat = 0x08,
        WindowCovering = 0x09,
        RepeaterSlave = 0x0F,
        SwitchBinary = 0x10,
        SwitchMultilevel = 0x11,
        SwitchRemote = 0x12,
        SwitchToggle = 0x13,
        SensorBinary = 0x20,
        SensorMultilevel = 0x21,
        WaterControl = 0x22,
        MeterPulse = 0x30,
        Meter = 0x31,
        EntryControl = 0x40,
        SemiInteroperable = 0x50,
        SensorAlarm = 0xA1,
        NonInteroperable = 0xFF
    }

    public enum CommandClass : byte
    {
        Basic = 0x20,
        //
        SwitchBinary = 0x25,
        SwitchMultilevel = 0x26,
        SwitchAll = 0x27,
        //
        SceneActivation = 0x2B,
        //
        SensorBinary = 0x30,
        SensorMultilevel = 0x31,
        //
        Meter = 0x32,
        //
        ThermostatHeating = 0x38,
        ThermostatMode = 0x40,
        ThermostatOperatingState = 0x42,
        ThermostatSetPoint = 0x43,
        ThermostatFanMode = 0x44,
        ThermostatFanState = 0x45, 
        ThermostatSetBack = 0x47,
        //
        MultiInstance = 0x60,
        Configuration = 0x70,
        Alarm = 0x71,
        ManufacturerSpecific = 0x72,
        NodeNaming = 0x77,
        //
        Battery = 0x80,
        Hail = 0x82,
        WakeUp = 0x84,
        Association = 0x85,
        Version = 0x86,
        //
        SensorAlarm = 0x9C,
        SilenceAlarm = 0x9D,
        //
        Crc16Encap = 0x56,
        //
        UserCode = 0x63
    }

    public enum Command : byte
    {
        BasicSet = 0x01,
        BasicGet = 0x02,
        BasicReport = 0x03,
        //
        SwitchBinarySet = 0x01,
        SwitchBinaryGet = 0x02,
        SwitchBinaryReport = 0x03,
        //
        SwitchMultilevelSet = 0x01,
        SwitchMultilevelGet = 0x02,
        SwitchMultilevelReport = 0x03,
        SwitchMultilevelStartLevelChange = 0x04,
        SwitchMultilevelStopLevelChange = 0x05,
        SwitchMultilevelSupportedGet = 0x06,
        SwitchMultilevelSupportedReport = 0x07,
        //
        BatteryGet = 0x02,
        BatteryReport = 0x03,
        //
        MeterGet = 0x01,
        MeterReport = 0x02,
        MeterSupportedGet = 0x03,
        MeterSupportedReport = 0x04,
        MeterReset = 0x05,
        //
        SensorBinaryGet = 0x02,
        SensorBinaryReport = 0x03,
        //
        SensorMultilevelGet = 0x04,
        SensorMultilevelReport = 0x05,
        //
        SensorAlarmGet = 0x01,
        SensorAlarmReport = 0x02,
        SensorAlarmSupportedGet = 0x03,
        SensorAlarmSupportedReport = 0x04,
        //
        AlarmGet = 0x04,
        AlarmReport = 0x05,

        // MultiInstance commands
        /// <summary>
        /// 0x01
        /// </summary>
        MultiInstanceSet = 0x01,
        /// <summary>
        /// 0x02
        /// </summary>
        MultiInstanceGet = 0x02,
        /// <summary>
        /// 0x04
        /// </summary>
        MultiInstanceCountGet = 0x04,
        /// <summary>
        /// 0x05
        /// </summary>
        MultiInstanceCountReport = 0x05,
        /// <summary>
        /// 0x06
        /// </summary>
        MultiInstanceEncapsulated = 0x06,
        //
        /// <summary>
        /// 0x0D
        /// </summary>
        MultiChannelEncapsulated = 0x0D,
        //
        AssociationSet = 0x01,
        AssociationGet = 0x02,
        AssociationReport = 0x03,
        AssociationRemove = 0x04,
        //
        ConfigurationSet = 0x04,
        ConfigurationGet = 0x05,
        ConfigurationReport = 0x06,
        //
        ManufacturerSpecificGet = 0x04,
        //
        WakeUpIntervalSet = 0x04,
        WakeUpIntervalGet = 0x05,
        WakeUpIntervalReport = 0x06,
        WakeUpNotification = 0x07,
        WakeUpIntervalCapabilitiesGet = 0x09,
        WakeUpIntervalCapabilitiesReport = 0x0A,
        //
        ThermostatSetPointSet = 0x01,
        ThermostatSetPointGet = 0x02,
        ThermostatSetPointReport = 0x03,
        ThermostatSetPointSupportedGet = 0x04,
        ThermostatSetPointSupportedReport = 0x05,
        //
        SceneActivationSet = 0x01,
        //
        UserCodeReport = 0x03,
        UserCodeSet = 0x01
    }
}

