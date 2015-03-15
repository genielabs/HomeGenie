namespace ZWaveLib.Handlers
{
    class ThermostatSetBack : ICommandClass
    {
        public byte GetCommandClassId()
        {
            return 0x47;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            return new ZWaveEvent(node, EventParameter.ThermostatSetBack, message[2], 0);
        }
    }
}
