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
    public enum ZWaveEnergyParameter : int
    {
        kWh = 0x00,
        kVAh = 0x01,
        Watt = 0x02,
        Pulses = 0x03,
        ACVolt = 0x04,
        ACCurrent = 0x05,
        PowerFactor = 0x06,
        Unknown = 0xFF
    }

    public class EnergyValue
    {
        public ParameterType EventType = ParameterType.GENERIC;
        public ZWaveEnergyParameter Parameter = ZWaveEnergyParameter.Unknown;
        public double Value = 0;

        public static EnergyValue Parse(byte[] message)
        {
            int scale = 0;
            EnergyValue energy = new EnergyValue();
            //energy.Value = ((double)int.Parse(
            //                       message[12].ToString("X2") + message[13].ToString("X2") + message[14].ToString("X2"),
            //                       System.Globalization.NumberStyles.HexNumber
            //                   )) / 1000D;
            energy.Value = Utility.ExtractValueFromBytes(message, 11, out scale);
            if (Enum.IsDefined(typeof(ZWaveEnergyParameter), scale))
            {
                energy.Parameter = (ZWaveEnergyParameter)scale;
            }
            switch (energy.Parameter)
            {
            // Accumulated power consumption kW/h
            case ZWaveEnergyParameter.kWh:
                energy.EventType = ParameterType.METER_KW_HOUR;
                break;
            // Accumulated power consumption kilo Volt Ampere / hours (kVA/h)
            case ZWaveEnergyParameter.kVAh:
                energy.EventType = ParameterType.METER_KVA_HOUR;
                break;
            // Instant power consumption Watt
            case ZWaveEnergyParameter.Watt:
                energy.EventType = ParameterType.METER_WATT;
                break;
            // Pulses count
            case ZWaveEnergyParameter.Pulses:
                energy.EventType = ParameterType.METER_PULSES;
                break;
            // AC load Voltage
            case ZWaveEnergyParameter.ACVolt:
                energy.EventType = ParameterType.METER_AC_VOLT;
                break;
            // AC load Current
            case ZWaveEnergyParameter.ACCurrent:
                energy.EventType = ParameterType.METER_AC_CURRENT;
                break;
            // Power Factor
            case ZWaveEnergyParameter.PowerFactor:
                energy.EventType = ParameterType.METER_POWER;
                break;
            default:
                energy.EventType = ParameterType.METER_WATT;
                break;
            }
            return energy;
        }
    }
}

