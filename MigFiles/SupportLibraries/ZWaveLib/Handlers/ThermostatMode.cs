namespace ZWaveLib.Handlers
{
    public enum Mode
    {
        Off = 0x00,
        Heat = 0x01,
        Cool = 0x02,
        Auto = 0x03,
        AuxHeat = 0x04,
        Resume = 0x05,
        FanOnly = 0x06,
        Furnace = 0x07,
        DryAir = 0x08,
        MoistAir = 0x09,
        AutoChangeover = 0x0A,
        HeatEconomy = 0x0B,
        CoolEconomy = 0x0C,
        Away = 0x0D
    }

    public class ThermostatMode : ICommandClass
    {
        public CommandClassType GetTypeId()
        {
            return CommandClassType.ThermostatMode;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
           return  new ZWaveEvent(node, EventParameter.ThermostatMode, (Mode)message[2], 0);
        }

        public static void Get(ZWaveNode node)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClassType.ThermostatMode, 
                (byte)CommandType.BasicGet
            });
        }       

        public static void Set(ZWaveNode node, Mode mode)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClassType.ThermostatMode, 
                (byte)CommandType.BasicSet, 
                (byte)mode
            });
        }
    }
}
