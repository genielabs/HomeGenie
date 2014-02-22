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
        private SerialPortInput _serialport;
        private Action<byte[]> _datareceived;
        private Action<string> _stringreceived;
        private Action<bool> _statuschanged;
        private string _portname = "/dev/ttyUSB0";

        public SerialPortHelper()
        {
            _serialport = new SerialPortInput();
        }

        public SerialPortHelper WithName(string portname)
        {
            _portname = portname;
            return this;
        }

        public SerialPortHelper OnStringReceived(Action<string> receivedaction)
        {
            _stringreceived = receivedaction;
            return this;
        }

        public SerialPortHelper OnMessageReceived(Action<byte[]> receivedaction)
        {
            _datareceived = receivedaction;
            return this;
        }

        public SerialPortHelper OnStatusChanged(Action<bool> statuschangeaction )
        {
            _statuschanged = statuschangeaction;
            return this;
        }

        public bool Connect()
        {
            return Connect(115200);
        }

        public bool Connect(int baudrate)
        {
            _serialport.MessageReceived += _serialport_MessageReceived;
            _serialport.ConnectedStateChanged += _serialport_ConnectedStateChanged;
            //
            _serialport.SetPort(_portname, baudrate);
            return _serialport.Connect();
        }

        public SerialPortHelper Disconnect()
        {
            _serialport.Disconnect();
            _serialport.MessageReceived -= _serialport_MessageReceived;
            _serialport.ConnectedStateChanged -= _serialport_ConnectedStateChanged;
            return this;
        }

        public void SendMessage(string message)
        {
            if (_serialport.IsConnected)
            {
                byte[] msg = Encoding.UTF8.GetBytes(message);
                _serialport.SendMessage(msg);
            }
        }

        public void SendMessage(byte[] message)
        {
            if (_serialport.IsConnected)
            {
                _serialport.SendMessage(message);
            }
        }

        public bool IsConnected
        {
            get { return _serialport.IsConnected; }
        }


        private void _serialport_MessageReceived(byte[] message)
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

        private void _serialport_ConnectedStateChanged(object sender, ConnectedStateChangedEventArgs statusargs)
        {
            if (_statuschanged != null)
            {
                _statuschanged(statusargs.Connected);
            }
        }

    }
}
