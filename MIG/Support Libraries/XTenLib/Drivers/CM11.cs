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
		private SerialPort _serialport;

		private object _commlock = new object();
		private string _portname = "COM6"; //"/dev/ttyUSB1";
		private bool _isconnected = false;

		public CM11 (string portname)
		{
			_portname = portname;
		}

		public void Close ()
		{
			if (_serialport != null)
			{
//////-->				_serialport.ErrorReceived -= HanldeErrorReceived;
				try
				{
//					_serialport.Dispose();
					_serialport.Close();
				} catch { }
				_serialport = null;
			}
		}
		public bool Open ()
		{
			bool success = false;
			//
			try
			{
                bool tryopen = (_serialport == null);
                if (Environment.OSVersion.Platform.ToString().StartsWith("Win") == false)
                {
                    tryopen = (tryopen && System.IO.File.Exists(_portname));
                }
				if (tryopen) {
					_serialport = new SerialPort();
					_serialport.PortName = _portname;
					_serialport.BaudRate = 4800;
					_serialport.Parity = Parity.None;
					_serialport.DataBits = 8;
					_serialport.StopBits = StopBits.One;
					_serialport.ReadTimeout = 150;
					_serialport.WriteTimeout = 150;
                    //_serialport.RtsEnable = true;
					//
////////-->					_serialport.ErrorReceived += HanldeErrorReceived;
					// DataReceived event won't work under Linux / Mono
					//sp.DataReceived += HandleDataReceived;
				}
				if (_serialport.IsOpen == false)
				{
					_serialport.Open();
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

		public byte[] ReadData ()
		{
			int buflen = 32;
			int length = 0;
			int readbytes = 0;
			byte[] buffer = new byte[buflen];
			//
            do
            {
                readbytes = _serialport.Read(buffer, length, buflen - length);
                length += readbytes;
                //
                if (length > 1 && buffer[0] < length) 
                    break;
                else if (buffer[0] > 0x10 && _serialport.BytesToRead == 0) 
                    break;
            } while (readbytes > 0 && (buflen - length > 0));
            //
			byte[] readdata = new byte[length + 1];
            if (length > 1 && length < 13)
            {
                readdata[0] = (int)X10CommandType.PLC_Poll;
                Array.Copy(buffer, 0, readdata, 1, length);
            }
            else
            {
                Array.Copy(buffer, readdata, length);
            }
                //
			return readdata;
		}

		public void WriteData(byte[] bytesToSend)
		{
			if ( _serialport != null && _serialport.IsOpen)
			{
				_serialport.Write(bytesToSend, 0, bytesToSend.Length);
            }
		}

	}
}

