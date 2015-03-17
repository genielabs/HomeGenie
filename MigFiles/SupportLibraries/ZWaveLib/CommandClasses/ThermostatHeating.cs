namespace ZWaveLib.CommandClasses
{
    public class ThermostatHeating : ICommandClass
    {
        public CommandClass GetClassId()
        {
            return CommandClass.ThermostatHeating;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            return new ZWaveEvent(node, EventParameter.ThermostatHeating, message[2], 0);
        }
    }
}
