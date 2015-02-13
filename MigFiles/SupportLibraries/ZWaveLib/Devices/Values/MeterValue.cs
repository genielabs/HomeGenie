using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZWaveLib.Devices.Values
{

    /// <summary>
    /// Meter unit byte values per ZWAVE spec doc.
    /// </summary>
    public enum ZWaveMeterUnit : int
    {
        ElectricMeter_kWh = 0x00,
        ElectricMeter_kVAh = 0x01,
        ElectricMeter_Watt = 0x02,
        CubicMeters = 0x00, // Same for GAS and WATER.
        CubicFeet = 0x01, // Same for GAS and WATER.
        USGallons = 0x02, // Only for WATER.
        Pulses = 0x03, // Same for GAS, WATER, ELECTRIC.
        Unknown = 0xff
    }


    public enum ZWaveRateType : int
    {
        Reserved = 0x00,
        Import = 0x01,
        Export = 0x02,
        Reserved2 = 0x03, // Ends here.
        Unknown = 0xff
    }

    public enum ZWaveMeterType : int
    {
        Reserved = 0x00,
        ElectricMeter = 0x01,
        GasMeter = 0x02,
        WaterMeter = 0x03,
        Reserved2 = 0x04, // through 0x1F are all reserved
        Unknown = 0xff
    }


    /// <summary>
    /// Meter value message parsing based on ZWAVE specs document.
    /// 
    /// TODO:
    /// + Add Support for other meter types in ParameterEvent and enable here (only supports energy at the moment).
    /// + When HG supports way to specify rate type, we can use our ZWaveRateType that we have parsed here.
    /// 
    /// </summary>
    public class MeterValue
    {
        public ParameterEvent EventType = ParameterEvent.MeterWatt;
        public ZWaveMeterUnit MeterUnit = ZWaveMeterUnit.Unknown;

        public ZWaveRateType RateType = ZWaveRateType.Unknown;
        public ZWaveMeterType MeterType = ZWaveMeterType.Unknown;

        public double Value = 0; // Signed
        public double PreviousValue = 0;
        public double DeltaT = 0;

        // From ZWAVE specification.
        public static int DeltaTimeValue_NoPreviousValue = 0x0000;
        public static int DeltaTimeValue_UnknownDeltaTime = 0xffff;

        public static MeterValue Parse(byte[] message)
        {
            // TODO: If caller is trying to parse message that is NOT a meter value, throw exception?

            int dataStart = 11; // Basic messages have data at index 11. We change later if message is not basic.

            byte cmdClass = message[7];
            byte cmdType = message[8];
            if (cmdClass == (byte)CommandClass.MultiInstance)
            {
                if (cmdType == (byte)Command.MultiInstaceV2Encapsulated)
                {
                    dataStart = 15;
                    //byte encappedCmdClass = message[11];
                    //byte encappedCmd = message[12];
                }
                else 
                {
                    // Must be multiinstance_encap v1.
                    dataStart = 14; // TODO: Confirm.
                    //byte encappedCmdClass = message[10];
                    //byte encappedCmd = message[11];
                }
            }

            MeterValue meter = new MeterValue();
            int rateType, meterType;
            ZWaveValue currentValue = ExtractMeterValueFromBytes(message, dataStart, out rateType, out meterType);

            meter.Value = currentValue.Value; //ExtractMeterValueFromBytes(message, dataStart, out size, out precision, out scale, out rateType, out meterType);
            if (Enum.IsDefined(typeof(ZWaveRateType), rateType))
            {
                meter.RateType = (ZWaveRateType)rateType;
            }
            if (Enum.IsDefined(typeof(ZWaveMeterType), meterType))
            {
                meter.MeterType = (ZWaveMeterType)meterType;
            }

            double deltaTime;
            ZWaveValue previousValue = ExtractPreviousValueFromBytes(message, dataStart + currentValue.Size, currentValue.Size, currentValue.Precision, currentValue.Scale, out deltaTime); // Func does not look for prec/size byte.

            if (previousValue.Value == DeltaTimeValue_NoPreviousValue)
            {
                // TODO: Indicate somehow that the specified value we are sending is NOT really a previous value.
            }
            else if (previousValue.Value == DeltaTimeValue_UnknownDeltaTime)
            {
                // TODO: Indicate somehow that the deltaT is not available (instead of showing 65536 seconds).
            }

            // NOTE: I suppose we don't need the if / elseif here, but common byte values for meter types get messy.
            //       Also, we may want to perform validation later and/or other logic. Without this, we don't know what meter type 
            //       a shared unit (pulses, CFM, etc) is for.
            // MORE: Might want to make sub object types later for different meter types to clean this up.
            if (meter.MeterType == ZWaveMeterType.ElectricMeter)
            {
                switch (meter.MeterUnit)
                {
                    // Accumulated power consumption kW/h
                    case ZWaveMeterUnit.ElectricMeter_kWh:
                        meter.EventType = ParameterEvent.MeterKwHour;
                        break;
                    // Instant power consumption Watt
                    case ZWaveMeterUnit.ElectricMeter_Watt:
                        meter.EventType = ParameterEvent.MeterWatt;
                        break;
                    // Accumulated power consumption kilo Volt Ampere / hours (kVA/h)
                    case ZWaveMeterUnit.ElectricMeter_kVAh:
                        meter.EventType = ParameterEvent.MeterKvaHour;
                        break;
                    case ZWaveMeterUnit.Pulses:
                        meter.EventType = ParameterEvent.MeterPulses;
                        break;
                    default:
                        meter.EventType = ParameterEvent.MeterWatt;
                        break;
                }
            }
            else if (meter.MeterType == ZWaveMeterType.WaterMeter || meter.MeterType == ZWaveMeterType.GasMeter)
            {
                
                if (meter.MeterType == ZWaveMeterType.WaterMeter && meter.MeterUnit == ZWaveMeterUnit.USGallons)
                {
                    // TODO: Below not yet added to ParameterEvent enum. Add these and then enable.
                    //meter.EventType = ParameterEvent.MeterUSGallons;
                }
                else
                {
                    // These units are shared between WaterMeter and ElectricMeter.
                    switch (meter.MeterUnit)
                    {
                        /*
                       // TODO: Below not yet added to ParameterEvent enum. Add these and then enable.
                       // 
                       case ZWaveMeterUnit.CubicFeet:
                           meter.EventType = ParameterEvent.MeterCubicFeet;
                           break;
                       // 
                       case ZWaveMeterUnit.CubicMeters:
                           meter.EventType = ParameterEvent.MeterCubicMeters;
                           break;
                       */
                        // 
                        case ZWaveMeterUnit.Pulses:
                            meter.EventType = ParameterEvent.MeterPulses;
                            break;

                        default:
                            meter.EventType = ParameterEvent.Generic; // ?
                            break;
                    }
                }
            }
           

            return meter;
        }

        public static ZWaveValue ExtractMeterValueFromBytes(byte[] message, int valueOffset,
            out int rateType,
            out int meterType)
        {
            ZWaveValue result = new ZWaveValue {Scale = 0, Precision = 0, Value = 0};

            rateType = 0; // bits 1-3;
            meterType = 0; // bits 4-8;
            try
            {
                byte meterMeta = message[valueOffset - 2];
                byte meterTypeMask = 0x18, meterTypeShift = 0x1F,
                rateTypeMask = 0x60, rateTypeShift = 0x05;
                //
                rateType = (byte)((meterMeta & rateTypeMask) >> rateTypeShift);
                meterType = (byte)((meterMeta & meterTypeMask) >> meterTypeShift);

                byte sizeMask = 0x07,
                scaleMask = 0x18, scaleShift = 0x03,
                precisionMask = 0xe0, precisionShift = 0x05;
                //
                result.Size = (byte)(message[valueOffset - 1] & sizeMask);
                result.Precision = (byte)((message[valueOffset - 1] & precisionMask) >> precisionShift);
                result.Scale = (int)((message[valueOffset - 1] & scaleMask) >> scaleShift);

                //
                int value = ExtractSignedValue(message, valueOffset, result.Size);

                result.Value = ((double)value / (result.Precision == 0 ? 1 : Math.Pow(10D, result.Precision)));
            }
            catch
            {
                // TODO: report/handle exception
            }
            return result;
        }

        /// <summary>
        /// For METER report, we have an extra value that holds the PREVIOUS reported value, including time between previous and current (delta t).
        /// </summary>
        /// <param name="message"></param>
        /// <param name="valueOffset"></param>
        /// <param name="size"></param>
        /// <param name="precision"></param>
        /// <param name="scale"></param>
        /// <param name="deltaTime">out</param>
        /// <returns></returns>
        public static ZWaveValue ExtractPreviousValueFromBytes(byte[] message, int valueOffset, int size, int precision, int scale, out double deltaTime)
        {
            ZWaveValue result = new ZWaveValue { Scale = scale, Precision = precision, Value = 0 };
            deltaTime = 0;
            try
            {
                deltaTime = ((UInt32)message[valueOffset - 2]) * 256 + ((UInt32)message[valueOffset - 1]); // deltatime is 2 bytes.
                int value = ExtractSignedValue(message, valueOffset, size);
                result.Value = ((double)value / (precision == 0 ? 1 : Math.Pow(10D, precision)));
            }
            catch
            {
                // TODO: report/handle exception
            }
            return result;
        }

        // TODO: Move to shared.
        public static int ExtractSignedValue(byte[] message, int valueOffset, int size)
        {
            int value = 0;
            // Deal with sign extension. All values are signed. Sizes allowed are 1, 2 and 4 bytes.
            byte[] valueBytes = new byte[size];
            System.Array.Copy(message, valueOffset, valueBytes, 0, size);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(valueBytes);
            }

            if (size == 1)
            {
                value = (sbyte)valueBytes[0];
            }
            else if (size == 2)
            {
                value = BitConverter.ToInt16(valueBytes, 0);
            }
            else if (size == 4)
            {
                value = BitConverter.ToInt32(valueBytes, 0);
            }
            else
            {
                //TODO:  not supported by METER. Might want to support more for other types if needed? Would need to name LONG.
            }

            return value;

        }

    }
}
