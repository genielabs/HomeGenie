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

namespace ZWaveLib.Values
{
    public enum ZWaveAlarmType
    {
        Generic = 0,
        Smoke,
        CarbonMonoxide,
        CarbonDioxide,
        Heat,
        Flood
    }

    public class AlarmValue
    {
        public ParameterEvent EventType = ParameterEvent.Generic;
        public ZWaveAlarmType Parameter = ZWaveAlarmType.Generic;
        public byte Value = 0x00;

        public static AlarmValue Parse(byte[] message)
        {
            AlarmValue alarm = new AlarmValue();
            alarm.Value = message[10];
            //
            byte cmdClass = message[7];
            if (cmdClass == (byte)CommandClass.SensorAlarm)
            {
                alarm.Parameter = (ZWaveAlarmType)Enum.Parse(typeof(ZWaveAlarmType), message[10].ToString());
                alarm.Value = message[11];
            }
            //
            switch (alarm.Parameter)
            {
            case ZWaveAlarmType.CarbonDioxide:
                alarm.EventType = ParameterEvent.AlarmCarbonDioxide;
                break;
            case ZWaveAlarmType.CarbonMonoxide:
                alarm.EventType = ParameterEvent.AlarmCarbonMonoxide;
                break;
            case ZWaveAlarmType.Smoke:
                alarm.EventType = ParameterEvent.AlarmSmoke;
                break;
            case ZWaveAlarmType.Heat:
                alarm.EventType = ParameterEvent.AlarmHeat;
                break;
            case ZWaveAlarmType.Flood:
                alarm.EventType = ParameterEvent.AlarmFlood;
                break;
            //case ZWaveSensorAlarmParameter.GENERIC:
            default:
                alarm.EventType = ParameterEvent.AlarmGeneric;
                break;
            }
            //
            return alarm;
        }
    }
}
