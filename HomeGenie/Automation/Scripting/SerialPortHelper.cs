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
using System.IO.Ports;

using SerialPortLib;

namespace HomeGenie.Automation.Scripting
{
    /// <summary>
    /// Serial port helper.\n
    /// Class instance accessor: **SerialPort**
    /// </summary>
    [Serializable]
    public class SerialPortHelper
    {
        private SerialPortInput serialPort;
        private Action<byte[]> dataReceived;
        private Action<string> stringReceived;
        private Action<bool> statusChanged;
        private string portName = "";
        private string[] textEndOfLine = new string[] { "\n" };
        private string textBuffer = "";

        public SerialPortHelper()
        {
            serialPort = new SerialPortInput();
        }

        /// <summary>
        /// Selects the serial port with the specified name.
        /// </summary>
        /// <returns>SerialPortHelper.</returns>
        /// <param name="port">Port name.</param>
        public SerialPortHelper WithName(string port)
        {
            portName = port;
            return this;
        }

        /// <summary>
        /// Connect the serial port (@115200bps).
        /// </summary>
        public bool Connect()
        {
            return Connect(115200);
        }

        /// <summary>
        /// Connect the serial port at the specified speed.
        /// </summary>
        /// <param name="baudRate">Baud rate.</param>
        /// <param name="stopBits">Stop Bits.</param>
        /// <param name="parity">Parity.</param>
        /// 
        public bool Connect(int baudRate, StopBits stopBits = StopBits.One, Parity parity = Parity.None)
        {
            serialPort.MessageReceived += SerialPort_MessageReceived;
            serialPort.ConnectionStatusChanged += SerialPort_ConnectionStatusChanged;
            serialPort.SetPort(portName, baudRate);
            return serialPort.Connect();
        }

        /// <summary>
        /// Disconnects the serial port.
        /// </summary>
        public SerialPortHelper Disconnect()
        {
            serialPort.Disconnect();
            serialPort.MessageReceived -= SerialPort_MessageReceived;
            serialPort.ConnectionStatusChanged -= SerialPort_ConnectionStatusChanged;
            return this;
        }

        /// <summary>
        /// Sends a string message.
        /// </summary>
        /// <param name="message">Message.</param>
        public void SendMessage(string message)
        {
            if (serialPort.IsConnected)
            {
                byte[] msg = Encoding.UTF8.GetBytes(message);
                serialPort.SendMessage(msg);
            }
        }

        /// <summary>
        /// Sends a raw data message.
        /// </summary>
        /// <param name="message">Message.</param>
        public void SendMessage(byte[] message)
        {
            if (serialPort.IsConnected)
            {
                serialPort.SendMessage(message);
            }
        }

        /// <summary>
        /// Sets the function to call when a new string message is received.
        /// </summary>
        /// <param name="receivedAction">Function or inline delegate.</param>
        public SerialPortHelper OnStringReceived(Action<string> receivedAction)
        {
            stringReceived = receivedAction;
            return this;
        }

        /// <summary>
        /// Sets the function to call when a new raw message is received.
        /// </summary>
        /// <param name="receivedAction">Function or inline delegate.</param>
        public SerialPortHelper OnMessageReceived(Action<byte[]> receivedAction)
        {
            dataReceived = receivedAction;
            return this;
        }

        /// <summary>
        /// Sets the function to call when the status of the serial connection changes.
        /// </summary>
        /// <param name="statusChangeAction">Function or inline delegate.</param>
        public SerialPortHelper OnStatusChanged(Action<bool> statusChangeAction)
        {
            statusChanged = statusChangeAction;
            return this;
        }

        /// <summary>
        /// Gets a value indicating whether the serial port is connected.
        /// </summary>
        /// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
        public bool IsConnected
        {
            get { return serialPort.IsConnected; }
        }

        /// <summary>
        /// Gets or sets the end of line delimiter used in text messaging.
        /// </summary>
        /// <value>The end of line.</value>
        public string EndOfLine
        {
            get { return textEndOfLine[0]; }
            set { textEndOfLine = new string[] { value }; }
        }

        public void Reset()
        {
            Disconnect();
        }

        private void SerialPort_MessageReceived(object sender, MessageReceivedEventArgs args)
        {
            if (dataReceived != null)
            {
                dataReceived(args.Data);
            }
            if (stringReceived != null)
            {
                string textMessage = textBuffer + Encoding.UTF8.GetString(args.Data);
                if (String.IsNullOrEmpty(textEndOfLine[0]))
                {
                    // raw string receive
                    try { stringReceived(textMessage); } catch { }
                }
                else
                {
                    // text line based string receive
                    textBuffer = "";
                    if (textMessage.Contains(textEndOfLine[0]))
                    {
                        string[] lines = textMessage.Split(textEndOfLine, StringSplitOptions.RemoveEmptyEntries);
                        for (int l = 0; l < lines.Length - (textMessage.EndsWith(textEndOfLine[0]) ? 0 : 1); l++)
                        {
                            try { stringReceived(lines[l]); } catch { }
                        }
                        if (!textMessage.EndsWith(textEndOfLine[0]))
                        {
                            textBuffer = lines[lines.Length - 1];
                        }
                    }
                    else
                    {
                        textBuffer = textMessage;
                    }
                }
            }
        }

        private void SerialPort_ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs args)
        {
            // send last received text buffer before disconnecting
            if (!args.Connected && !String.IsNullOrEmpty(textBuffer))
            {
                try { stringReceived(textBuffer); } catch { }
            }
            // reset text receive buffer
            textBuffer = "";
            if (statusChanged != null)
            {
                statusChanged(args.Connected);
            }
        }

    }
}
