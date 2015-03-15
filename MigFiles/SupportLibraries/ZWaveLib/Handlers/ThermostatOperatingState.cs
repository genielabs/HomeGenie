namespace ZWaveLib.Handlers
{
    public enum OperatingState
    {
        Idle = 0x00,
        Heating = 0x01,
        Cooling = 0x02,
        FanOnly = 0x03,
        PendingHeat = 0x04,
        PendingCool = 0x05,
        VentEconomizer = 0x06,
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

    public class ThermostatOperatingState : ICommandClass
    {
        public byte GetCommandClassId()
        {
            return 0x42;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            return new ZWaveEvent(node, EventParameter.ThermostatOperatingState, message[2], 0);
        }

        public static void GetOperatingState(ZWaveNode node)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.ThermostatOperatingState, 
                (byte)Command.BasicGet
            });
        }
    }
}
