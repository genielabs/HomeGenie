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
using System.IO;
using System.IO.Ports;

namespace XTenLib.Drivers
{
    public class CM11 : XTenInterface
    {
        private SerialPort serialPort;

        private object commLock = new object();
        private string portName = "COM6"; //"/dev/ttyUSB1";
        private bool isConnected = false;

        public CM11(string port)
        {
            portName = port;
        }

        public void Close()
        {
            if (serialPort != null)
            {
                //////-->				_serialport.ErrorReceived -= HanldeErrorReceived;
                try
                {
                    //					_serialport.Dispose();
                    serialPort.Close();
                }
                catch { }
                serialPort = null;
            }
        }
        public bool Open()
        {
            bool success = false;
            //
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
                    serialPort.BaudRate = 4800;
                    serialPort.Parity = Parity.None;
                    serialPort.DataBits = 8;
                    serialPort.StopBits = StopBits.One;
                    serialPort.ReadTimeout = 150;
                    serialPort.WriteTimeout = 150;
                    //_serialport.RtsEnable = true;
                    //
                    ////////-->					_serialport.ErrorReceived += HanldeErrorReceived;
                    // DataReceived event won't work under Linux / Mono
                    //sp.DataReceived += HandleDataReceived;
                }
                if (serialPort.IsOpen == false)
                {
                    serialPort.Open();
                }
                //
                this.WriteData(new byte[] { 0x8B }); // status request
                success = true;
            }
            catch (Exception ex)
            {
            }
            //
            return success;
        }

        public byte[] ReadData()
        {
            int buflen = 32;
            int length = 0;
            int readBytes = 0;
            byte[] buffer = new byte[buflen];
            //
            do
            {
                readBytes = serialPort.Read(buffer, length, buflen - length);
                length += readBytes;
                //
                if (length > 1 && buffer[0] < length)
                    break;
                else if (buffer[0] > 0x10 && serialPort.BytesToRead == 0)
                    break;
            } while (readBytes > 0 && (buflen - length > 0));
            //
            byte[] readData = new byte[length + 1];
            if (length > 1 && length < 13)
            {
                readData[0] = (int)X10CommandType.PLC_Poll;
                Array.Copy(buffer, 0, readData, 1, length);
            }
            else
            {
                Array.Copy(buffer, readData, length);
            }
            //
            return readData;
        }

        public void WriteData(byte[] bytesToSend)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Write(bytesToSend, 0, bytesToSend.Length);
            }
        }

    }
}

