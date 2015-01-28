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
//                                             http://www.smarthus.info/support/download/x10_RF_formats.pdf
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

    /// <summary>
    /// X10 Interface Driver for CM11/CM15
    /// </summary>
    public class XTenManager
    {
        #region Public Events

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<RfDataReceivedAction> RfDataReceived;

        #endregion

        #region Private Fields

        private Thread readerTask;
        private Thread connectionWatcher;

        private string monitoredHouseCode = "A";
        private Dictionary<string, X10Module> moduleStatus = new Dictionary<string, X10Module>();
        private List<X10Module> addressedModules = new List<X10Module>();
        private bool newAddressData = true;

        private string portName = "USB";
        private XTenInterface x10interface;

        private bool isInterfaceReady = false;
        private X10CommState communicationState = X10CommState.Ready;
        private byte expectedChecksum = 0x00;

        private double commandTimeoutSeconds = 5.0;
        private int commandResendMax = 1;
        private byte[] commandLastMessage = new byte[0];
        private int commandResendAttempts = 0;
        private object waitAckMonitor = new object();
        private DateTime waitAckTimestamp = DateTime.Now;
        private DateTime lastReceivedTs = DateTime.Now;

        private DateTime lastRfReceivedTs = DateTime.Now;
        private string lastRfMessage = "";

        private bool gotReadWriteError = true;
        private bool keepConnectionAlive = false;

        // this is used on Linux for detecting when the link gets disconnected
        private int zeroChecksumCount = 0;

        #endregion

        #region Instance Management

        public XTenManager()
        {
            HouseCode = "A";
            x10interface = new CM15();
        }

        ~XTenManager()
        {
            Close();
        }

        #endregion

        #region Public Members

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
            Disconnect();
            //
            bool returnValue = Open();
            //
            keepConnectionAlive = true;
            connectionWatcher = new Thread(new ThreadStart(delegate()
            {
                gotReadWriteError = !returnValue;
                //
                while (keepConnectionAlive)
                {
                    if (gotReadWriteError)
                    {
                        isInterfaceReady = false;
                        try
                        {
                            UnselectModules();
                            //
                            Close();
                            //
                            // wait 5 secs before reconnecting
                            Thread.Sleep(5000);
                            if (keepConnectionAlive)
                            {
                                try
                                {
                                    gotReadWriteError = !Open();
                                }
                                catch
                                {
                                }
                            }
                        }
                        catch
                        {
                            //Console.WriteLine(unex.Message + "\n" + unex.StackTrace);
                        }
                    }
                    Thread.Sleep(2000);
                }
            }));
            connectionWatcher.Start();
            //    
            return returnValue;
        }

        public void Disconnect()
        {
            keepConnectionAlive = false;
            try
            {
                connectionWatcher.Abort();
            }
            catch
            {
            }
            connectionWatcher = null;
            //
            Close();
        }

        public bool IsConnected
        {
            get { return isInterfaceReady || (!gotReadWriteError && x10interface.GetType().Equals(typeof(CM15))); }
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
                    InitializeCm15();
                }
            }
        }

        public Dictionary<string, X10Module> ModulesStatus
        {
            get { return moduleStatus; }
        }

        #region X10 Commands Implementation

        public void Dim(X10HouseCode housecode, X10UnitCode unitcode, int percentage)
        {
            lock (this)
            {
                string huc = Utility.HouseUnitCodeFromEnum(housecode, unitcode);
                string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.Dim);
                //
                SendModuleAddress(housecode, unitcode);
                if (x10interface.GetType().Equals(typeof(CM15)))
                {
                    double normalized = ((double)percentage / 100D);
                    SendMessage(new byte[] {
                        (int)X10CommandType.Function,
                        byte.Parse(
                            hcfuntion,
                            System.Globalization.NumberStyles.HexNumber
                        ),
                        (byte)(normalized * 210)
                    });
                    double newLevel = moduleStatus[huc].Level - normalized;
                    if (newLevel < 0) newLevel = 0;
                    moduleStatus[huc].Level = newLevel;
                }
                else
                {
                    byte dimvalue = Utility.GetDimValue(percentage);
                    SendMessage(new byte[] {
                        (byte)((int)X10CommandType.Function | dimvalue | 0x04),
                        byte.Parse(
                            hcfuntion,
                            System.Globalization.NumberStyles.HexNumber
                        )
                    });
                    double newLevel = moduleStatus[huc].Level - Utility.GetPercentageValue(dimvalue);
                    if (newLevel < 0) newLevel = 0;
                    moduleStatus[huc].Level = newLevel;
                }
            }
        }

        public void Bright(X10HouseCode housecode, X10UnitCode unitcode, int percentage)
        {
            lock (this)
            {
                string huc = Utility.HouseUnitCodeFromEnum(housecode, unitcode);
                //string hcunit = String.Format("{0:X}{1:X}", (int)housecode, (int)unitcode);
                string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.Bright);
                //
                SendModuleAddress(housecode, unitcode);
                if (x10interface.GetType().Equals(typeof(CM15)))
                {
                    double normalized = ((double)percentage / 100D);
                    SendMessage(new byte[] {
                        (int)X10CommandType.Function,
                        byte.Parse(
                            hcfuntion,
                            System.Globalization.NumberStyles.HexNumber
                        ),
                        (byte)(normalized * 210)
                    });
                    double newLevel = moduleStatus[huc].Level + normalized;
                    if (newLevel > 1) newLevel = 1;
                    moduleStatus[huc].Level = newLevel;
                }
                else
                {
                    byte dimvalue = Utility.GetDimValue(percentage);
                    SendMessage(new byte[] {
                        (byte)((int)X10CommandType.Function | dimvalue | 0x04),
                        byte.Parse(
                            hcfuntion,
                            System.Globalization.NumberStyles.HexNumber
                        )
                    });
                    double newLevel = moduleStatus[huc].Level + Utility.GetPercentageValue(dimvalue);
                    if (newLevel > 1) newLevel = 1;
                    moduleStatus[huc].Level = newLevel;
                }
            }
        }

        public void UnitOn(X10HouseCode housecode, X10UnitCode unitcode)
        {
            lock (this)
            {
                //string hcunit = String.Format("{0:X}{1:X}", (int)housecode, (int)unitcode);
                string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.On);
                SendModuleAddress(housecode, unitcode);
                SendMessage(new byte[] {
                    (int)X10CommandType.Function,
                    byte.Parse(
                        hcfuntion,
                        System.Globalization.NumberStyles.HexNumber
                    )
                });
                //
                string huc = Utility.HouseUnitCodeFromEnum(housecode, unitcode);
                if (moduleStatus[huc].Level == 0.0)
                {
                    moduleStatus[huc].Level = 1.0;
                }
            }
        }

        public void UnitOff(X10HouseCode housecode, X10UnitCode unitcode)
        {
            lock (this)
            {
                //string hcunit = String.Format("{0:X}{1:X}", (int)housecode, (int)unitcode);
                string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.Off);
                SendModuleAddress(housecode, unitcode);
                SendMessage(new byte[] {
                    (int)X10CommandType.Function,
                    byte.Parse(
                        hcfuntion,
                        System.Globalization.NumberStyles.HexNumber
                    )
                });
                //
                string huc = Utility.HouseUnitCodeFromEnum(housecode, unitcode);
                moduleStatus[huc].Level = 0.0;
            }
        }

        public void AllLightsOn(X10HouseCode housecode)
        {
            lock (this)
            {
                string hcunit = String.Format("{0:X}{1:X}", (int)housecode, 0);
                string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.All_Lights_On);
                SendMessage(new byte[] {
                    (int)X10CommandType.Address,
                    byte.Parse(
                        hcunit,
                        System.Globalization.NumberStyles.HexNumber
                    )
                });
                SendMessage(new byte[] {
                    (int)X10CommandType.Function,
                    byte.Parse(
                        hcfuntion,
                        System.Globalization.NumberStyles.HexNumber
                    )
                });
                //
                // TODO: pick only lights module
                CommandEvent_AllLightsOn(housecode.ToString());
            }
        }

        public void AllUnitsOff(X10HouseCode housecode)
        {
            lock (this)
            {
                string hcunit = String.Format("{0:X}{1:X}", (int)housecode, 0);
                string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.All_Units_Off);
                SendMessage(new byte[] {
                    (int)X10CommandType.Address,
                    byte.Parse(
                        hcunit,
                        System.Globalization.NumberStyles.HexNumber
                    )
                });
                SendMessage(new byte[] {
                    (int)X10CommandType.Function,
                    byte.Parse(
                        hcfuntion,
                        System.Globalization.NumberStyles.HexNumber
                    )
                });
                //
                // TODO: pick only lights module
                CommandEvent_AllUnitsOff(housecode.ToString());
            }
        }
        
        public void StatusRequest(X10HouseCode housecode, X10UnitCode unitcode)
        {
            lock (this)
            {
                //string hcunit = String.Format("{0:X}{1:X}", (int)housecode, (int)unitcode);
                string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.Status_Request);
                SendModuleAddress(housecode, unitcode);
                SendMessage(new byte[] {
                    (int)X10CommandType.Function,
                    byte.Parse(
                        hcfuntion,
                        System.Globalization.NumberStyles.HexNumber
                        )
                });
            }
        }

        #endregion

        #endregion

        #region Private Members

        #region X10 Interface Commands
        
        private void SendModuleAddress(X10HouseCode housecode, X10UnitCode unitcode)
        {
            // TODO: do more tests about this optimization
            //if (!addressedModules.Contains(mod) || addressedModules.Count > 1) // optimization disabled, uncomment to enable
            {
                UnselectModules();
                SelectModule(Utility.HouseUnitCodeFromEnum(housecode, unitcode));
                string hcUnit = String.Format("{0:X}{1:X}", (int)housecode, (int)unitcode);
                SendMessage(new byte[] {
                    (int)X10CommandType.Address,
                    byte.Parse(
                        hcUnit,
                        System.Globalization.NumberStyles.HexNumber
                        )
                });
                newAddressData = true;
            }
        }

        private void UpdateInterfaceTime(bool batteryClear)
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
            message[6] = Convert.ToByte((batteryClear ? 0x07 : 0x03) + Utility.HouseCodeFromString(this.HouseCode)); // Send timer purgeflag + Monitored status clear flag, monitored house code.
            //
            if (x10interface.GetType().Equals(typeof(CM15)))
            {
                // this seems to be needed only with CM15
                message[7] = 0x02;
            }
            //
            UnselectModules();
            Utility.DebugLog("X10 <", Utility.ByteArrayToString(message));
            //
            SendMessage(message);
        }

        private void InitializeCm15()
        {
            lock (this)
            {
                // BuildTransceivedCodesMessage return byte message for setting transceive codes from given comma separated _monitoredhousecode
                UpdateInterfaceTime(false);
                byte[] trcommand = CM15.BuildTransceivedCodesMessage(monitoredHouseCode);
                SendMessage(trcommand);
                SendMessage(new byte[] { 0x8B });
            }
        }

        #endregion

        #region X10 Command Input Events

        private void CommandEvent_On()
        {
            for (int m = 0; m < addressedModules.Count; m++)
            {
                X10Module mod = addressedModules[m];
                mod.Level = 1.0;
            }
        }

        private void CommandEvent_Off()
        {
            for (int m = 0; m < addressedModules.Count; m++)
            {
                X10Module mod = addressedModules[m];
                mod.Level = 0.0;
            }
        }

        private void CommandEvent_Bright(byte parameter)
        {
            for (int m = 0; m < addressedModules.Count; m++)
            {
                X10Module mod = addressedModules[m];
                var brightLevel = Math.Round(mod.Level + (((double)parameter) / 210D), 2);
                if (brightLevel > 1) brightLevel = 1;
                mod.Level = brightLevel;
            }
        }

        private void CommandEvent_Dim(byte parameter)
        {
            for (int m = 0; m < addressedModules.Count; m++)
            {
                X10Module mod = addressedModules[m];
                var dimLevel = Math.Round(mod.Level - (((double)parameter) / 210D), 2);
                if (dimLevel < 0) dimLevel = 0;
                mod.Level = dimLevel;
            }
        }

        private void CommandEvent_AllUnitsOff(string housecode)
        {
            UnselectModules();
            // TODO: select only light modules 
            foreach (KeyValuePair<string, X10Module> modkv in moduleStatus)
            {
                if (modkv.Value.Code.StartsWith(housecode))
                {
                    modkv.Value.Level = 0.0;
                }
            }
        }

        private void CommandEvent_AllLightsOn(string housecode)
        {
            UnselectModules();
            // TODO: pick only light modules 
            foreach (KeyValuePair<string, X10Module> modkv in moduleStatus)
            {
                if (modkv.Value.Code.StartsWith(housecode))
                {
                    modkv.Value.Level = 1.0;
                }
            }
        }

        #endregion

        #region Modules status and events

        private X10Module SelectModule(string address)
        {
            if (!moduleStatus.Keys.Contains(address))
            {
                var newModule = new X10Module() { Code = address };
                newModule.PropertyChanged += ModulePropertyChanged;
                moduleStatus.Add(address, newModule);
            }
            var module = moduleStatus[address];
            if (!addressedModules.Contains(module))
            {
                addressedModules.Add(module);
            }
            return module;
        }

        private void UnselectModules()
        {
            addressedModules.Clear();
        }

        private void ModulePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            // route event
            if (PropertyChanged != null)
            {
                try { PropertyChanged(sender, args); } catch { 
                    // TODO: handle/report exception
                }
            }
        }

        #endregion
                
        #region X10 Interface I/O operations

        private bool Open()
        {
            bool success = (x10interface != null && x10interface.Open());
            if (success)
            {
                //
                // set transceived house codes for CM15 X10 RF-->PLC
                if (x10interface.GetType().Equals(typeof(CM15)))
                {
                    InitializeCm15();
                }
                //
                readerTask = new Thread(new ThreadStart(ReaderThreadLoop));
                readerTask.Start();
            }
            return success;
        }

        private void Close()
        {
            try
            {
                readerTask.Abort();
            }
            catch
            {
            }
            readerTask = null;
            //
            try
            {
                x10interface.Close();
            }
            catch
            {
            }
            isInterfaceReady = false;
        }

        private void SendMessage(byte[] message)
        {
            try
            {
                // Wait for message delivery acknowledge
                if (message.Length > 1 && IsConnected)
                {
                    lock(waitAckMonitor)
                    {
                        while((DateTime.Now - lastReceivedTs).TotalMilliseconds < 500)
                        {
                            Thread.Sleep(1);
                        }

                        Utility.DebugLog("X10 <", Utility.ByteArrayToString(message));
                        x10interface.WriteData(message);

                        commandLastMessage = message;
                        waitAckTimestamp = DateTime.Now;

                        if (x10interface.GetType().Equals(typeof(CM11)))
                        {
                            expectedChecksum = (byte)((message[0] + message[1]) & 0xff);
                            communicationState = X10CommState.WaitingChecksum;
                        }
                        else
                        {
                            communicationState = X10CommState.WaitingAck;
                        }

                        while (commandResendAttempts < commandResendMax && communicationState != X10CommState.Ready)
                        {
                            var elapsedFromWaitAck = DateTime.Now - waitAckTimestamp;
                            while (elapsedFromWaitAck.TotalSeconds < commandTimeoutSeconds && communicationState != X10CommState.Ready)
                            {
                                Thread.Sleep(1);
                                elapsedFromWaitAck = DateTime.Now - waitAckTimestamp;
                            }
                            if (elapsedFromWaitAck.TotalSeconds >= commandTimeoutSeconds && communicationState != X10CommState.Ready)
                            {
                                // Resend last message
                                commandResendAttempts++;
                                Utility.DebugLog(
                                    "X10 >",
                                    "PREVIOUS COMMAND TIMED OUT, RESENDING(" + commandResendAttempts + ")"
                                    );
                                x10interface.WriteData(commandLastMessage);
                                waitAckTimestamp = DateTime.Now;
                            }
                        }
                        commandResendAttempts = 0;
                        commandLastMessage = new byte[0];

                    }
                }
                else
                {

                    Utility.DebugLog("X10 <", Utility.ByteArrayToString(message));
                    x10interface.WriteData(message);

                }
            }
            catch (Exception ex)
            {
                Utility.DebugLog("X10 !", ex.Message);
                Utility.DebugLog("X10 !", ex.StackTrace);

                gotReadWriteError = true;
            }
        }

        private void ReaderThreadLoop()
        {
            while (true)
            {
                try
                {
                    byte[] readData = x10interface.ReadData();
                    if (readData.Length > 0)
                    {
                        Utility.DebugLog(
                            "X10 >", 
                            Utility.ByteArrayToString(readData)
                        );
                        //
                        var elapsedFromWaitAck = DateTime.Now - waitAckTimestamp;
                        if (elapsedFromWaitAck.TotalSeconds >= commandTimeoutSeconds && communicationState != X10CommState.Ready) 
                        {
                            Utility.DebugLog(
                                "X10 >",
                                "COMMAND TIMEOUT"
                            );
                            communicationState = X10CommState.Ready;
                        }
                        //
                        if (communicationState == X10CommState.WaitingAck && readData[0] == (int)X10CommandType.PLC_Ready && readData.Length <= 2) // ack received
                        {
                            Utility.DebugLog(
                                "X10 >",
                                "COMMAND SUCCESSFUL"
                            );
                            communicationState = X10CommState.Ready;
                        }
                        else if ((readData.Length >= 13 || (readData.Length == 2 && readData[0] == 0xFF && readData[1] == 0x00)) && !isInterfaceReady)
                        {
                            UpdateInterfaceTime(false);
                            isInterfaceReady = true;
                            communicationState = X10CommState.Ready;
                        }
                        else if (readData.Length == 2 && communicationState == X10CommState.WaitingChecksum && readData[0] == expectedChecksum && readData[1] == 0x00)
                        {
                            // checksum is received only from CM11
                            Utility.DebugLog(
                                "X10 >",
                                "CKSUM: " + "Expected [" + Utility.ByteArrayToString(new byte[] { expectedChecksum }) + "] Checksum ==> " + Utility.ByteArrayToString(readData)
                            );
                            //TODO: checksum verification not handled, we just reply 0x00 (OK)
                            SendMessage(new byte[] { 0x00 });
                            communicationState = X10CommState.WaitingAck;
                        }
                        else if (readData[0] == (int)X10CommandType.Macro)
                        {
                            lastReceivedTs = DateTime.Now;
                            Utility.DebugLog("X10 >", "MACRO: " + Utility.ByteArrayToString(readData));
                        }
                        else if (readData[0] == (int)X10CommandType.RF)
                        {
                            lastReceivedTs = DateTime.Now;
                            string message = Utility.ByteArrayToString(readData);
                            Utility.DebugLog("X10 >", "RFCOM: " + message);
                            // repeated messages check
                            if (lastRfMessage == message && (lastReceivedTs - lastRfReceivedTs).TotalMilliseconds < 200) 
                            {
                                Utility.DebugLog("X10 >", "RFCOM: ^^^^^^^^^^^^^^^^^ Ignoring repeated message within 200ms");
                                continue;
                            }
                            lastRfMessage = message;
                            lastRfReceivedTs = lastReceivedTs;
                            //
                            if (RfDataReceived != null)
                            {
                                Thread signal = new Thread(() =>
                                {
                                    try { RfDataReceived(new RfDataReceivedAction() { RawData = readData }); } catch { 
                                        // TODO: handle/report exception
                                    }
                                });
                                signal.Start();
                            }

                            // Decode X10 RF Module Command (eg. "5D 20 70 8F 48 B7")
                            if (readData.Length == 6 && readData[1] == 0x20 && ((readData[3] &~ readData[2]) == readData[3] && (readData[5] &~ readData[4]) == readData[5]))
                            {
                                byte hu = readData[2]; // house code + 4th bit of unit code
                                byte hf = readData[4]; // unit code (3 bits) + function code
                                string houseCode = ((X10HouseCode)(Utility.ReverseByte((byte)(hu >> 4)) >> 4)).ToString();
                                switch (hf)
                                {
                                case 0x98: // DIM ONE STEP
                                    CommandEvent_Dim(0x0F);
                                    break;
                                case 0x88: // BRIGHT ONE STEP
                                    CommandEvent_Bright(0x0F);
                                    break;
                                case 0x90: // ALL LIGHTS ON
                                    if (houseCode != "") CommandEvent_AllLightsOn(houseCode);
                                    break;
                                case 0x80: // ALL LIGHTS OFF
                                    if (houseCode != "") CommandEvent_AllUnitsOff(houseCode);
                                    break;
                                default:
                                    string houseUnit = Convert.ToString(hu, 2).PadLeft(8, '0');
                                    string unitFunction = Convert.ToString(hf, 2).PadLeft(8, '0');
                                    string unitCode = (Convert.ToInt16(houseUnit.Substring(5, 1) + unitFunction.Substring(1, 1) + unitFunction.Substring(4, 1) + unitFunction.Substring(3, 1), 2) + 1).ToString();
                                    //
                                    UnselectModules();
                                    SelectModule(houseCode + unitCode);
                                    //
                                    if (unitFunction[2] == '1') // 1 = OFF, 0 = ON
                                    {
                                        CommandEvent_Off();
                                    }
                                    else
                                    {
                                        CommandEvent_On();
                                    }
                                    break;
                                }
                            }

                        }
                        else if ((readData[0] == (int)X10CommandType.PLC_Poll) && readData.Length <= 2)
                        {
                            isInterfaceReady = true;
                            SendMessage(new byte[] { (byte)X10CommandType.PLC_ReplyToPoll }); // reply to poll
                        }
                        else if ((readData[0] == (int)X10CommandType.PLC_FilterFail_Poll) && readData.Length <= 2)
                        {
                            isInterfaceReady = true;
                            SendMessage(new byte[] { (int)X10CommandType.PLC_FilterFail_Poll }); // reply to filter fail poll
                        }
                        else if ((readData[0] == (int)X10CommandType.PLC_Poll))
                        {
                            lastReceivedTs = DateTime.Now;
                            Utility.DebugLog("X10 >", "PLCRX: " + Utility.ByteArrayToString(readData));
                            //
                            if (readData.Length > 3)
                            {
                                int messageLength = readData[1];
                                if (readData.Length > messageLength - 2)
                                {
                                    char[] bitmapData = Convert.ToString(readData[2], 2).PadLeft(8, '0').ToCharArray();
                                    byte[] functionBitmap = new byte[messageLength - 1];
                                    for (int i = 0; i < functionBitmap.Length; i++)
                                    {
                                        functionBitmap[i] = byte.Parse(bitmapData[7 - i].ToString());
                                    }

                                    byte[] messageData = new byte[messageLength - 1];
                                    Array.Copy(readData, 3, messageData, 0, messageLength - 1);
                                    //
                                    // CM15 Extended receive has got inverted data
                                    if (messageLength > 2 && x10interface.GetType().Equals(typeof(CM15)))
                                    {
                                        Array.Reverse(functionBitmap, 0, functionBitmap.Length);
                                        Array.Reverse(messageData, 0, messageData.Length);
                                    }
                                    //
                                    Utility.DebugLog("X10 >", "FNMAP: " + Utility.ByteArrayToString(functionBitmap));
                                    Utility.DebugLog("X10 >", " DATA: " + Utility.ByteArrayToString(messageData));

                                    for (int b = 0; b < messageData.Length; b++)
                                    {
                                        // read current byte data (type: 0x00 address, 0x01 function)
                                        if (functionBitmap[b] == (byte)X10FunctionType.Address) // address
                                        {
                                            X10HouseCode houseCode = (X10HouseCode)Convert.ToInt16(messageData[b].ToString("X2").Substring(0, 1), 16);
                                            X10UnitCode unitCode = (X10UnitCode)Convert.ToInt16(messageData[b].ToString("X2").Substring(1, 1), 16);
                                            string address = Utility.HouseUnitCodeFromEnum(houseCode, unitCode);
                                            //
                                            Utility.DebugLog("X10 >", "      " + b + ") Address = " + address);
                                            //
                                            if (newAddressData)
                                            {
                                                newAddressData = false;
                                                UnselectModules();
                                            }
                                            SelectModule(address);
                                        }
                                        else if (functionBitmap[b] == (byte)X10FunctionType.Function) // function
                                        {
                                            string function = ((X10Command)Convert.ToInt16(messageData[b].ToString("X2").Substring(1, 1), 16)).ToString().ToUpper();
                                            string houseCode = ((X10HouseCode)Convert.ToInt16(messageData[b].ToString("X2").Substring(0, 1), 16)).ToString();
                                            //
                                            Utility.DebugLog("X10 >", "      " + b + ") House code = " + houseCode);
                                            Utility.DebugLog("X10 >", "      " + b + ")    Command = " + function);
                                            //
                                            switch (function)
                                            {
                                            case "ALL_UNITS_OFF":
                                                if (houseCode != "") CommandEvent_AllUnitsOff(houseCode);
                                                break;
                                            case "ALL_LIGHTS_ON":
                                                if (houseCode != "") CommandEvent_AllLightsOn(houseCode);
                                                break;
                                            case "ON":
                                                CommandEvent_On();
                                                break;
                                            case "OFF":
                                                CommandEvent_Off();
                                                break;
                                            case "BRIGHT":
                                                CommandEvent_Bright(messageData[++b]);
                                                break;
                                            case "DIM":
                                                CommandEvent_Dim(messageData[++b]);
                                                break;
                                            }
                                            //
                                            newAddressData = true;
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

                            #region This is an hack for detecting disconnection status on Linux platforms

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
                            else
                            {
                                SendMessage(new byte[] { 0x00 });
                            }

                            #endregion

                        }
                    }
                }
                catch (Exception e)
                {
                    if (!e.GetType().Equals(typeof(TimeoutException)) && !e.GetType().Equals(typeof(OverflowException)))
                    {
                        gotReadWriteError = true;
                        Utility.DebugLog("X10 !", e.Message);
                        Utility.DebugLog("X10 !", e.StackTrace);
                    }
                }
            }
        }

        #endregion

        #endregion

    }

}

