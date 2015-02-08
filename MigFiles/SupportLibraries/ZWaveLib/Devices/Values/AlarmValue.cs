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

namespace ZWaveLib.Devices.Values
{

    public enum ZWaveAlarmParameter
    {
        GENERIC = 0,
        SMOKE,
        CARBONMONOXIDE,
        CARBONDIOXIDE,
        HEAT,
        FLOOD
    }

    public class AlarmValue
    {
        public ParameterEvent EventType = ParameterEvent.Generic;
        public ZWaveAlarmParameter Parameter = ZWaveAlarmParameter.GENERIC;
        public byte Value = 0x00;

        public static AlarmValue Parse(byte[] message)
        {
            AlarmValue alarm = new AlarmValue();
            alarm.Value = message[10]; // CommandClass.COMMAND_CLASS_ALARM
            //
            byte cmdClass = message[7];
            if (cmdClass == (byte)CommandClass.SensorAlarm)
            {
                alarm.Parameter = (ZWaveAlarmParameter)Enum.Parse(
                    typeof(ZWaveAlarmParameter),
                    message[10].ToString()
                    );
                alarm.Value = message[11];
            }
            //
            switch (alarm.Parameter)
            {
                case ZWaveAlarmParameter.CARBONDIOXIDE:
                alarm.EventType = ParameterEvent.AlarmCarbonDioxide;
                break;
                case ZWaveAlarmParameter.CARBONMONOXIDE:
                alarm.EventType = ParameterEvent.AlarmCarbonMonoxide;
                break;
                case ZWaveAlarmParameter.SMOKE:
                alarm.EventType = ParameterEvent.AlarmSmoke;
                break;
                case ZWaveAlarmParameter.HEAT:
                alarm.EventType = ParameterEvent.AlarmHeat;
                break;
                case ZWaveAlarmParameter.FLOOD:
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

