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
using System.IO.Ports;
using System.Threading;
using System.Linq;

using SerialPortLib;
using ZWaveLib.Devices;

namespace ZWaveLib
{

	public enum ZWaveMessageHeader
	{
		SOF = 0x01,
		ACK = 0x06,
		NAK = 0x15,
		CAN = 0x18
	}

	public enum ZWaveMessageType
	{
		REQUEST = 0x00,
		RESPONSE = 0x01
	}

	public class ZWaveMessageReceivedEventArgs
	{
		public byte[] Message;

		public ZWaveMessageReceivedEventArgs (byte[] msg)
		{
			Message = msg;
		}
	}

	public class ZWavePort
	{      
		public delegate void ZWaveMessageReceivedEvent (object sender, ZWaveMessageReceivedEventArgs zwaveargs);
		public ZWaveMessageReceivedEvent ZWaveMessageReceived;

        private byte _callbackidseq = 1;
		private object _callbacklock = new object ();

		private Logger _logger = new Logger ();

        private SerialPortInput _serialport;
        private string _portname = "/dev/ttyUSB0";

        private List<ZWaveMessage> _pendingmsgs = new List<ZWaveMessage>();
        private bool _initialized;

        private Timer _discoverytimer;

        private ManualResetEvent _ackwait = new ManualResetEvent(true);


        public ZWavePort()
		{
            _serialport = new SerialPortInput();
            _serialport.Debug = true;
            _serialport.MessageReceived += ReceiveMessage;
            _serialport.ConnectedStateChanged += new SerialPortInput.ConnectedStateChangedEvent(_serialport_ConnectedStateChanged);
            _discoverytimer = new Timer((object state) => {
                Discovery();
            });
		}

		public string PortName {
			get { return _portname; }
			set {
                _portname = value;
                _serialport.SetPort(value, 115200);
			}
		}

		public bool IsConnected {
            get { return _serialport.IsConnected; }
		}

		public bool Connect ()
		{
            return _serialport.Connect();
		}

		public void Disconnect ()
		{
            _serialport.Disconnect();
		}

        //public void Dispose ()
        //{
        //    Disconnect();
        //}

        public void Discovery()
        {
            _serialport.SendMessage(new byte[] { 0x01, 0x03, 0x00, 0x02, 0xFE });
        }

        public List<ZWaveMessage> PendingMessages
        {
            get { return _pendingmsgs; }
        }

        
        public void SendAck()
        {
            byte[] MSG_ACKNOWLEDGE = new byte[] { (byte)ZWaveMessageHeader.ACK };
            _serialport.SendMessage(MSG_ACKNOWLEDGE);
        }

        public byte SendMessage(ZWaveMessage msg, bool disablecallback = false)
		{
            byte callbackid = 0x00;
            //
            if (!disablecallback)
            {
                _ackwait.WaitOne();
                _ackwait.Reset();
                ////
                //// discard timed-out messages (prevent flooding)
                ////
                //TimeSpan ttl = new TimeSpan(DateTime.UtcNow.Ticks - msg.Timestamp.Ticks);
                //if (ttl.TotalSeconds > 5)
                //{
                //    return 0;
                //}
                //
                if (msg.ResendCount == 0)
                {
                    // Insert the callback id into the message
                    callbackid = GetCallbackId();
                    msg.Message[msg.Message.Length - 2] = callbackid;
                    msg.CallbackId = callbackid;
                }
            }
            // Insert checksum
            msg.Message[msg.Message.Length - 1] = GenerateChecksum(msg.Message); 
            _pendingmsgs.Add(msg);
            //
            _serialport.SendMessage(msg.Message);
            //
            // wait for any previous message callback response
            int maxwait = 50; // 5 seconds max wait
            while (_pendingmsgs.Contains(msg) && maxwait > 0)
            {
                Thread.Sleep(100);
                maxwait--;
            }
            _pendingmsgs.Remove(msg);
            //
            // remove timed out messages (requeued messages after failure)
            //
            _pendingmsgs.RemoveAll(zm =>
            {
                TimeSpan ttl = new TimeSpan(DateTime.UtcNow.Ticks - zm.Timestamp.Ticks);
                if (ttl.TotalSeconds >= 5)
                {
                    return true;
                }
                return false;
            });
            //
            Thread.Sleep(300);
            _ackwait.Set();
            //
            return callbackid;
        }

        public void ResendLastMessage(byte callbackid)
        {
            ZWaveMessage msg = _pendingmsgs.Find(zm => zm.CallbackId == callbackid);
            if (msg != null)
            {
                _pendingmsgs.Remove(msg);
                if (msg.ResendCount < 3)
                {
                    msg.ResendCount++;
                    SendMessage(msg);
                }
            }
        }

        public void ResendLastMessage()
        {
            if (_pendingmsgs.Count > 0)
            {
                ZWaveMessage msg = _pendingmsgs[_pendingmsgs.Count - 1];
                _pendingmsgs.Remove(msg);
                if (msg.ResendCount < 3)
                {
                    msg.ResendCount++;
                    SendMessage(msg);
                }
            }
        }

		
		public byte GetCallbackId ()
		{
			lock (this._callbacklock) 
            {
				if (++this._callbackidseq > 0xFF) 
                {
					this._callbackidseq = 1;
				}
				return this._callbackidseq;
			}
		}

        public ManualResetEvent CallbackAckEvent
        {
            get { return _ackwait; }
        }

		public String ByteArrayToString (byte[] message)
		{
			String ret = String.Empty;
			foreach (byte b in message) 
            {
				ret += b.ToString ("X2") + " ";
			}
			return ret.Trim ();
		}   		


 
		private void HanldeErrorReceived (object sender, SerialErrorReceivedEventArgs e)
		{
Console.WriteLine("ZWaveLib ERROR: " + e.EventType.ToString() + " => " + e.ToString());
		}


		private static byte GenerateChecksum (byte[] data)
		{
			int offset = 1;
			byte ret = data [offset];
			for (int i = offset + 1; i < data.Length - 1; i++) {
				// Xor bytes
				ret ^= data [i];
			}
			// Not result
			ret = (byte)(~ret);
			return ret;
		}

        private static bool VerifyChecksum(byte[] data)
        {
            //byte[] data = { 0x01, 0x0F, 0x00, 0x04, 0x00, 0x32, 0x09, 0x60, 0x06, 0x03, 0x31, 0x05, 0x01, 0x2A, 0x02, 0xC0, 0x77, 0x18 };
            //{ 0x01, 0x0E, 0x00, 0x04, 0x00, 0x30, 0x08, 0x32, 0x02, 0x21, 0x74, 0x00, 0x00, 0x18, 0x6F, 0xDF };
            int offset = 1;
            byte ret = data[offset];
            for (int i = offset + 1; i < data.Length - 1; i++)
            {
                // Xor bytes
                ret ^= data[i];
            }
            // Not result
            ret = (byte)(~ret);

            return (ret == data[data.Length - 1]);
        }

		private void ReceiveMessage (byte[] message)
		{
            ZWaveMessageHeader header = (ZWaveMessageHeader)((int)message[0]);
            //
            if (header == ZWaveMessageHeader.ACK)
            {
                Logger.Log(LogLevel.DEBUG_IN, ByteArrayToString(new byte[] { (byte)ZWaveMessageHeader.ACK }));
                //
                ZWaveMessageReceived(this, new ZWaveMessageReceivedEventArgs(new byte[] { (byte)ZWaveMessageHeader.ACK }));
                if (message.Length > 1)
                {
                    byte[] msg = new byte[message.Length - 1];
                    Array.Copy(message, 1, msg, 0, msg.Length);
                    ReceiveMessage(msg);
                }
                return;
            }
            //
            int msglen = 0;
            byte[] nextmessage = null;
            if (message.Length > 1)
            {
                msglen = (int)message[1];
                if (message.Length > msglen + 3)
                {
                    nextmessage = new byte[message.Length - msglen - 2];
                    Array.Copy(message, msglen + 2, nextmessage, 0, nextmessage.Length);
                    byte[] tmpmsg = new byte[msglen + 2];
                    Array.Copy(message, 0, tmpmsg, 0, msglen + 2);
                    message = tmpmsg;
                }
            }
            //
            //Console.WriteLine("=== > " + ByteArrayToString(message));
            //
			if (header == ZWaveMessageHeader.SOF) { // found SOF
				//
                Logger.Log(LogLevel.DEBUG_IN, ByteArrayToString(message));
				//
                byte[] cmdack = new byte[]{ 0x01, 0x04, 0x01, 0x13, 0x01, 0xE8 };
                if (message.SequenceEqual(cmdack))
                {
                    //_ackwait.Set();
                }
                else if (VerifyChecksum(message))
                {
                    ZWaveMessageReceived(this, new ZWaveMessageReceivedEventArgs(message));
                }
                else
                {
Console.WriteLine("\nZWaveLib: bad checksum message " + ByteArrayToString(message) + "\n");
                }
			} else if (header == ZWaveMessageHeader.CAN) {
                // RESEND
                ResendLastMessage();
                //
				Logger.Log (LogLevel.WARNING, ByteArrayToString (message));
				//
                ZWaveMessageReceived(this, new ZWaveMessageReceivedEventArgs(new byte[] { (byte)ZWaveMessageHeader.CAN }));
			} else {
Console.WriteLine("ZWaveLib: unhandled message " + ByteArrayToString(message));
                Logger.Log(LogLevel.ERROR, ByteArrayToString(message));
//                ZWaveMessageReceived(this, new ZWaveMessageReceivedEventArgs(new byte[] { (byte)ZWaveMessageHeader.NAK }));
			}

            if (nextmessage != null)
            {
                ReceiveMessage(nextmessage);
            }
		}

        private void _serialport_ConnectedStateChanged(object sender, ConnectedStateChangedEventArgs statusargs)
        {
            if (statusargs.Connected && !_initialized)
            {
                _discoverytimer.Change(5000, Timeout.Infinite);
            }
            else
            {
                _initialized = false;
            }
        }

	}
}
