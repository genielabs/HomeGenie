namespace ZWaveLib.Handlers
{
    public enum FanState
    {
        Idle = 0x00,
        Running = 0x01,
        RunningHigh = 0x02,
        State03 = 0x03,
        State04 = 0x04,
        State05 = 0x05,
        State06 = 0x06,
        State07 = 0x07,
        State08 = 0x08,
        State09 = 0x09,
        State10 = 0x0A,
        State11 = 0x0B,
        State12 = 0x0C,
        State13 = 0x0D,
        State14 = 0x0E,
        State15 = 0x0F
    }

    class ThermostatFanState :ICommandClass
    {
        public byte GetCommandClassId()
        {
            return 0x45;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            return new ZWaveEvent(node, EventParameter.ThermostatFanState, message[2], 0);
        }
    }
}
