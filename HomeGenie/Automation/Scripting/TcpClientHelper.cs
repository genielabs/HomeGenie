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

using TcpClientLib;

namespace HomeGenie.Automation.Scripting
{

    public class TcpClientHelper
    {
        private TcpClient tcpClient;
        private Action<byte[]> dataReceived;
        private Action<string> stringReceived;
        private Action<bool> statusChanged;
        private string serverAddress = "127.0.0.1";

        public TcpClientHelper()
        {
            tcpClient = new TcpClient();
        }

        public TcpClientHelper Service(string address)
        {
            serverAddress = address;
            return this;
        }

        public TcpClientHelper OnStringReceived(Action<string> receivedAction)
        {
            stringReceived = receivedAction;
            return this;
        }

        public TcpClientHelper OnMessageReceived(Action<byte[]> receivedAction)
        {
            dataReceived = receivedAction;
            return this;
        }

        public TcpClientHelper OnStatusChanged(Action<bool> statusChangeAction)
        {
            statusChanged = statusChangeAction;
            return this;
        }

        public bool Connect(int port)
        {
            tcpClient.MessageReceived += tcpClient_MessageReceived;
            tcpClient.ConnectedStateChanged += tcpClient_ConnectedStateChanged;
            //
            return tcpClient.Connect(this.serverAddress, port);
        }

        public TcpClientHelper Disconnect()
        {
            tcpClient.Disconnect();
            tcpClient.MessageReceived -= tcpClient_MessageReceived;
            tcpClient.ConnectedStateChanged -= tcpClient_ConnectedStateChanged;
            return this;
        }

        public void SendMessage(string message)
        {
            if (tcpClient.IsConnected)
            {
                byte[] msg = Encoding.UTF8.GetBytes(message);
                tcpClient.SendMessage(msg);
            }
        }

        public void SendMessage(byte[] message)
        {
            if (tcpClient.IsConnected)
            {
                tcpClient.SendMessage(message);
            }
        }

        public bool IsConnected
        {
            get { return tcpClient.IsConnected; }
        }


        private void tcpClient_MessageReceived(byte[] message)
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

        private void tcpClient_ConnectedStateChanged(object sender, ConnectedStateChangedEventArgs statusargs)
        {
            if (statusChanged != null)
            {
                statusChanged(statusargs.Connected);
            }
        }

    }
}
