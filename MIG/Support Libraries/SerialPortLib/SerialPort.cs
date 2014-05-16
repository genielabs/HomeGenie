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
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;

namespace SerialPortLib
{
    public class ConnectedStateChangedEventArgs
    {
        public bool Connected;

        public ConnectedStateChangedEventArgs(bool state)
        {
            Connected = state;
        }
    }

    public class SerialPortInput
    {
        public delegate void ConnectedStateChangedEvent(object sender, ConnectedStateChangedEventArgs statusargs);
        public event ConnectedStateChangedEvent ConnectedStateChanged;

        public delegate void MessageReceivedEvent(byte[] message);
        public event MessageReceivedEvent MessageReceived;

        private SerialPort serialPort;
        private string portName = "";
        private int baudRate = 115200;

        private bool gotReadWriteError = true;
        private bool keepConnectionAlive = false;
        private Thread connectionWatcher;

        private bool isConnected = false;
        private bool isRunning = true;

        private object writeLock = new object();

        private Thread receiverTask;
        private Thread senderTask;

        private Queue<byte[]> messageQueue = new Queue<byte[]>();

        private bool debug = false;


        public SerialPortInput()
        {
        }

        public SerialPortInput(string portName)
        {
            // reset connection if new portname is set
            try
            {
                serialPort.Close();
            }
            catch { }
            //
            gotReadWriteError = true;
        }
        public bool IsConnected
        {
            get { return isConnected && !gotReadWriteError; }
        }

        public bool Debug
        {
            get { return debug; }
            set { debug = value; }
        }

        public void SetPort(string portname, int baudrate)
        {
            if (portName != portname && serialPort != null)
            {
                Close();
            }
            portName = portname;
            baudRate = baudrate;
        }


        public bool Connect()
        {
            bool success = Open();
            //
            //
            // we use reader loop for Linux/Mono compatibility
            //
            if (connectionWatcher != null)
            {
                try
                {
                    keepConnectionAlive = false;
                    connectionWatcher.Abort();
                }
                catch { }
            }
            //
            keepConnectionAlive = true;
            //
            connectionWatcher = new Thread(new ThreadStart(delegate()
            {
                gotReadWriteError = !success;
                //
                while (keepConnectionAlive)
                {
                    if (gotReadWriteError)
                    {
                        try
                        {
                            Close();
                            //
                        }
                        catch (Exception unex)
                        {
                            //							Console.WriteLine(unex.Message + "\n" + unex.StackTrace);
                        }
                        Thread.Sleep(5000);
                        if (keepConnectionAlive)
                        {
                            try
                            {
                                gotReadWriteError = !Open();
                            }
                            catch (Exception unex)
                            {
                                //                                Console.WriteLine(unex.Message + "\n" + unex.StackTrace);
                            }
                        }
                    }
                    //
                    Thread.Sleep(1000);
                }
            }));
            connectionWatcher.Start();
            //
            return success;
        }

        public void Disconnect()
        {
            keepConnectionAlive = false;
            //
            try { senderTask.Abort(); }
            catch { }
            senderTask = null;
            try { receiverTask.Abort(); }
            catch { }
            receiverTask = null;
            //
            Close();
        }


        public void SendMessage(byte[] message)
        {
            messageQueue.Enqueue(message);
        }








        // this won't work under Linux/Mono
        /*
        private void HandleDataReceived (object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType == SerialData.Chars)
            {
                ReceiveMessage();
            }
        }
        */







        private bool Open()
        {
            bool success = false;
            try
            {
                bool tryOpen = (serialPort == null);
                if (Environment.OSVersion.Platform.ToString().StartsWith("Win") == false)
                {
                    tryOpen = (tryOpen && System.IO.File.Exists(portName));
                }
                if (tryOpen)
                {
                    serialPort = new SerialPort();
                    serialPort.PortName = portName;
                    serialPort.BaudRate = baudRate;
                    //
                    serialPort.ErrorReceived += HanldeErrorReceived;
                }
                if (serialPort.IsOpen == false)
                {
                    serialPort.Open();
                }
                success = true;
            }
            catch (Exception ex)
            {
            }
            //
            if (ConnectedStateChanged != null)
            {
                ConnectedStateChanged(this, new ConnectedStateChangedEventArgs(success));
            }
            isConnected = success;
            //
            if (success && receiverTask == null)
            {
                receiverTask = new Thread(ReceiverLoop);
                //_receiverthread.Priority = ThreadPriority.Highest;
                receiverTask.Start();
                //
                senderTask = new Thread(SenderLoop);
                //_senderthread.Priority = ThreadPriority.Highest;
                senderTask.Start();
            }
            return success;
        }


        private void Close()
        {
            if (serialPort != null)
            {
                try
                {
                    serialPort.Close();
                    serialPort.ErrorReceived -= HanldeErrorReceived;
                }
                catch
                {
                }
                serialPort = null;
                //
                if (isConnected && ConnectedStateChanged != null)
                {
                    ConnectedStateChanged(this, new ConnectedStateChangedEventArgs(false));
                }
                //
                isConnected = false;
            }
        }

        private void HanldeErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            DebugLog("SPI !", e.EventType.ToString());
            DebugLog("SPI !", e.ToString());
        }

        private void SenderLoop(object obj)
        {
            messageQueue.Clear();
            while (isRunning)
            {
                if (serialPort != null)
                {
                    try
                    {
                        while (messageQueue.Count > 0)
                        {
                            byte[] message = messageQueue.Dequeue();
                            try
                            {
                                if (Debug)
                                {
                                    DebugLog("SPO <", ByteArrayToString(message));
                                }
                                serialPort.Write(message, 0, message.Length);
                            }
                            catch (Exception e)
                            {
                                if (Debug)
                                {
                                    DebugLog("SPO !", e.Message);
                                    DebugLog("SPO !", e.StackTrace);
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                }
                else
                {
                    Thread.Sleep(950);
                }
                Thread.Sleep(50);
            }
        }


        private void ReceiverLoop()
        {
            while (isRunning)
            {
                int msglen = 0;
                //
                if (serialPort != null)
                {
                    try
                    {
                        msglen = serialPort.BytesToRead;
                        //
                        if (msglen > 0)
                        {
                            byte[] message = new byte[msglen];
                            //
                            int readbytes = 0;
                            while (serialPort.Read(message, readbytes, msglen - readbytes) <= 0)
                                ; // noop
                            if (Debug)
                            {
                                DebugLog("SPI >", ByteArrayToString(message));
                            }
                            if (MessageReceived != null)
                            {
                                //ThreadPool.QueueUserWorkItem(new WaitCallback(ReceiveMessage), message);
                                Thread deliver = new Thread(() =>
                                {
                                    ReceiveMessage(message);
                                });
                                deliver.Priority = ThreadPriority.AboveNormal;
                                deliver.Start();
                            }
                        }
                        else
                        {
                            Thread.Sleep(50);
                        }
                    }
                    catch (Exception e)
                    {
                        gotReadWriteError = true;
                        Thread.Sleep(1000);
                    }
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        public String ByteArrayToString(byte[] message)
        {
            String ret = String.Empty;
            foreach (byte b in message)
            {
                ret += b.ToString("X2") + " ";
            }
            return ret.Trim();
        }


        private void ReceiveMessage(object message)
        {
            if (MessageReceived != null)
            {
                MessageReceived((byte[])message);
            }
        }




        private void DebugLog(string prefix, string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            if (prefix.Contains(">"))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
            }
            else if (prefix.Contains("!"))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
            }
            Console.Write("[" + DateTime.Now.ToString("HH:mm:ss.ffffff") + "] ");
            Console.WriteLine(prefix + " " + message);
            Console.ForegroundColor = ConsoleColor.White;
        }

    }

}
