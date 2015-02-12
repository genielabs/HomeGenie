using System;

namespace ZWaveLib.Devices
{
    
    public enum ParameterEvent
    {
        Level,
        ManufacturerSpecific,
        Generic,
        MeterKwHour,
        MeterKvaHour,
        MeterWatt,
        MeterPulses,
        MeterAcVolt,
        MeterAcCurrent,
        MeterPower,
        SensorTemperature,
        SensorHumidity,
        SensorLuminance,
        SensorMotion,
        AlarmDoorWindow,
        AlarmGeneric,
        AlarmSmoke,
        AlarmCarbonMonoxide,
        AlarmCarbonDioxide,
        AlarmHeat,
        AlarmFlood,
        AlarmTampered,
        Configuration,
        WakeUpInterval,
        WakeUpNotify,
        Association,
        Battery,
        NodeInfo,
        MultiinstanceSwitchBinaryCount,
        MultiinstanceSwitchBinary,
        MultiinstanceSwitchMultilevelCount,
        MultiinstanceSwitchMultilevel,
        MultiinstanceSensorBinaryCount,
        MultiinstanceSensorBinary,
        MultiinstanceSensorMultilevelCount,
        MultiinstanceSensorMultilevel,
        ThermostatFanMode,
        ThermostatFanState,
        ThermostatHeating,
        ThermostatMode,
        ThermostatOperatingState,
        ThermostatSetBack,
        ThermostatSetPoint,
        UserCode
    }

}

