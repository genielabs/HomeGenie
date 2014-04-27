/*
    This file is part of HomeGenie Project source code.

    HomeGenie is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    HomeGenie is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with HomeGenie.  If not, see <http://www.gnu.org/licenses/>.  
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: http://homegenie.it
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SerialPortLib;

namespace HomeGenie.Automation.Scripting
{

    public class SerialPortHelper
    {
        private SerialPortInput serialPort;
        private Action<byte[]> dataReceived;
        private Action<string> stringReceived;
        private Action<bool> statusChanged;
        private string portName = "";

        public SerialPortHelper()
        {
            serialPort = new SerialPortInput();
        }

        public SerialPortHelper WithName(string port)
        {
            portName = port;
            return this;
        }

        public SerialPortHelper OnStringReceived(Action<string> receivedAction)
        {
            stringReceived = receivedAction;
            return this;
        }

        public SerialPortHelper OnMessageReceived(Action<byte[]> receivedAction)
        {
            dataReceived = receivedAction;
            return this;
        }

        public SerialPortHelper OnStatusChanged(Action<bool> statusChangeAction)
        {
            statusChanged = statusChangeAction;
            return this;
        }

        public bool Connect()
        {
            return Connect(115200);
        }

        public bool Connect(int baudRate)
        {
            serialPort.MessageReceived += serialPort_MessageReceived;
            serialPort.ConnectedStateChanged += serialPort_ConnectedStateChanged;
            //
            serialPort.SetPort(portName, baudRate);
            return serialPort.Connect();
        }

        public SerialPortHelper Disconnect()
        {
            serialPort.Disconnect();
            serialPort.MessageReceived -= serialPort_MessageReceived;
            serialPort.ConnectedStateChanged -= serialPort_ConnectedStateChanged;
            return this;
        }

        public void SendMessage(string message)
        {
            if (serialPort.IsConnected)
            {
                byte[] msg = Encoding.UTF8.GetBytes(message);
                serialPort.SendMessage(msg);
            }
        }

        public void SendMessage(byte[] message)
        {
            if (serialPort.IsConnected)
            {
                serialPort.SendMessage(message);
            }
        }

        public bool IsConnected
        {
            get { return serialPort.IsConnected; }
        }


        private void serialPort_MessageReceived(byte[] message)
        {
            if (dataReceived != null)
            {
                dataReceived(message);
            }
            if (stringReceived != null)
            {
                try
                {
                    stringReceived(Encoding.UTF8.GetString(message));
                }
                catch { }
            }
        }

        private void serialPort_ConnectedStateChanged(object sender, ConnectedStateChangedEventArgs statusargs)
        {
            if (statusChanged != null)
            {
                statusChanged(statusargs.Connected);
            }
        }

    }
}
