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

namespace TcpClientLib
{
    // State object for receiving data from remote device.
    public class StateObject {
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 256;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        //
        public List<byte> message = new List<byte>();
    }
    public class ConnectedStateChangedEventArgs
    {
        public bool Connected;

        public ConnectedStateChangedEventArgs(bool state)
        {
            Connected = state;
        }
    }
    public class TcpClient
    {
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

        private bool _debug = false;

        // The response from the remote device.
        private byte[] rawresponse = null;
        private Socket client = null;

        private Thread _receiverthread;

        public bool Connect(string remoteserver, int remoteport)
        {
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
                if (!IPAddress.TryParse(remoteserver, out ipAddress))
                {
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(remoteserver);
                    ipAddress = ipHostInfo.AddressList[0];
                }
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, remoteport);

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
            try { _receiverthread.Abort(); }
            catch { }
            _receiverthread = null;
            try
            {
                // Release the socket.
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch { }
            client = null;
            //
            if (ConnectedStateChanged != null) ConnectedStateChanged(this, new ConnectedStateChangedEventArgs(false));
        }


        public bool Debug
        {
            get { return _debug; }
            set { _debug = value; }
        }

        public byte[] ReceiveMessage()
        {
            rawresponse = null;

            // Receive the response from the remote device.
            if (Receive(client)) receiveDone.WaitOne(10000);

            return rawresponse;
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
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                if (Debug)
                {
                    Console.WriteLine("Socket connected to {0}",
                        client.RemoteEndPoint.ToString());
                }

                if (ConnectedStateChanged != null) ConnectedStateChanged(this, new ConnectedStateChangedEventArgs(true));

                _receiverthread = new Thread(_receiverloop);
                _receiverthread.Start();
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

        private void _receiverloop(object obj)
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
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
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
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    byte[] rd = new byte[bytesRead];
                    Array.Copy(state.buffer, 0, rd, 0, bytesRead);
                    state.message.AddRange(rd);
                    // Get the rest of the data.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.
                    if (state.message.Count > 1)
                    {
                        rawresponse = new byte[state.message.Count];
                        Array.Copy(state.message.ToArray(), 0, rawresponse, 0, state.message.Count);
                        //
                        if (MessageReceived != null) MessageReceived(rawresponse);
                    }
                    // Signal that all bytes have been received.
                    receiveDone.Set();
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
                client.BeginSend(byteData, 0, byteData.Length, 0,
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
                client.BeginSend(byteData, 0, byteData.Length, 0,
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
                Socket client = (Socket)ar.AsyncState;

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

        private String ByteArrayToString(byte[] message)
        {
            String ret = String.Empty;
            foreach (byte b in message)
            {
                ret += b.ToString("X2") + " ";
            }
            return ret.Trim();
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
