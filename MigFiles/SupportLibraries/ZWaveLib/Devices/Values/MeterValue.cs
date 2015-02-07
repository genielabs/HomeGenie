using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZWaveLib.Devices.Values
{

    public enum ZWaveMeterUnit : int
    {
        ElectricMeter_kWh = 0x00,
        ElectricMeter_kVAh = 0x01,
        ElectricMeter_Watt = 0x02,
        CubicMeters = 0x00,
        CubicFeet = 0x01,
        USGallons = 0x02,
        Pulses = 0x03,
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
    /// + Support other meter types (only supports energy at the moment).
    /// + When HG supports way to specify rate type, we can use our ZWaveRateType that we have parsed here.
    /// 
    /// </summary>
    public class MeterValue
    {
        public ParameterType EventType = ParameterType.METER_WATT;
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
            // TODO: If message is NOT a meter value, show error.
            int dataStart = 11;

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
                    dataStart = 14; // TODO: Confirm this.
                    //byte encappedCmdClass = message[10];
                    //byte encappedCmd = message[11];
                }

            }

            MeterValue meter = new MeterValue();
            int size, precision, scale, rateType, meterType;

            meter.Value = ExtractMeterValueFromBytes(message, dataStart, out size, out precision, out scale, out rateType, out meterType);
            if (Enum.IsDefined(typeof(ZWaveRateType), rateType))
            {
                meter.RateType = (ZWaveRateType)rateType;
            }
            if (Enum.IsDefined(typeof(ZWaveMeterType), meterType))
            {
                meter.MeterType = (ZWaveMeterType)meterType;
            }

            double deltaTime;
            double previousValue = ExtractPreviousValueFromBytes(message, dataStart + size, size, precision, scale, out deltaTime); // Func does not look for prec/size byte.

            if (previousValue == DeltaTimeValue_NoPreviousValue)
            {
                // TODO: Report that the specified value we are sending is NOT really a previous value.
            }
            else if (previousValue == DeltaTimeValue_UnknownDeltaTime)
            {
                // TODO: Report that the deltaT is not available (instead of showing 65536 seconds).
            }


            if (meter.MeterType == ZWaveMeterType.ElectricMeter)
            {
                switch (meter.MeterUnit)
                {
                    // Accumulated power consumption kW/h
                    case ZWaveMeterUnit.ElectricMeter_kWh:
                        meter.EventType = ParameterType.METER_KW_HOUR;
                        break;
                    // Instant power consumption Watt
                    case ZWaveMeterUnit.ElectricMeter_Watt:
                        meter.EventType = ParameterType.METER_WATT;
                        break;
                    // Accumulated power consumption kilo Volt Ampere / hours (kVA/h)
                    case ZWaveMeterUnit.ElectricMeter_kVAh:
                        meter.EventType = ParameterType.METER_KVA_HOUR;
                        break;
                    default:
                        meter.EventType = ParameterType.METER_WATT;
                        break;
                }
            }

            return meter;
        }

        public static double ExtractMeterValueFromBytes(byte[] message, int valueOffset,
            out int size,
            out int precision,
            out int scale,
            out int rateType,
            out int meterType)
        {
            double result = 0;
            rateType = 0; // bits 1-3;
            meterType = 0; // bits 4-8;
            scale = 0;
            size = 0;
            precision = 0;
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
                size = (byte)(message[valueOffset - 1] & sizeMask);
                precision = (byte)((message[valueOffset - 1] & precisionMask) >> precisionShift);
                scale = (int)((message[valueOffset - 1] & scaleMask) >> scaleShift);

                //
                int value = ExtractSignedValue(message, valueOffset, size);

                result = ((double)value / (precision == 0 ? 1 : Math.Pow(10D, precision)));
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
        public static double ExtractPreviousValueFromBytes(byte[] message, int valueOffset, int size, int precision, int scale, out double deltaTime)
        {
            double result = 0;
            deltaTime = 0;
            try
            {
                deltaTime = ((UInt32)message[valueOffset - 2]) * 256 + ((UInt32)message[valueOffset - 1]); // deltatime is 2 bytes.
                int value = ExtractSignedValue(message, valueOffset, size);
                result = ((double)value / (precision == 0 ? 1 : Math.Pow(10D, precision)));
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

        #region "OLD"
        public static double OLDExtractMeterValueFromBytes(byte[] message, int valueOffset,
            out int size,
            out int precision,
            out int scale,
            out int rateType,
            out int meterType)
        {
            double result = 0;
            rateType = 0; // bits 1-3;
            meterType = 0; // bits 4-8;
            scale = 0;
            size = 0;
            precision = 0;
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
                size = (byte)(message[valueOffset - 1] & sizeMask);
                precision = (byte)((message[valueOffset - 1] & precisionMask) >> precisionShift);
                scale = (int)((message[valueOffset - 1] & scaleMask) >> scaleShift);
                //
                int value = 0;
                byte i;
                for (i = 0; i < size; ++i)
                {
                    value <<= 8;
                    value |= (int)message[i + (int)valueOffset];
                }
                // Deal with sign extension. All values are signed
                value = OLDHandleSignedValue(message, value, valueOffset, size);
                //
                result = ((double)value / (precision == 0 ? 1 : Math.Pow(10D, precision)));
            }
            catch
            {
                // TODO: report/handle exception
            }
            return result;
        }

        // TODO: Move to common util.
        public static int OLDHandleSignedValue(byte[] message, int value, int valueOffset, int size)
        {
            //int value = 0;
            // Deal with sign extension. All values are signed. Sizes allowed are 1, 2 and 4 bytes.

            // MSB is signed
            if (size == 1 && (message[valueOffset] & 0x80) > 0)
            {
                value = (int)((uint)value | 0xffffff00);
            }
            else if (size == 2 && (message[valueOffset] & 0x8000) > 0)
            {
                value = (int)((uint)value | 0xffff0000);
            }
            else if (size == 4 && (message[valueOffset] & 0x800000) > 0)
            {
                value = (int)((uint)value | 0xff000000);
            }
            else
            {
                // TODO: Invalid.
            }

            return value;

        }
        #endregion






    }
}
