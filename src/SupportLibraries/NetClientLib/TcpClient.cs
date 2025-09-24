/*
   Copyright 2012-2025 G-Labs (https://github.com/genielabs)

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: https://homegenie.it
 */

using System;
using System.Net.Sockets;
using System.Threading;
using System.Text;

// adapted code from:
// http://msdn.microsoft.com/en-us/library/bew39x2a(v=vs.110).aspx
// Updated to use the new Net. 4.5 System.Net.Sockets.TcpClient

namespace NetClientLib
{
    public class TcpClient
    {

        public delegate void ConnectedStateChangedEvent(object sender, ConnectedStateChangedEventArgs statusargs);
        public event ConnectedStateChangedEvent ConnectedStateChanged;

        public delegate void MessageReceivedEvent(byte[] message);
        public event MessageReceivedEvent MessageReceived;

        private bool debug = false;

        private System.Net.Sockets.TcpClient client = null;
        private NetworkStream netStream;
        private byte[] readBuffer;

        private Thread receiverTask;

        public bool Connect(string remoteServer, int remotePort)
        {
            Disconnect();
            // Connect to a remote device.
            try
            {
                // Establish the remote endpoint for the socket.
                // The name of the
                // remote device is "host.contoso.com".

                // Create a TCP/IP client
                client = new System.Net.Sockets.TcpClient(remoteServer, remotePort);
                //client.NoDelay = true;
                //client.ReceiveTimeout = 10000;
                netStream = client.GetStream();
                readBuffer = new byte[client.ReceiveBufferSize];

                if (ConnectedStateChanged != null) ConnectedStateChanged(this, new ConnectedStateChangedEventArgs(true));

                receiverTask = new Thread(ReceiverLoop);
                receiverTask.Start();

            }
            catch (Exception e)
            {
                Disconnect();
                //
                Console.WriteLine(e.ToString());
            }

            return IsConnected;
        }

        public void Disconnect()
        {
            if (IsConnected && ConnectedStateChanged != null && client != null) ConnectedStateChanged(this, new ConnectedStateChangedEventArgs(false));
            // Release all allocated resources
            if (client != null)
            {
                try { client.Close(); } catch { }
                try { netStream.Close(); } catch { }
                client = null;
                netStream = null;
            }
            if (receiverTask != null && !receiverTask.Join(2000))
            {
                try
                {
                    receiverTask.Interrupt();
                } catch { }
            }
            receiverTask = null;
        }


        public bool Debug
        {
            get { return debug; }
            set { debug = value; }
        }

        public bool SendMessage(byte[] byteData)
        {
            // Begin sending the data to the remote device.
            return SendRaw(byteData);
        }

        private void ReceiverLoop(object obj)
        {
            while (client != null && client.Connected && netStream != null && netStream.CanRead)
            {
                try
                {
                    int bytesRead = netStream.Read(readBuffer, 0, readBuffer.Length);
                    if (bytesRead > 0)
                    {
                        byte[] rd = new byte[bytesRead];
                        Array.Copy(readBuffer, 0, rd, 0, bytesRead);
                        if (MessageReceived != null) MessageReceived(rd);

                        if (Debug)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("[TcpClient] > " + BitConverter.ToString(readBuffer));
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }
                    else
                    {
                        Disconnect();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            Disconnect();
        }

        private bool SendRaw(byte[] byteData)
        {
            bool success = false;
            if (netStream != null && netStream.CanWrite)
                try
                {
                    netStream.Write(byteData, 0, byteData.Length);
                    success = true;
                    if (Debug)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("[TcpClient] < " + BitConverter.ToString(byteData));
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
                catch (Exception ex)
                {
                    Disconnect();
                    Console.WriteLine(ex.ToString());
                }
            else
                Disconnect();
            return success;
        }

        private bool Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            return SendRaw(byteData);
        }

        public bool IsConnected
        {
            get
            {
                return (client != null && client.Connected ? true : false);
            }
        }
    }
}
