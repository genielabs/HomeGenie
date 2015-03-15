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
        public byte GetCommandClassId()
        {
            return 0x44;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            return new ZWaveEvent(node, EventParameter.ThermostatFanMode, message[2], 0);
        }
    }
}
