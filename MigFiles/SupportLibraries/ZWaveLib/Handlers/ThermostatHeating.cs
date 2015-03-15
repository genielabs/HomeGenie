namespace ZWaveLib.Handlers
{
    class ThermostatHeating : ICommandClass
    {
        public byte GetCommandClassId()
        {
            return 0x38;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            return new ZWaveEvent(node, EventParameter.ThermostatHeating, message[2], 0);
        }
    }
}
