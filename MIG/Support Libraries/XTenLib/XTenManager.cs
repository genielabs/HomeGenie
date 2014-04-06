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

using XTenLib.Drivers;

namespace XTenLib
{
    public class RfDataReceivedAction
    {
        public byte[] RawData;
    }

    public class XTenManager
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public event Action<RfDataReceivedAction> RfDataReceived;

        private Queue<byte[]> sendQueue = new Queue<byte[]>();

        private Thread readerTask;
        private Thread writerTask;

        private object comLock = new object();
        private object accessLock = new object();

        private Dictionary<string, X10Module> moduleStatus = new Dictionary<string, X10Module>();
        private string currentUnitCode = "";
        private string monitoredHouseCode = "A";

        private bool gotReadWriteError = true;
        private bool keepConnectionAlive = false;
        private Thread connectionWatcher;

        private string portName = "USB";
        private XTenInterface x10interface;

        private int zeroChecksumCount = 0;
        private bool statusRequestOk = false;

        private bool isWaitingChecksum = false;
        private byte expectedChecksum = 0x00;

        public XTenManager()
        {
            HouseCode = "A";
            x10interface = new CM15();
        }


        public string HouseCode
        {
            get { return monitoredHouseCode; }
            set
            {
                monitoredHouseCode = value;
                for (int i = 0; i < moduleStatus.Keys.Count; i++)
                {
                    moduleStatus[moduleStatus.Keys.ElementAt(i)].PropertyChanged -= ModulePropertyChanged;
                }
                moduleStatus.Clear();
                //
                string[] hc = monitoredHouseCode.Split(',');
                for (int i = 0; i < hc.Length; i++)
                {
                    for (int x = 1; x <= 16; x++)
                    {
                        var module = new X10Module() { Code = hc[i] + x.ToString(), /*Status = "OFF",*/ Level = 0.0 };
                        //
                        module.PropertyChanged += ModulePropertyChanged;
                        //
                        moduleStatus.Add(hc[i] + x.ToString(), module);
                    }
                }
                //
                if (!gotReadWriteError && x10interface != null && x10interface.GetType().Equals(typeof(CM15)))
                {
                    SetCm15Codes();
                }
            }
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
                    if (value.ToUpper() == "USB")
                    {
                        x10interface = new CM15();
                    }
                    else
                    {
                        x10interface = new CM11(value);
                    }
                    //
                    gotReadWriteError = true;
                }
                portName = value;
            }
        }

        public bool Connect()
        {
            bool returnValue = Open();
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
                gotReadWriteError = !returnValue;
                //
                lock (accessLock)
                {
                    while (keepConnectionAlive)
                    {
                        if (gotReadWriteError)
                        {
                            statusRequestOk = false;
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
            return returnValue;
        }

        public void Disconnect()
        {
            keepConnectionAlive = false;
            //
            Close();
        }

        public bool IsConnected
        {
            get { return statusRequestOk || (!gotReadWriteError && x10interface.GetType().Equals(typeof(CM15))); }
        }






        public void StatusRequest(X10HouseCodes housecode, X10UnitCodes unitcode)
        {
            string hcunit = String.Format("{0:X}{1:X}", (int)housecode, (int)unitcode);
            string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.Status_Request);
            sendQueue.Enqueue(new byte[] { (int)X10CommandType.Address, byte.Parse(hcunit, System.Globalization.NumberStyles.HexNumber) });
            sendQueue.Enqueue(new byte[] { (int)X10CommandType.Function, byte.Parse(hcfuntion, System.Globalization.NumberStyles.HexNumber) });
        }

        public void Dim(X10HouseCodes housecode, X10UnitCodes unitcode, int percentage)
        {
            string huc = Utility.HouseUnitCodeFromEnum(housecode, unitcode);
            string hcunit = String.Format("{0:X}{1:X}", (int)housecode, (int)unitcode);
            string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.Dim);
            string dimbright = String.Format("{0:x1}", (int)(((double)percentage / 100D) * 210));
            //
            moduleStatus[huc].Level -= ((double)percentage / 100.0);
            if (moduleStatus[huc].Level < 0.0) moduleStatus[huc].Level = 0.0;
            //
            sendQueue.Enqueue(new byte[] { (int)X10CommandType.Address, byte.Parse(hcunit, System.Globalization.NumberStyles.HexNumber) });
            if (x10interface.GetType().Equals(typeof(CM11)))
            {
                int dimvalue = (int)(((double)percentage / 100D) * 16) << 3;
                dimbright = String.Format("{0:x1}", dimvalue);
                sendQueue.Enqueue(new byte[] { (byte)((int)X10CommandType.Function | dimvalue), byte.Parse(hcfuntion, System.Globalization.NumberStyles.HexNumber) });
            }
            else
            {
                sendQueue.Enqueue(new byte[] { (int)X10CommandType.Function, byte.Parse(hcfuntion, System.Globalization.NumberStyles.HexNumber), byte.Parse(dimbright, System.Globalization.NumberStyles.HexNumber) });
            }
        }

        public void Bright(X10HouseCodes housecode, X10UnitCodes unitcode, int percentage)
        {
            string huc = Utility.HouseUnitCodeFromEnum(housecode, unitcode);
            string hcunit = String.Format("{0:X}{1:X}", (int)housecode, (int)unitcode);
            string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.Bright);
            string dimbright = String.Format("{0:x1}", (int)(((double)percentage / 100D) * 210));
            //
            moduleStatus[huc].Level += ((double)percentage / 100.0);
            if (moduleStatus[huc].Level > 1.0) moduleStatus[huc].Level = 1.0;
            //
            sendQueue.Enqueue(new byte[] { (int)X10CommandType.Address, byte.Parse(hcunit, System.Globalization.NumberStyles.HexNumber) });
            if (x10interface.GetType().Equals(typeof(CM11)))
            {
                int dimvalue = (int)(((double)percentage / 100D) * 16) << 3;
                dimbright = String.Format("{0:x1}", dimvalue);
                sendQueue.Enqueue(new byte[] { (byte)((int)X10CommandType.Function | dimvalue), byte.Parse(hcfuntion, System.Globalization.NumberStyles.HexNumber) });
            }
            else
            {
                sendQueue.Enqueue(new byte[] { (int)X10CommandType.Function, byte.Parse(hcfuntion, System.Globalization.NumberStyles.HexNumber), byte.Parse(dimbright, System.Globalization.NumberStyles.HexNumber) });
            }
        }

        public void LightOn(X10HouseCodes housecode, X10UnitCodes unitcode)
        {
            string hcunit = String.Format("{0:X}{1:X}", (int)housecode, (int)unitcode);
            string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.On);
            sendQueue.Enqueue(new byte[] { (int)X10CommandType.Address, byte.Parse(hcunit, System.Globalization.NumberStyles.HexNumber) });
            sendQueue.Enqueue(new byte[] { (int)X10CommandType.Function, byte.Parse(hcfuntion, System.Globalization.NumberStyles.HexNumber) });
            //
            string huc = Utility.HouseUnitCodeFromEnum(housecode, unitcode);
            if (moduleStatus[huc].Level == 0.0)
            {
                moduleStatus[huc].Level = 1.0;
            }
        }

        public void LightOff(X10HouseCodes housecode, X10UnitCodes unitcode)
        {
            string hcunit = String.Format("{0:X}{1:X}", (int)housecode, (int)unitcode);
            string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.Off);
            sendQueue.Enqueue(new byte[] { (int)X10CommandType.Address, byte.Parse(hcunit, System.Globalization.NumberStyles.HexNumber) });
            sendQueue.Enqueue(new byte[] { (int)X10CommandType.Function, byte.Parse(hcfuntion, System.Globalization.NumberStyles.HexNumber) });
            //
            string huc = Utility.HouseUnitCodeFromEnum(housecode, unitcode);
            moduleStatus[huc].Level = 0.0;
        }

        public void AllLightsOn(X10HouseCodes housecode)
        {
            string hcunit = String.Format("{0:X}{1:X}", (int)housecode, 0);
            string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.All_Lights_On);
            sendQueue.Enqueue(new byte[] { (int)X10CommandType.Address, byte.Parse(hcunit, System.Globalization.NumberStyles.HexNumber) });
            sendQueue.Enqueue(new byte[] { (int)X10CommandType.Function, byte.Parse(hcfuntion, System.Globalization.NumberStyles.HexNumber) });
            //
            // TODO: pick only lights module
            AllLightsOn(housecode.ToString());
        }

        public void AllUnitsOff(X10HouseCodes housecode)
        {
            string hcunit = String.Format("{0:X}{1:X}", (int)housecode, 0);
            string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.All_Units_Off);
            sendQueue.Enqueue(new byte[] { (int)X10CommandType.Address, byte.Parse(hcunit, System.Globalization.NumberStyles.HexNumber) });
            sendQueue.Enqueue(new byte[] { (int)X10CommandType.Function, byte.Parse(hcfuntion, System.Globalization.NumberStyles.HexNumber) });
            //
            // TODO: pick only lights module
            AllUnitsOff(housecode.ToString());
        }






        public Dictionary<string, X10Module> ModulesStatus
        {
            get { return moduleStatus; }
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
                        byte[] readData = x10interface.ReadData();



                        if (readData.Length >= 13)
                        {
                            /*
                            Console.WriteLine("\n\n\n\n{0}:{1}:{2}", (readdata[4] * 2).ToString("D2"),
                                                    (readdata[3]).ToString("D2"),
                                                    (readdata[2]).ToString("D2"));

                            */
                            /*
                            // A1 Status request
                            Thread t = new Thread(new ThreadStart(delegate()
                            {
                                Thread.Sleep(10000);
                                //_sendqueue.Enqueue(new byte[] { 0x8B });
                                _sendqueue.Enqueue(new byte[] { 0x07, 0x67, 0x06, 0x03, 0x3b });
                                _sendqueue.Enqueue(new byte[] { 0x07, 0x67, 0x06, 0x00, 0x37 });
                            }));
                            t.Start();
                            */

                            if (!statusRequestOk)
                            {
                                UpdateInterfaceTime(false);
                                statusRequestOk = true;
                            }

                        }



                        if (readData.Length > 0)
                        {
                            //Console.WriteLine("<<<<< IN " + Utility.ByteArrayToString(readdata));
                            //
                            if (readData[0] == (int)X10CommandType.PLC_Ready)
                            {
                                isWaitingChecksum = false;
                                Monitor.Enter(comLock);
                                Monitor.Pulse(comLock);
                                Monitor.Exit(comLock);
                            }
                            else if (readData[0] == (int)X10CommandType.Macro)
                            {
                                //Console.WriteLine("Macro   ==> " + Utility.ByteArrayToString(readdata));
                            }
                            else if (readData[0] == (int)X10CommandType.RF)
                            {
                                //Console.WriteLine("RF      ==> " + Utility.ByteArrayToString(readdata));
                                if (RfDataReceived != null)
                                {
                                    RfDataReceived(new RfDataReceivedAction() { RawData = readData });
                                }
                            }
                            else if ((readData[0] == (int)X10CommandType.PLC_Poll) && readData.Length == 1)
                            {
                                statusRequestOk = true;
                                sendQueue.Enqueue(new byte[] { 0xC3 }); // reply to poll
                            }
                            else if ((readData[0] == (int)X10CommandType.PLC_Poll))
                            {
                                //Console.WriteLine("PLC     ==> " + Utility.ByteArrayToString(readdata));
                                if (readData.Length > 2)
                                {
                                    if (readData[2] == 0x00 && readData.Length > 3)
                                    {
                                        string housecode = ((X10HouseCodes)Convert.ToInt16(readData[3].ToString("X2").Substring(0, 1), 16)).ToString();
                                        string unitcode = ((X10UnitCodes)Convert.ToInt16(readData[3].ToString("X2").Substring(1, 1), 16)).ToString();
                                        if (unitcode.IndexOf("_") > 0) unitcode = unitcode.Substring(unitcode.IndexOf("_") + 1);
                                        //
                                        //Console.WriteLine("            0x00 = Address");
                                        //Console.WriteLine("      House code = " + housecode);
                                        //Console.WriteLine("       Unit code = " + unitcode);
                                        //
                                        currentUnitCode = housecode + unitcode;
                                    }
                                    else if (readData[2] == 0x01 && readData.Length > 3)
                                    {
                                        string command = ((X10Command)Convert.ToInt16(readData[3].ToString("X2").Substring(1, 1), 16)).ToString().ToUpper();
                                        string housecode = ((X10HouseCodes)Convert.ToInt16(readData[3].ToString("X2").Substring(0, 1), 16)).ToString();
                                        //Console.WriteLine("            0x01 = Function");
                                        //Console.WriteLine("      House code = " + housecode);
                                        //Console.WriteLine("         Command = " + command);
                                        //
                                        if (currentUnitCode != "")
                                        {
                                            if (!moduleStatus.Keys.Contains(currentUnitCode))
                                            {
                                                var module = new X10Module() { Code = currentUnitCode };
                                                //
                                                module.PropertyChanged += ModulePropertyChanged;
                                                //
                                                moduleStatus.Add(currentUnitCode, module);
                                            }
                                            var mod = moduleStatus[currentUnitCode];
                                            switch (command)
                                            {
                                                case "ON":
                                                    //mod.Status = "ON";
                                                    mod.Level = 1.0;
                                                    break;
                                                case "OFF":
                                                    //mod.Status = "OFF";
                                                    mod.Level = 0.0;
                                                    break;
                                                case "BRIGHT":
                                                    mod.Level += (double.Parse((readData[4] >> 3).ToString()) / 16D);
                                                    if (mod.Level > 1) mod.Level = 1;
                                                    break;
                                                case "DIM":
                                                    mod.Level -= (double.Parse((readData[4] >> 3).ToString()) / 16D);
                                                    if (mod.Level < 0) mod.Level = 0;
                                                    break;
                                                case "ALL_UNITS_OFF":
                                                    AllUnitsOff(housecode);
                                                    break;
                                                case "ALL_LIGHTS_ON":
                                                    AllLightsOn(housecode);
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                            else if ((readData[0] == (int)X10CommandType.PLC_TimeRequest)) // IS THIS A TIME REQUEST?
                            {
                                UpdateInterfaceTime(false);
                            }
                            else
                            {
                                // BEGIN: This is an hack for detecting disconnection status in Linux/Raspi
                                if (readData[0] == 0x00)
                                {
                                    zeroChecksumCount++;
                                }
                                else
                                {
                                    zeroChecksumCount = 0;
                                }
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
                                    //Console.WriteLine("Expected [" + Utility.ByteArrayToString(new byte[] { _expectedchecksum }) + "] Checksum ==> " + Utility.ByteArrayToString(readdata));
                                    //TODO: checksum verification not handled, we just reply 0x00 (OK)
                                    SendMessage(new byte[] { 0x00 });
                                }
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
                Thread.Sleep(100);
            }
        }



        private void UpdateInterfaceTime(bool batteryclear)
        {
            /*
            The PC must then respond with the following transmission

            Bit range	Description
            55 to 48	timer download header (0x9b)
            47 to 40	Current time (seconds)
            39 to 32	Current time (minutes ranging from 0 to 119)
            31 to 23	Current time (hours/2, ranging from 0 to 11)
            23 to 16	Current year day (bits 0 to 7)
            15	Current year day (bit 8)
            14 to 8		Day mask (SMTWTFS)
            7 to 4		Monitored house code
            3		Reserved
            2		Battery timer clear flag
            1		Monitored status clear flag
            0		Timer purge flag
            */
            var date = DateTime.Now;
            int minute = date.Minute;
            int hour = date.Hour / 2;
            if (Math.IEEERemainder(date.Hour, 2) > 0)
            { // Add remaining minutes 
                minute += 60;
            }
            int wday = Convert.ToInt16(Math.Pow(2, (int)date.DayOfWeek));
            int yearDay = date.DayOfYear - 1;
            if (yearDay > 255)
            {
                yearDay = yearDay - 256;
                // Set current yearDay flag in wday's 7:th bit, since yearDay overflowed...
                wday = wday + Convert.ToInt16(Math.Pow(2, 7));
            }
            // Build message
            byte[] message = new byte[8];
            message[0] = 0x9b;   // cm11 x10 time download header
            message[1] = Convert.ToByte(date.Second);
            message[2] = Convert.ToByte(minute);
            message[3] = Convert.ToByte(hour);
            message[4] = Convert.ToByte(yearDay);
            message[5] = Convert.ToByte(wday);
            message[6] = Convert.ToByte((batteryclear ? 0x07 : 0x03) + Utility.HouseCodeFromString(this.HouseCode)); // Send timer purgeflag + Monitored status clear flag, monitored house code.
            //
            if (x10interface.GetType().Equals(typeof(CM15)))
            {
                // this seems to be needed only with CM15
                message[7] = 0x02;
            }
            //
            SendMessage(message);
        }


        private void AllUnitsOff(string housecode)
        {
            // TODO: pick only lights module _unitsOff(string housecode)
            foreach (KeyValuePair<string, X10Module> modkv in moduleStatus)
            {
                if (modkv.Value.Code.StartsWith(housecode))
                {
                    modkv.Value.Level = 0.0;
                }
            }
        }

        private void AllLightsOn(string housecode)
        {
            // TODO: pick only lights module _unitsOff(string housecode)
            foreach (KeyValuePair<string, X10Module> modkv in moduleStatus)
            {
                if (modkv.Value.Code.StartsWith(housecode))
                {
                    modkv.Value.Level = 1.0;
                }
            }
        }

        private void SetCm15Codes()
        {
            // BuildTransceivedCodesMessage return byte message for setting transceive codes from given comma separated _monitoredhousecode
            //byte[] trcommand = CM15.BuildTransceivedCodesMessage(_monitoredhousecode);
            ////_sendqueue.Enqueue(new byte[] { 0x8B });
            ////Thread.Sleep(200);
            ////_sendqueue.Enqueue(new byte[] { 0xDB, 0x1F, 0xF0 });
            ////Thread.Sleep(600);
            //_sendqueue.Enqueue(trcommand);
            //
            //
            // these two lines are sent to CM15 before setting transceived codes
            // but who knows what that does mean?!?!? =)
            SendMessage(new byte[] { 0xfb, 0x20, 0x00, 0x02 });
            SendMessage(new byte[] { 0xfb, 0x20, 0x00, 0x02 });
            Thread.Sleep(30);
            // set transceived codes to AUTO
            //byte[] trcommand = new byte[] { 0xbb, 0x40, 0x00, 0x05, 0x00, 0x14, 0x20, 0x28 };
            // BuildTransceivedCodesMessage return byte message for setting transceive codes from given comma separated _monitoredhousecode
            byte[] trcommand = CM15.BuildTransceivedCodesMessage(monitoredHouseCode);
            SendMessage(trcommand);
            SendMessage(trcommand);
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
            x10interface.WriteData(message);
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
                        //Console.WriteLine(">>>>>OUT " + Utility.ByteArrayToString(msg));

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
                        Thread.Sleep(400); // some extra sleep after sending a message                      

                    }
                }
                catch
                {
                    gotReadWriteError = true;
                }
                finally
                {
                }
                Thread.Sleep(100);
            }
        }

        private bool Open()
        {
            bool success = false;
            lock (accessLock)
            {
                success = (x10interface != null && x10interface.Open());
                if (success)
                {
                    //
                    // set transceived house codes for CM15 X10 RF-->PLC
                    if (x10interface.GetType().Equals(typeof(CM15)))
                    {
                        SetCm15Codes();
                    }
                    //
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
                    writerTask.Abort();
                }
                catch
                {
                }
                try
                {
                    readerTask.Abort();
                }
                catch
                {
                }
                writerTask = null;
                readerTask = null;
                //
                try
                {
                    x10interface.Close();
                }
                catch
                {
                }
            }
        }


    }

}

