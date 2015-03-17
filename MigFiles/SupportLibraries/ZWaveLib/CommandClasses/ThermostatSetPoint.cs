using System.Dynamic;
using ZWaveLib.Values;
using System.Collections.Generic;

namespace ZWaveLib.CommandClasses
{
    public enum SetPointType
    {
        Unused = 0x00,
        Heating = 0x01,
        Cooling = 0x02,
        Unused03 = 0x03,
        Unused04 = 0x04,
        Unused05 = 0x05,
        Unused06 = 0x06,
        Furnace = 0x07,
        DryAir = 0x08,
        MoistAir = 0x09,
        AutoChangeover = 0x0A,
        HeatingEconomy = 0x0B,
        CoolingEconomy = 0x0C,
        HeatingAway = 0x0D
    }

    public class ThermostatSetPoint : ICommandClass
    {
        public CommandClass GetClassId()
        {
            return CommandClass.ThermostatSetPoint;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveValue zvalue = ZWaveValue.ExtractValueFromBytes(message, 4);
            var setPoint = GetSetPointData(node);
            setPoint.Precision = zvalue.Precision;
            setPoint.Scale = zvalue.Scale;
            setPoint.Size = zvalue.Size;
            setPoint.Value = zvalue.Value;
            dynamic ptype = new ExpandoObject();
            ptype.Type = (SetPointType)message[2];
            // convert from Fahrenheit to Celsius if needed
            ptype.Value = (zvalue.Scale == (int) ZWaveTemperatureScaleType.Fahrenheit
                ? SensorValue.FahrenheitToCelsius(zvalue.Value)
                : zvalue.Value);
            return new ZWaveEvent(node, EventParameter.ThermostatSetPoint, ptype, 0);
        }
                
        public static void Get(ZWaveNode node, SetPointType ptype)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatSetPoint, 
                (byte)Command.ThermostatSetPointGet,
                (byte)ptype
            });
        }

        public static void Set(ZWaveNode node, SetPointType ptype, double temperature)
        {
            List<byte> message = new List<byte>();
            message.AddRange(new byte[] { 
                (byte)CommandClass.ThermostatSetPoint, 
                (byte)Command.ThermostatSetPointSet, 
                (byte)ptype
            });
            var setPoint = ThermostatSetPoint.GetSetPointData(node);
            message.AddRange(ZWaveValue.GetValueBytes(temperature, setPoint.Precision, setPoint.Scale, setPoint.Size));
            node.SendRequest(message.ToArray());
        }

        public static ZWaveValue GetSetPointData(ZWaveNode node)
        {
            if (!node.Data.ContainsKey("SetPoint"))
            {
                node.Data.Add("SetPoint", new ZWaveValue());
            }
            return (ZWaveValue)node.Data["SetPoint"];
        }

    }
}
