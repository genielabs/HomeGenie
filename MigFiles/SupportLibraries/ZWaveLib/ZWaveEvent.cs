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
    public enum EventParameter
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

    public class ZWaveEvent
    {
        public ZWaveNode Node;
        public EventParameter Parameter;
        public object Value;
        public int Instance;
        public ZWaveEvent NestedEvent;

        public ZWaveEvent(ZWaveNode node, EventParameter eventType, object eventValue, int instance)
        {
            this.Node = node;
            this.Parameter = eventType;
            this.Value = eventValue;
            this.Instance = instance;
        }
    }
}

