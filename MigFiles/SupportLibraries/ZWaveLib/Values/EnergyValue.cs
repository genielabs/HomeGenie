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
    public enum ZWaveEnergyScaleType : int
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
        public EventParameter EventType = EventParameter.Generic;
        public ZWaveEnergyScaleType Parameter = ZWaveEnergyScaleType.Unknown;
        public double Value = 0;

        public static EnergyValue Parse(byte[] message)
        {
            ZWaveValue zvalue = ZWaveValue.ExtractValueFromBytes(message, 4);
            EnergyValue energy = new EnergyValue();
            //energy.Value = ((double)int.Parse(
            //                       message[12].ToString("X2") + message[13].ToString("X2") + message[14].ToString("X2"),
            //                       System.Globalization.NumberStyles.HexNumber
            //                   )) / 1000D;
            energy.Value = zvalue.Value;
            if (Enum.IsDefined(typeof(ZWaveEnergyScaleType), zvalue.Scale))
            {
                energy.Parameter = (ZWaveEnergyScaleType)zvalue.Scale;
            }
            switch (energy.Parameter)
            {
            // Accumulated power consumption kW/h
            case ZWaveEnergyScaleType.kWh:
                energy.EventType = EventParameter.MeterKwHour;
                break;
            // Accumulated power consumption kilo Volt Ampere / hours (kVA/h)
            case ZWaveEnergyScaleType.kVAh:
                energy.EventType = EventParameter.MeterKvaHour;
                break;
            // Instant power consumption Watt
            case ZWaveEnergyScaleType.Watt:
                energy.EventType = EventParameter.MeterWatt;
                break;
            // Pulses count
            case ZWaveEnergyScaleType.Pulses:
                energy.EventType = EventParameter.MeterPulses;
                break;
            // AC load Voltage
            case ZWaveEnergyScaleType.ACVolt:
                energy.EventType = EventParameter.MeterAcVolt;
                break;
            // AC load Current
            case ZWaveEnergyScaleType.ACCurrent:
                energy.EventType = EventParameter.MeterAcCurrent;
                break;
            // Power Factor
            case ZWaveEnergyScaleType.PowerFactor:
                energy.EventType = EventParameter.MeterPower;
                break;
            default:
                energy.EventType = EventParameter.MeterWatt;
                break;
            }
            return energy;
        }
    }
}

