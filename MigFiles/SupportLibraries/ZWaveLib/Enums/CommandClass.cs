using System;

namespace ZWaveLib
{
    
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
        Crc16Encapsulated = 0x56,
        //
        MultiInstance = 0x60,
        DoorLock = 0x62,
        UserCode = 0x63,
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
        Security = 0x98,
        //
        SensorAlarm = 0x9C,
        SilenceAlarm = 0x9D
    }

}

