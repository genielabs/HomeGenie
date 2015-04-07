namespace ZWaveLib.CommandClasses
{
    public class DoorLock : ICommandClass
    {
        public CommandClass GetClassId()
        {
            return CommandClass.DoorLock;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;
            byte cmdType = message[1];

            if (cmdType == (byte)Command.DoorLock_Report) {
                int lockState;

                if (message[2] == 0xFF)
                    lockState = 6;
                else
                    lockState = System.Convert.ToInt32(message[2].ToString("X2"));

                if (lockState > 6) {
                    lockState = 7;
                }

                string resp;

                if (lockState == 0) {
                    resp = "Unlocked";
                }
                else if (lockState == 6)
                {
                    resp = "Locked";
                }
                else {
                    resp = "Unknown";
                }

                var messageEvent = new ZWaveEvent(node, EventParameter.DoorLockStatus, resp, 0);
                node.RaiseUpdateParameterEvent(messageEvent);

            }

            return nodeEvent;
        }

        public static void Set(ZWaveNode node, int value)
        {

            node.SendRequest(new byte[] { 
                (byte)CommandClass.DoorLock, 
                (byte)Command.DoorLock_Set,
                byte.Parse(value.ToString())
            });
            
        }

        public static void Get(ZWaveNode node)
        {

            node.SendRequest(new byte[] { 
                (byte)CommandClass.DoorLock, 
                (byte)Command.DoorLock_Get
            });

        }

        public static void Unlock(ZWaveNode node)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.Basic, 
                (byte)Command.BasicGet 
            });
        }
    }
}
