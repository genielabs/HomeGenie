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

    public enum ZWaveSensorParameter
    {
        Unknown = -1,
        Temperature = 1,
        GeneralPurposeValue = 2,
        Luminance = 3,
        Power = 4,
        RelativeHumidity = 5,
        Velocity = 6,
        Direction = 7,
        AtmosphericPressure = 8,
        BarometricPressure = 9,
        SolarRadiation = 10,
        DewPoint = 11,
        RainRate = 12,
        TideLevel = 13,
        Weight = 14,
        Voltage = 15,
        Current = 16,
        Co2Level = 17,
        AirFlow = 18,
        TankCapacity = 19,
        Distance = 20,
        AnglePosition = 21
    }

    public enum ZWaveTemperatureScaleType : int
    {
        Celsius,
        Fahrenheit
    }

    public class SensorValue
    {
        public ParameterEvent EventType = ParameterEvent.Generic;
        public ZWaveSensorParameter Parameter = ZWaveSensorParameter.Unknown;
        public double Value = 0d;

        public static SensorValue Parse(byte[] message)
        {
            SensorValue sensor = new SensorValue();
            //
            ZWaveValue zvalue = Utility.ExtractValueFromBytes(message, 11);
            //
            byte key = message[9];
            if (key == (byte)ZWaveSensorParameter.Temperature)
            {
                zvalue = ExtractTemperatureFromBytes(message);
                sensor.Parameter = ZWaveSensorParameter.Temperature;
                // convert from Fahrenheit to Celsius if needed
                sensor.Value = (zvalue.Scale == (int)ZWaveTemperatureScaleType.Fahrenheit ? SensorValue.FahrenheitToCelsius(zvalue.Value) : zvalue.Value);
                sensor.EventType = ParameterEvent.SensorTemperature;
            }
            else if (key == (byte)ZWaveSensorParameter.GeneralPurposeValue)
            {
                sensor.Parameter = ZWaveSensorParameter.GeneralPurposeValue;
                sensor.Value = zvalue.Value;
                sensor.EventType = ParameterEvent.Generic;
            }
            else if (key == (byte)ZWaveSensorParameter.Luminance)
            {
                sensor.Parameter = ZWaveSensorParameter.Luminance;
                sensor.Value = zvalue.Value;
                sensor.EventType = ParameterEvent.SensorLuminance;
            }
            else if (key == (byte)ZWaveSensorParameter.RelativeHumidity)
            {
                sensor.Parameter = ZWaveSensorParameter.RelativeHumidity;
                sensor.Value = zvalue.Value;
                sensor.EventType = ParameterEvent.SensorHumidity;
            }
            else if (key == (byte)ZWaveSensorParameter.Power)
            {
                //sensor.Value = BitConverter.ToUInt16(new byte[2] { message[12], message[11] }, 0) / 10D;
                //sensor.Value = ((double)int.Parse(
                //    message[12].ToString("X2") + message[13].ToString("X2") + message[14].ToString("X2"),
                //    System.Globalization.NumberStyles.HexNumber
                //    )) / 1000D;
                // TODO: this might be very buggy.... to be tested
                EnergyValue energy = EnergyValue.Parse(message);
                sensor.Parameter = ZWaveSensorParameter.Power;
                sensor.Value = energy.Value;
                sensor.EventType = ParameterEvent.MeterPower;
            }
            else
            {
                sensor.Value = zvalue.Value;
            }
            //
            return sensor;
        }
                
        public static ZWaveValue ExtractTemperatureFromBytes(byte[] message)
        {
            byte[] tmp = new byte[4];
            System.Array.Copy(message, message.Length - 4, tmp, 0, 4);
            message = tmp;

            ZWaveValue zvalue = Utility.ExtractValueFromBytes(message, 1);
            // zvalue.Scale == 1 -> Fahrenheit
            // zvalue.Scale == 0 -> Celsius 

            return zvalue;
        }

        public static double FahrenheitToCelsius(double temperature)
        {
            return ((5.0 / 9.0) * (temperature - 32.0));
        }
    }

}

