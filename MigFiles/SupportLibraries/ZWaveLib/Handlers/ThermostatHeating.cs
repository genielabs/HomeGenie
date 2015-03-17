namespace ZWaveLib.Handlers
{
    public class ThermostatHeating : ICommandClass
    {
        public CommandClassType GetCommandClassId()
        {
            return CommandClassType.ThermostatHeating;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            return new ZWaveEvent(node, EventParameter.ThermostatHeating, message[2], 0);
        }
    }
}
