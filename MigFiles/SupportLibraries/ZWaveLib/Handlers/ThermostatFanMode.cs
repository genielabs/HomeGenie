namespace ZWaveLib.Handlers
{
    public enum FanMode
    {
        AutoLow = 0x00,
        OnLow = 0x01,
        AutoHigh = 0x02,
        OnHigh = 0x03,
        Unknown4 = 0x04,
        Unknown5 = 0x05,
        Circulate = 0x06
    }

    public class ThermostatFanMode : ICommandClass
    {
        public CommandClassType GetTypeId()
        {
            return CommandClassType.ThermostatFanMode;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            return new ZWaveEvent(node, EventParameter.ThermostatFanMode, message[2], 0);
        }
                
        public static void Get(ZWaveNode node)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClassType.ThermostatFanMode, 
                (byte)CommandType.BasicGet
            });
        }

        public static void Set(ZWaveNode node, FanMode mode)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClassType.ThermostatFanMode, 
                (byte)CommandType.BasicSet, 
                (byte)mode
            });
        }
    }

}
