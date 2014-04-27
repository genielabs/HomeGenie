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
using System;
using System.IO;
using System.IO.Ports;

namespace W800RF32
{
    public class RfDirect
    {
        
        private SerialPort serialPort;

        private object commLock = new object();
        private string portName = "";
        private bool isConnected = false;

        
        public RfDirect(string portname)
        {
            portName = portname;
        }

        public void Close()
        {
            if (serialPort != null)
            {
                try
                {
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
                //				this.WriteData(new byte[] { 0x8B }); // status request
                success = true;
                //Console.WriteLine("OK");
            }
            catch (Exception ex)
            {
                // TODO: add error logging 
            }
            //
            return success;
        }

        public byte[] ReadData()
        {
            int buflen = 4;
            int length = 0;
            int readBytes = 0;
            byte[] buffer = new byte[buflen];
            //
            do
            {
                readBytes = serialPort.Read(buffer, length, buflen - length);
                length += readBytes;
                //
                //if (length > 1 && buffer[0] < length) 
                //	break;
                //else if (buffer[0] > 0x10 && _serialport.BytesToRead == 0) 
                //	break;
                //if (_serialport.BytesToRead == 0) break;
                System.Threading.Thread.Sleep(5);
            } while (readBytes > 0 && (buflen - length > 0));
            //
            byte[] readData = new byte[length];
            //			if (length > 1)
            //			{
            //				readdata[0] = (int)X10CommandType.PLC_Poll;
            //				Array.Copy(buffer, 0, readdata, 1, length);
            //			}
            //			else
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

