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
 *     Project Homepage: http://homegenie.it
 */

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections.Generic;

namespace NetClientLib
{

    public class UdpClient
    {
        // State object for receiving data from remote device.
        private class StateObject
        {
            // UDP Client
            public System.Net.Sockets.UdpClient workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 256;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
            //
            //public List<byte> message = new List<byte>();
        }

        public delegate void ConnectedStateChangedEvent(object sender, ConnectedStateChangedEventArgs statusargs);
        public event ConnectedStateChangedEvent ConnectedStateChanged;

        public delegate void MessageReceivedEvent(byte[] message);
        public event MessageReceivedEvent MessageReceived;

        // ManualResetEvent instances signal completion.
        private ManualResetEvent connectDone = new ManualResetEvent(false);
        private ManualResetEvent sendDone = new ManualResetEvent(false);
        private ManualResetEvent receiveDone = new ManualResetEvent(false);

        private bool debug = false;

        // The response from the remote device.
        private byte[] rawResponse = null;
        // create 2 UDP clients, one used for send and the other for receive
        private System.Net.Sockets.UdpClient clientSend = null;
        private System.Net.Sockets.UdpClient clientReceive = null;
        private IPEndPoint localEP = null;

        private Thread receiverTask;

        // Connect for receiving messages
        public bool Connect(int localPort)
        {
            Disconnect();
            receiveDone.Reset();
            // Connect to a local port.
            try
            {
                localEP = new IPEndPoint(IPAddress.Any, localPort);

                // Create a UDP Client.
                clientReceive = new System.Net.Sockets.UdpClient(localEP);

                if (ConnectedStateChanged != null) ConnectedStateChanged(this, new ConnectedStateChangedEventArgs(true));

                // Setup the receiver client thread
                receiverTask = new Thread(ReceiverLoop);
                receiverTask.Start();

                // Signal that the connection has been made.
                connectDone.Set();
            }
            catch (Exception e)
            {
                Disconnect();
                //
                Console.WriteLine(e.ToString());
            }

            return IsConnected;
        }

        // Connect for sending data
        public bool Connect(string remoteServer, int remotePort)
        {
            Disconnect();
            connectDone.Reset();
            sendDone.Reset();
            if (clientSend != null)
            {
                clientSend.Close();
                clientSend = null;
            }
            // Connect to a remote device.
            try
            {
                // Create a UDPClient
                clientSend = new System.Net.Sockets.UdpClient(remoteServer, remotePort);

                if (ConnectedStateChanged != null) ConnectedStateChanged(this, new ConnectedStateChangedEventArgs(true));

                // Signal that the connection has been made.
                connectDone.Set();
            }
            catch (Exception e)
            {
                if (clientSend != null)
                {
                    clientSend.Close();
                    clientSend = null;
                }                //
                Console.WriteLine(e.ToString());
            }

            return IsConnected;
        }

        public void Disconnect()
        {
            DisconnectReceiver();
            try
            {
                // Release the socket.
                clientSend.Close();
            }
            catch { }
            clientSend = null;
            //
            if (ConnectedStateChanged != null) ConnectedStateChanged(this, new ConnectedStateChangedEventArgs(false));
        }


        public bool Debug
        {
            get { return debug; }
            set { debug = value; }
        }

        public byte[] ReceiveMessage()
        {
            rawResponse = null;

            // Receive the response from the remote device.
            if (Receive(clientReceive)) receiveDone.WaitOne(10000);

            return rawResponse;
        }

        public void SendMessage(byte[] byteData)
        {
            // Begin sending the data to the remote device.
            if (SendRaw(byteData)) sendDone.WaitOne();
        }

        private void DisconnectReceiver()
        {
            try { receiverTask.Abort(); }
            catch { }
            receiverTask = null;
            try
            {
                // Release the socket.
                clientReceive.Close();
            }
            catch { }
            clientReceive = null;
        }

        private void ReceiverLoop(object obj)
        {
            while (clientReceive != null)
            {
                byte[] msg = ReceiveMessage();
                if (msg != null)
                {
                    if (Debug)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("UDP > " + BitConverter.ToString(msg));
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
                else
                {
                    Thread.Sleep(300);
                }
            }
            Disconnect();
        }

        private bool Receive(System.Net.Sockets.UdpClient client)
        {
            bool success = true;
            try
            {
                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.
                clientReceive.BeginReceive(new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                success = false;
                Console.WriteLine(e.ToString());
            }
            return success;
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                System.Net.Sockets.UdpClient client = state.workSocket;

                // Read data from the remote device.
                byte[] bytesRead = client.EndReceive(ar, ref localEP);

                if (bytesRead.Length > 0)
                {
                    byte[] rd = new byte[bytesRead.Length];
                    Array.Copy(bytesRead, 0, rd, 0, bytesRead.Length);
                    if (MessageReceived != null) MessageReceived(rd);

                    // Signal that all bytes have been received.
                    receiveDone.Set();

                    // Continue receiving data.
                    client.BeginReceive(new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    Disconnect();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private bool SendRaw(byte[] byteData)
        {
            bool success = true;
            try
            {
                clientSend.BeginSend(byteData, byteData.Length, new AsyncCallback(SendCallback), clientSend);
                //
                if (Debug)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("UDP < " + BitConverter.ToString(byteData));
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            catch (Exception ex)
            {
                success = false;
                Console.WriteLine(ex.ToString());
            }
            return success;
        }

        private void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            //
            try
            {
                // Begin sending the data to the remote device.
                client.BeginSend(byteData, 0, byteData.Length, 0,
                                 new AsyncCallback(SendCallback), client);
                //
                if (Debug)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("TCO[" + client.RemoteEndPoint.ToString() + "] < " + BitConverter.ToString(byteData));
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                System.Net.Sockets.UdpClient client = (System.Net.Sockets.UdpClient)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                if (Debug)
                {
                    Console.WriteLine("Sent {0} bytes to client.", bytesSent);
                }

                // Signal that all bytes have been sent.
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public bool IsConnected
        {
            get
            {
                return (clientSend != null || clientReceive != null);
            }
        }
    }
}
