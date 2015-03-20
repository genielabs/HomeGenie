using System;

namespace ZWaveLib
{
    
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

}

