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

    public enum ZWaveSensorParameter
    {
        UNKNOWN = -1,
        TEMPERATURE = 1,
        GENERAL_PURPOSE_VALUE = 2,
        LUMINANCE = 3,
        POWER = 4,
        RELATIVE_HUMIDITY = 5,
        VELOCITY = 6,
        DIRECTION = 7,
        ATMOSPHERIC_PRESSURE = 8,
        BAROMETRIC_PRESSURE = 9,
        SOLAR_RADIATION = 10,
        DEW_POINT = 11,
        RAIN_RATE = 12,
        TIDE_LEVEL = 13,
        WEIGHT = 14,
        VOLTAGE = 15,
        CURRENT = 16,
        CO2_LEVEL = 17,
        AIR_FLOW = 18,
        TANK_CAPACITY = 19,
        DISTANCE = 20,
        ANGLE_POSITION = 21
    }

    public class SensorValue
    {
        public ParameterType EventType = ParameterType.GENERIC;
        public ZWaveSensorParameter Parameter = ZWaveSensorParameter.UNKNOWN;
        public double Value = 0d;

        public static SensorValue Parse(byte[] message)
        {
            SensorValue sensor = new SensorValue();
            //
            ZWaveValue zvalue = Utility.ExtractValueFromBytes(message, 11);
            //
            byte key = message[9];
            if (key == (byte)ZWaveSensorParameter.TEMPERATURE)
            {
                sensor.Parameter = ZWaveSensorParameter.TEMPERATURE;
                sensor.Value = Utility.ExtractTemperatureFromBytes(message);
                sensor.EventType = ParameterType.SENSOR_TEMPERATURE;
            }
            else if (key == (byte)ZWaveSensorParameter.GENERAL_PURPOSE_VALUE)
            {
                sensor.Parameter = ZWaveSensorParameter.GENERAL_PURPOSE_VALUE;
                sensor.Value = zvalue.Value;
                sensor.EventType = ParameterType.GENERIC;
            }
            else if (key == (byte)ZWaveSensorParameter.LUMINANCE)
            {
                sensor.Parameter = ZWaveSensorParameter.LUMINANCE;
                sensor.Value = zvalue.Value;
                sensor.EventType = ParameterType.SENSOR_LUMINANCE;
            }
            else if (key == (byte)ZWaveSensorParameter.RELATIVE_HUMIDITY)
            {
                sensor.Parameter = ZWaveSensorParameter.RELATIVE_HUMIDITY;
                sensor.Value = zvalue.Value;
                sensor.EventType = ParameterType.SENSOR_HUMIDITY;
            }
            else if (key == (byte)ZWaveSensorParameter.POWER)
            {
                //sensor.Value = BitConverter.ToUInt16(new byte[2] { message[12], message[11] }, 0) / 10D;
                //sensor.Value = ((double)int.Parse(
                //    message[12].ToString("X2") + message[13].ToString("X2") + message[14].ToString("X2"),
                //    System.Globalization.NumberStyles.HexNumber
                //    )) / 1000D;
                // TODO: this might be very buggy.... to be tested
                EnergyValue energy = EnergyValue.Parse(message);
                sensor.Parameter = ZWaveSensorParameter.POWER;
                sensor.Value = energy.Value;
                sensor.EventType = ParameterType.METER_POWER;
            }
            else
            {
                sensor.Value = zvalue.Value;
            }
            //
            return sensor;
        }
    }

}

