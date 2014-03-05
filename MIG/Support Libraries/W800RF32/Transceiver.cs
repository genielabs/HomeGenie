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


//
// references: X10 protocol documentation from http://www.linuxha.com/USB/cm15a.html
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace W800RF32
{
    public class RfDataReceivedAction
    {
        public byte[] RawData;
    }

    public class Transceiver
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public event Action<RfDataReceivedAction> RfDataReceived;

        private Queue<byte[]> sendQueue = new Queue<byte[]>();

        private Thread readerTask;
        private Thread writerTask;

        private object comLock = new object();
        private object accessLock = new object();

        private bool gotReadWriteError = true;
        private bool keepConnectionAlive = false;
        private Thread connectionWatcher;

        private string portName = "COM1";
        private RfDirect rawInterface;

        bool isWaitingChecksum = false;
        byte expectedChecksum = 0x00;

        private int zeroChecksumCount = 0;

        public Transceiver()
        {
            rawInterface = new RfDirect(portName);
        }



        public string PortName
        {
            get { return portName; }
            set
            {
                if (portName != value)
                {
                    Close();
                    //
                    rawInterface = new RfDirect(value);
                    gotReadWriteError = true;
                }
                portName = value;
            }
        }

        public bool Connect()
        {
            bool returnvalue = Open();
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
                gotReadWriteError = !returnvalue;
                //
                lock (accessLock)
                {
                    while (keepConnectionAlive)
                    {
                        if (gotReadWriteError)
                        {
                            try
                            {
                                sendQueue.Clear();
                                //
                                Close();
                                //
                                Thread.Sleep(5000);
                                if (keepConnectionAlive)
                                {
                                    try
                                    {
                                        gotReadWriteError = !Open();
                                    }
                                    catch { }
                                }
                            }
                            catch (Exception unex)
                            {
                                //Console.WriteLine(unex.Message + "\n" + unex.StackTrace);
                            }
                        }
                        //
                        Monitor.Wait(accessLock, 5000);
                    }
                }
            }));
            connectionWatcher.Start();
            //    
            return returnvalue;
        }

        public void Disconnect()
        {
            keepConnectionAlive = false;
            //
            Close();
        }

        public bool IsConnected
        {
            get { return !gotReadWriteError; }
        }







        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }


        private void ReaderThreadLoop()
        {
            lock (accessLock)
            {
                while (true)
                {
                    //
                    try
                    {
                        byte[] readdata = rawInterface.ReadData();
                        //                        Console.WriteLine("<<<<< " + Utility.ByteArrayToString(readdata));
                        //
                        if (readdata.Length > 0)
                        {
                            zeroChecksumCount = 0; // Linux/Pi disconnection detection
                            //
                            string sb1 = Convert.ToString(readdata[0], 2).PadLeft(8, '0');
                            string sb2 = Convert.ToString(readdata[1], 2).PadLeft(8, '0');
                            string sb3 = Convert.ToString(readdata[2], 2).PadLeft(8, '0');
                            string sb4 = Convert.ToString(readdata[3], 2).PadLeft(8, '0');
                            sb1 = Reverse(sb1);
                            sb2 = Reverse(sb2);
                            sb3 = Reverse(sb3);
                            sb4 = Reverse(sb4);

                            readdata[3] = Convert.ToByte(sb3, 2);
                            readdata[2] = Convert.ToByte(sb4, 2);
                            readdata[1] = Convert.ToByte(sb1, 2);
                            readdata[0] = Convert.ToByte(sb2, 2);

                            //Console.WriteLine("RF      ==> " + Transceiver.ByteArrayToString(readdata));
                            if (RfDataReceived != null)
                            {
                                RfDataReceived(new RfDataReceivedAction() { RawData = readdata });
                            }
                        }
                        else
                        {
                            // BEGIN: This is an hack for detecting disconnection status in Linux/Raspi
                            zeroChecksumCount++;
                            //
                            if (zeroChecksumCount > 10)
                            {
                                zeroChecksumCount = 0;
                                gotReadWriteError = true;
                                Close();
                            }
                            // END: Linux/Raspi hack
                            else if (isWaitingChecksum)
                            {
                                //Console.WriteLine("Expected [" + Traneceiver.ByteArrayToString(new byte[] { _expectedchecksum }) + "] Checksum ==> " + Transceiver.ByteArrayToString(readdata));
                                // checksum verification not handled, we just reply 0x00 (OK)
                                SendMessage(new byte[] { 0x00 });
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        if (!e.GetType().Equals(typeof(TimeoutException)))
                        {
                            // TODO: add error logging 
                            gotReadWriteError = true;
                        }
                    }
                    Monitor.Wait(accessLock, 10);
                }
            }
        }


        public void WaitComplete()
        {
            int hitcount = 0;
            while (true)
            {
                if (sendQueue.Count == 0 && ++hitcount == 2)
                {
                    break;
                }
                Thread.Sleep(50);
            }
        }


        public static String ByteArrayToString(byte[] message)
        {
            String ret = String.Empty;
            foreach (byte b in message)
            {
                ret += b.ToString("X2") + " ";
            }
            return ret.Trim();
        }


        private void ModulePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            // route event
            if (PropertyChanged != null)
            {
                PropertyChanged(sender, args);
            }
        }

        private void SendMessage(byte[] message)
        {
            rawInterface.WriteData(message);
        }

        private void WriterThreadLoop()
        {
            while (true)
            {
                try
                {
                    if (sendQueue.Count > 0)
                    {
                        byte[] msg = sendQueue.Dequeue();
                        //Console.WriteLine(">>>>> " + Transceiver.ByteArrayToString(msg));

                        Monitor.Enter(comLock);

                        if (isWaitingChecksum && msg.Length > 1)
                        {
                            Monitor.Wait(comLock, 3000);
                            isWaitingChecksum = false;
                        }

                        SendMessage(msg);

                        if (msg.Length > 1)
                        {
                            expectedChecksum = (byte)((msg[0] + msg[1]) & 0xff);
                            isWaitingChecksum = true;
                        }

                        Monitor.Exit(comLock);


                    }
                    else
                    {
                        Thread.Sleep(250);
                    }
                }
                catch
                {
                    gotReadWriteError = true;
                }
                finally
                {
                }
            }
        }

        private bool Open()
        {
            bool success = false;
            lock (accessLock)
            {
                success = (rawInterface != null && rawInterface.Open());
                if (success)
                {
                    readerTask = new Thread(new ThreadStart(ReaderThreadLoop));
                    writerTask = new Thread(new ThreadStart(WriterThreadLoop));
                    //
                    readerTask.Start();
                    writerTask.Start();
                }
            }
            return success;
        }

        private void Close()
        {
            lock (accessLock)
            {
                try
                {
                    if (rawInterface != null)
                        rawInterface.Close();
                }
                catch
                { }
                //
                try
                {
                    if (writerTask != null)
                        writerTask.Abort();
                }
                catch
                { }
                try
                {
                    if (readerTask != null)
                        readerTask.Abort();
                }
                catch
                { }
                writerTask = null;
                readerTask = null;
            }
        }


    }

}

