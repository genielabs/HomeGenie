﻿/*
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

using NetClientLib;

namespace HomeGenie.Automation.Scripting
{

    /// <summary>
    /// Tcp client helper.
    /// Class instance accessor: **TcpClient**
    /// </summary>
    public class TcpClientHelper
    {
        private TcpClient tcpClient;
        private Action<byte[]> dataReceived;
        private Action<string> stringReceived;
        private Action<bool> statusChanged;
        private string serverAddress = "127.0.0.1";
        private string[] textEndOfLine = new string[] { "\n" };
        private string textBuffer = "";

        public TcpClientHelper()
        {
            tcpClient = new TcpClient();
        }

        /// <summary>
        /// Sets the server address to connect to.
        /// </summary>
        /// <returns>TcpClientHelper.</returns>
        /// <param name="port">Host DNS or IP address.</param>
        public TcpClientHelper Service(string address)
        {
            serverAddress = address;
            return this;
        }

        /// <summary>
        /// Connect to the server using the specified port.
        /// </summary>
        /// <param name="port">Port number.</param>
        public bool Connect(int port)
        {
            tcpClient.MessageReceived += tcpClient_MessageReceived;
            tcpClient.ConnectedStateChanged += tcpClient_ConnectedStateChanged;
            return tcpClient.Connect(this.serverAddress, port);
        }

        /// <summary>
        /// Disconnects from the remote host.
        /// </summary>
        public TcpClientHelper Disconnect()
        {
            tcpClient.Disconnect();
            tcpClient.MessageReceived -= tcpClient_MessageReceived;
            tcpClient.ConnectedStateChanged -= tcpClient_ConnectedStateChanged;
            return this;
        }

        /// <summary>
        /// Sends a string message.
        /// </summary>
        /// <param name="message">Message.</param>
        public void SendMessage(string message)
        {
            if (tcpClient.IsConnected)
            {
                byte[] msg = Encoding.UTF8.GetBytes(message);
                tcpClient.SendMessage(msg);
            }
        }

        /// <summary>
        /// Sends a raw data message.
        /// </summary>
        /// <param name="message">Message.</param>
        public void SendMessage(byte[] message)
        {
            if (tcpClient.IsConnected)
            {
                tcpClient.SendMessage(message);
            }
        }

        /// <summary>
        /// Sets the function to call when a new string message is received.
        /// </summary>
        /// <param name="receivedAction">Function or inline delegate.</param>
        public TcpClientHelper OnStringReceived(Action<string> receivedAction)
        {
            stringReceived = receivedAction;
            return this;
        }

        /// <summary>
        /// Sets the function to call when a new raw message is received.
        /// </summary>
        /// <param name="receivedAction">Function or inline delegate.</param>
        public TcpClientHelper OnMessageReceived(Action<byte[]> receivedAction)
        {
            dataReceived = receivedAction;
            return this;
        }

        /// <summary>
        /// Sets the function to call when the status of the connection changes.
        /// </summary>
        /// <param name="statusChangeAction">Function or inline delegate.</param>
        public TcpClientHelper OnStatusChanged(Action<bool> statusChangeAction)
        {
            statusChanged = statusChangeAction;
            return this;
        }

        /// <summary>
        /// Gets a value indicating whether the connection to the service is estabilished.
        /// </summary>
        /// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
        public bool IsConnected
        {
            get { return tcpClient.IsConnected; }
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

        private void tcpClient_MessageReceived(byte[] message)
        {
            if (dataReceived != null)
            {
                dataReceived(message);
            }
            if (stringReceived != null)
            {
                string textMessage = textBuffer + Encoding.UTF8.GetString(message);
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
                }
            }
        }

        private void tcpClient_ConnectedStateChanged(object sender, ConnectedStateChangedEventArgs statusargs)
        {
            // send last received text buffer before disconnecting
            if (!statusargs.Connected && !String.IsNullOrEmpty(textBuffer))
            {
                try { stringReceived(textBuffer); } catch { }
            }
            // reset text receive buffer
            textBuffer = "";
            if (statusChanged != null)
            {
                statusChanged(statusargs.Connected);
            }
        }

    }
}
