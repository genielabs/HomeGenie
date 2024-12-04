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
 *     Author: smeghead http://www.homegenie.it/forum/index.php?topic=474.msg2666#msg2666
 *     Project Homepage: https://homegenie.it
 */

using System;
using System.Text;

using NetClientLib;

namespace HomeGenie.Automation.Scripting
{

    /// <summary>
    /// UDP client helper.\n
    /// Class instance accessor: **UdpClient**
    /// </summary>
    [Serializable]
    public class UdpClientHelper
    {
        private UdpClient udpClient;
        private Action<byte[]> dataReceived;
        private Action<string> stringReceived;
        private Action<bool> statusChanged;
        private string[] textEndOfLine = new string[] { "\n" };
        private string textBuffer = "";

        public UdpClientHelper()
        {
            udpClient = new UdpClient();
        }

        /// <summary>
        /// Sets the client as a sender to address:port
        /// </summary>
        /// <returns>UdpClientHelper.</returns>
        /// <param name="address">Remote DNS or IP address.</param>
        /// <param name="port">port to send to</param>
        public UdpClientHelper Sender(string address, int port)
        {
            udpClient.Connect(address, port);
            return this;
        }

        /// <summary>
        /// Connects to the remote using the specified port and returns true if successful, false otherwise.
        /// </summary>
        /// <returns>bool</returns>
        /// <param name="port">Port number.</param>
        public bool Receiver(int port)
        {
            udpClient.MessageReceived += udpClient_MessageReceived;
            udpClient.ConnectedStateChanged += udpClient_ConnectedStateChanged;
            //
            return udpClient.Connect(port);
        }

        /// <summary>
        /// Disconnects from the remote host.
        /// </summary>
        public UdpClientHelper Disconnect()
        {
            udpClient.Disconnect();
            udpClient.MessageReceived -= udpClient_MessageReceived;
            udpClient.ConnectedStateChanged -= udpClient_ConnectedStateChanged;
            return this;
        }

        /// <summary>
        /// Sends a string message.
        /// </summary>
        /// <param name="message">Message.</param>
        public void SendMessage(string message)
        {
            if (udpClient.IsConnected)
            {
                byte[] msg = Encoding.UTF8.GetBytes(message);
                udpClient.SendMessage(msg);
            }
        }

        /// <summary>
        /// Sends a raw data message.
        /// </summary>
        /// <param name="message">Message.</param>
        public void SendMessage(byte[] message)
        {
            if (udpClient.IsConnected)
            {
                udpClient.SendMessage(message);
            }
        }

        /// <summary>
        /// Sets the function to call when a new string message is received.
        /// </summary>
        /// <param name="receivedAction">Function or inline delegate.</param>
        public UdpClientHelper OnStringReceived(Action<string> receivedAction)
        {
            stringReceived = receivedAction;
            return this;
        }

        /// <summary>
        /// Sets the function to call when a new raw message is received.
        /// </summary>
        /// <param name="receivedAction">Function or inline delegate.</param>
        public UdpClientHelper OnMessageReceived(Action<byte[]> receivedAction)
        {
            dataReceived = receivedAction;
            return this;
        }

        /// <summary>
        /// Sets the function to call when the status of the connection changes.
        /// </summary>
        /// <param name="statusChangeAction">Function or inline delegate.</param>
        public UdpClientHelper OnStatusChanged(Action<bool> statusChangeAction)
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
            get { return udpClient.IsConnected; }
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

        private void udpClient_MessageReceived(byte[] message)
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

        private void udpClient_ConnectedStateChanged(object sender, ConnectedStateChangedEventArgs statusargs)
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
