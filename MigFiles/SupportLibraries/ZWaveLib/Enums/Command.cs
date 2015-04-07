using System;

namespace ZWaveLib
{
    
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
        /// <summary>
        /// 0x05
        /// </summary>
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
        UserCodeSet = 0x01,
        //
        DoorLock_Set = 0x01,
        DoorLock_Get = 0x02,
        DoorLock_Report = 0x03,
        DoorLock_Configuration_Set = 0x04,
        DoorLock_Configuration_Get = 0x05,
        DoorLock_Configuration_Report = 0x06
    }

}

