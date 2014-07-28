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
using System.Threading;

using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;

namespace XTenLib.Drivers
{
    public class CM15 : XTenInterface, IDisposable
    {
        public UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x0BC7, 0x0001);
        // Marmitek CM15Pro usb interface

        /// <summary>Use the first read endpoint</summary>
        public readonly byte TRANFER_ENDPOINT = UsbConstants.ENDPOINT_DIR_MASK;

        /// <summary>Number of transfers to sumbit before waiting begins</summary>
        public readonly int TRANFER_MAX_OUTSTANDING_IO = 3;

        /// <summary>Number of transfers before terminating the test</summary>
        public readonly int TRANSFER_COUNT = 30;

        /// <summary>Size of each transfer</summary>
        public int TRANFER_SIZE = 16;

        //private DateTime startTime = DateTime.MinValue;
        private UsbDevice myUsbDevice;

        private UsbEndpointReader reader = null;
        private UsbEndpointWriter writer = null;


        public CM15()
        {
        }

        public void Dispose()
        {
            if (myUsbDevice != null && myUsbDevice.IsOpen)
            {
                try
                {
                    reader.Abort();
                }
                catch
                {
                }
                //
                try
                {
                    writer.Abort();
                }
                catch
                {
                }
            }
        }

        public void Close()
        {
            this.Dispose();
            if (myUsbDevice != null)
            {
                if (myUsbDevice.DriverMode == UsbDevice.DriverModeType.MonoLibUsb)
                {
                    try
                    {
                        myUsbDevice.Close();
                    }
                    catch
                    {
                    }
                }
                myUsbDevice = null;
            }
        }

        public bool Open()
        {
            bool success = true;
            //
            try
            {
                // Find and open the usb device.
                myUsbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);
                //
                // If the device is open and ready
                if (myUsbDevice == null) throw new Exception("X10 CM15Pro device not connected.");
                //
                // If this is a "whole" usb device (libusb-win32, linux libusb)
                // it will have an IUsbDevice interface. If not (WinUSB) the 
                // variable will be null indicating this is an interface of a 
                // device.
                IUsbDevice wholeUsbDevice = myUsbDevice as IUsbDevice;
                if (!ReferenceEquals(wholeUsbDevice, null))
                {
                    // This is a "whole" USB device. Before it can be used, 
                    // the desired configuration and interface must be selected.
                    //
                    // Select config #1
                    wholeUsbDevice.SetConfiguration(1);
                    //
                    // Claim interface #0.
                    wholeUsbDevice.ClaimInterface(0);
                }
                //
                // open read endpoint 1.
                reader = myUsbDevice.OpenEndpointReader(ReadEndpointID.Ep01);
                // open write endpoint 2.
                writer = myUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep02);
                //
                this.WriteData(new byte[] { 0x8B }); // status request
            }
            catch
            {
                success = false;
                //throw new Exception("Error opening X10 CM15Pro device.");
            }
            return success;
        }


        public byte[] ReadData()
        {
            ErrorCode ecRead;
            int transferredIn;
            UsbTransfer usbReadTransfer = null;
            byte[] readBuffer;
            //
            readBuffer = new byte[16];
            ecRead = reader.SubmitAsyncTransfer(readBuffer, 0, 8, 1000, out usbReadTransfer);
            if (ecRead != ErrorCode.None)
            {
                throw new Exception("Submit Async Read Failed.");
            }
            //
            WaitHandle.WaitAll(new WaitHandle[] { usbReadTransfer.AsyncWaitHandle }, 1000, false);
            ecRead = usbReadTransfer.Wait(out transferredIn);
            //
            if (!usbReadTransfer.IsCompleted)
            {
                ecRead = reader.SubmitAsyncTransfer(readBuffer, 8, 8, 1000, out usbReadTransfer);
                if (ecRead != ErrorCode.None)
                {
                    throw new Exception("Submit Async Read Failed.");
                }
                WaitHandle.WaitAll(new WaitHandle[] { usbReadTransfer.AsyncWaitHandle }, 1000, false);
            }
            //
            if (!usbReadTransfer.IsCompleted) usbReadTransfer.Cancel();
            try
            {
                ecRead = usbReadTransfer.Wait(out transferredIn);
            }
            catch
            {
            }
            usbReadTransfer.Dispose();
            //
            byte[] readdata = new byte[transferredIn];
            Array.Copy(readBuffer, readdata, transferredIn);
            //
            return readdata;
        }

        public void WriteData(byte[] bytesToSend)
        {
            ErrorCode ecWrite;
            int transferredOut;
            UsbTransfer usbWriteTransfer = null;
            //
            ecWrite = writer.SubmitAsyncTransfer(bytesToSend, 0, bytesToSend.Length, 1000, out usbWriteTransfer);
            if (ecWrite != ErrorCode.None)
            {
                throw new Exception("Submit Async Write Failed.");
            }
            //
            WaitHandle.WaitAll(new WaitHandle[] { usbWriteTransfer.AsyncWaitHandle }, 1000, false);
            //
            if (!usbWriteTransfer.IsCompleted) usbWriteTransfer.Cancel();
            ecWrite = usbWriteTransfer.Wait(out transferredOut);
            // TODO: should check if transferredOut != bytesToSend.length, and eventually resend?
            usbWriteTransfer.Dispose();
        }

        public static byte[] BuildTransceivedCodesMessage(string csMonitoredCodes)
        {
            ushort transceivedCodes = 0;
            //
            if (csMonitoredCodes.Contains("A"))
            {
                transceivedCodes |= (ushort)Math.Pow(2, 14);
            }
            if (csMonitoredCodes.Contains("B"))
            {
                transceivedCodes |= (ushort)Math.Pow(2, 6);
            }
            if (csMonitoredCodes.Contains("C"))
            {
                transceivedCodes |= (ushort)Math.Pow(2, 10);
            }
            if (csMonitoredCodes.Contains("D"))
            {
                transceivedCodes |= (ushort)Math.Pow(2, 2);
            }
            if (csMonitoredCodes.Contains("E"))
            {
                transceivedCodes |= (ushort)Math.Pow(2, 9);
            }
            if (csMonitoredCodes.Contains("F"))
            {
                transceivedCodes |= (ushort)Math.Pow(2, 1);
            }
            if (csMonitoredCodes.Contains("G"))
            {
                transceivedCodes |= (ushort)Math.Pow(2, 13);
            }
            if (csMonitoredCodes.Contains("H"))
            {
                transceivedCodes |= (ushort)Math.Pow(2, 5);
            }
            if (csMonitoredCodes.Contains("I"))
            {
                transceivedCodes |= (ushort)Math.Pow(2, 15);
            }
            if (csMonitoredCodes.Contains("J"))
            {
                transceivedCodes |= (ushort)Math.Pow(2, 7);
            }
            if (csMonitoredCodes.Contains("K"))
            {
                transceivedCodes |= (ushort)Math.Pow(2, 11);
            }
            if (csMonitoredCodes.Contains("L"))
            {
                transceivedCodes |= (ushort)Math.Pow(2, 3);
            }
            if (csMonitoredCodes.Contains("M"))
            {
                transceivedCodes |= (ushort)Math.Pow(2, 8);
            }
            if (csMonitoredCodes.Contains("N"))
            {
                transceivedCodes |= (ushort)Math.Pow(2, 0);
            }
            if (csMonitoredCodes.Contains("O"))
            {
                transceivedCodes |= (ushort)Math.Pow(2, 12);
            }
            if (csMonitoredCodes.Contains("P"))
            {
                transceivedCodes |= (ushort)Math.Pow(2, 4);
            }
            //
            byte b1 = (byte)(transceivedCodes >> 8);
            byte b2 = (byte)(transceivedCodes);
            //
            //byte[] trcommand = new byte[] { 0xbb, 0xff, 0xff, 0x05, 0x00, 0x14, 0x20, 0x28, 0x24, 0x29 }; // transceive all
            //byte[] trcommand = new byte[] { 0xbb, 0x40, 0x00, 0x05, 0x00, 0x14, 0x20, 0x28, 0x24, 0x29 }; // autodetect
            byte[] trCommand = new byte[] { 0xbb, b1, b2, 0x05, 0x00, 0x14, 0x20, 0x28, 0x24, 0x29 };
            return trCommand;
        }
    }
}

