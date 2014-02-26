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
        private TcpClient _tcpclient;
        private Action<byte[]> _datareceived;
        private Action<string> _stringreceived;
        private Action<bool> _statuschanged;
        private string _serverip = "127.0.0.1";

        public TcpClientHelper()
        {
            _tcpclient = new TcpClient();
        }

        public TcpClientHelper Service(string ipaddress)
        {
            _serverip = ipaddress;
            return this;
        }

        public TcpClientHelper OnStringReceived(Action<string> receivedaction)
        {
            _stringreceived = receivedaction;
            return this;
        }

        public TcpClientHelper OnMessageReceived(Action<byte[]> receivedaction)
        {
            _datareceived = receivedaction;
            return this;
        }

        public TcpClientHelper OnStatusChanged(Action<bool> statuschangeaction)
        {
            _statuschanged = statuschangeaction;
            return this;
        }

        public bool Connect(int port)
        {
            _tcpclient.MessageReceived += _tcpclient_MessageReceived;
            _tcpclient.ConnectedStateChanged += _tcpclient_ConnectedStateChanged;
            //
            return _tcpclient.Connect(this._serverip, port);
        }

        public TcpClientHelper Disconnect()
        {
            _tcpclient.Disconnect();
            _tcpclient.MessageReceived -= _tcpclient_MessageReceived;
            _tcpclient.ConnectedStateChanged -= _tcpclient_ConnectedStateChanged;
            return this;
        }

        public void SendMessage(string message)
        {
            if (_tcpclient.IsConnected)
            {
                byte[] msg = Encoding.UTF8.GetBytes(message);
                _tcpclient.SendMessage(msg);
            }
        }

        public void SendMessage(byte[] message)
        {
            if (_tcpclient.IsConnected)
            {
                _tcpclient.SendMessage(message);
            }
        }

        public bool IsConnected
        {
            get { return _tcpclient.IsConnected; }
        }


        private void _tcpclient_MessageReceived(byte[] message)
        {
            if (_datareceived != null)
            {
                _datareceived(message);
            }
            if (_stringreceived != null)
            {
                try
                {
                    _stringreceived(Encoding.UTF8.GetString(message));
                }
                catch { }
            }
        }

        private void _tcpclient_ConnectedStateChanged(object sender, ConnectedStateChangedEventArgs statusargs)
        {
            if (_statuschanged != null)
            {
                _statuschanged(statusargs.Connected);
            }
        }

    }
}
