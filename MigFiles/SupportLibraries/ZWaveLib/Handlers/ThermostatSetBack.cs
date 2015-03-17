namespace ZWaveLib.Handlers
{
    public class ThermostatSetBack : ICommandClass
    {
        public CommandClass GetClassId()
        {
            return CommandClass.ThermostatSetBack;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            return new ZWaveEvent(node, EventParameter.ThermostatSetBack, message[2], 0);
        }
    }
}
