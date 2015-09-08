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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections.Generic;

// adapted code from:
// http://msdn.microsoft.com/en-us/library/bew39x2a(v=vs.110).aspx

namespace NetClientLib
{

    public class TcpClient
    {
        // State object for receiving data from remote device.
        private class StateObject
        {
            // Client socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 256;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
        }

        public delegate void ConnectedStateChangedEvent(object sender, ConnectedStateChangedEventArgs statusargs);
        public event ConnectedStateChangedEvent ConnectedStateChanged;

        public delegate void MessageReceivedEvent(byte[] message);
        public event MessageReceivedEvent MessageReceived;

        // The port number for the remote device.
        //private const int port = 11000;

        // ManualResetEvent instances signal completion.
        private ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        private bool debug = false;

        // The response from the remote device.
        private byte[] rawResponse = null;
        private Socket client = null;

        private Thread receiverTask;

        public bool Connect(string remoteServer, int remotePort)
        {
            Disconnect();
            connectDone.Reset();
            receiveDone.Reset();
            sendDone.Reset();
            // Connect to a remote device.
            try
            {
                // Establish the remote endpoint for the socket.
                // The name of the 
                // remote device is "host.contoso.com".
                IPAddress ipAddress;
                if (!IPAddress.TryParse(remoteServer, out ipAddress))
                {
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(remoteServer);
                    ipAddress = ipHostInfo.AddressList[0];
                }
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, remotePort);

                // Create a TCP/IP socket.
                client = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.
                client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();

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
            if (ConnectedStateChanged != null && client != null) ConnectedStateChanged(this, new ConnectedStateChangedEventArgs(false));
            // 
            // Release the socket.
            if (client != null)
            {
                try { client.Shutdown(SocketShutdown.Both); } catch { }
                try { client.Disconnect(false); } catch { }
                try { client.Close(); } catch { }
                client = null;
            }
            //
            try { receiverTask.Abort(); }
            catch { }
            receiverTask = null;
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
            if (Receive(client)) receiveDone.WaitOne(10000);

            return rawResponse;
        }

        public void SendMessage(byte[] byteData)
        {
            // Begin sending the data to the remote device.
            if (SendRaw(byteData)) sendDone.WaitOne();
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                var client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                if (Debug)
                {
                    Console.WriteLine("Socket connected to {0}",
                        client.RemoteEndPoint.ToString());
                }

                if (ConnectedStateChanged != null) ConnectedStateChanged(this, new ConnectedStateChangedEventArgs(true));

                receiverTask = new Thread(ReceiverLoop);
                receiverTask.Start();
                //

                // Signal that the connection has been made.
                connectDone.Set();
            }
            catch (Exception e)
            {
                Disconnect();
                //
                Console.WriteLine(e.ToString());
            }
        }

        private void ReceiverLoop(object obj)
        {
            while (client != null && client.Connected)
            {
                byte[] msg = ReceiveMessage();
                if (client != null && msg != null)
                {
                    if (Debug)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("TCI[" + client.RemoteEndPoint.ToString() + "] > " + ByteArrayToString(msg));
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
                else
                {
                    Thread.Sleep(300);
                }
            }
            //
            Disconnect();
        }

        private bool Receive(Socket client)
        {
            bool success = true;
            try
            {
                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None,
                    new AsyncCallback(ReceiveCallback), state);
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
                var state = (StateObject)ar.AsyncState;
                var client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    byte[] rd = new byte[bytesRead];
                    Array.Copy(state.buffer, 0, rd, 0, bytesRead);
                    if (MessageReceived != null) MessageReceived(rd);

                    // Signal that all bytes have been received.
                    receiveDone.Set();

                    // Continue receiving data.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None,
                        new AsyncCallback(ReceiveCallback), state);
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
                client.BeginSend(byteData, 0, byteData.Length, SocketFlags.None,
                    new AsyncCallback(SendCallback), client);
                //
                if (Debug)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("TCO[" + client.RemoteEndPoint.ToString() + "] < " + ByteArrayToString(byteData));
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
                client.BeginSend(byteData, 0, byteData.Length, SocketFlags.None,
                    new AsyncCallback(SendCallback), client);
                //
                if (Debug)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("TCO[" + client.RemoteEndPoint.ToString() + "] < " + ByteArrayToString(byteData));
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
                var client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                if (Debug)
                {
                    Console.WriteLine("Sent {0} bytes to server.", bytesSent);
                }

                // Signal that all bytes have been sent.
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private string ByteArrayToString(byte[] message)
        {
            string returnValue = String.Empty;
            foreach (byte b in message)
            {
                returnValue += b.ToString("X2") + " ";
            }
            return returnValue.Trim();
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
