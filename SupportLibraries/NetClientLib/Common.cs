using System;
using System.Net.Sockets;

namespace NetClientLib
{

    public class ConnectedStateChangedEventArgs
    {
        public bool Connected;

        public ConnectedStateChangedEventArgs(bool state)
        {
            Connected = state;
        }
    }

}

