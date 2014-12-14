/*
This file is part of HomeGenie Project source code.
HomeGenie is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
HomeGenie is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with HomeGenie. If not, see <http://www.gnu.org/licenses/>.
*/
/*
* Author: Generoso Martello <gene@homegenie.it>
* Project Homepage: http://homegenie.it
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

        public ZWaveMessageReceivedEventArgs(byte[] msg)
        {
            Message = msg;
        }
    }

    public class ZWavePort
    {
        public delegate void ZWaveMessageReceivedEvent(object sender, ZWaveMessageReceivedEventArgs zwaveargs);

        public ZWaveMessageReceivedEvent ZWaveMessageReceived;
        private byte callbackIdSeq = 1;
        private object callbackLock = new object();
        private SerialPortInput serialPort;
        private string portName = "";
        private List<ZWaveMessage> pendingMessages = new List<ZWaveMessage>();
        private bool isInitialized;
        private Timer discoveryTimer;
        private ManualResetEvent ackWait = new ManualResetEvent(true);

        public ZWavePort()
        {
            serialPort = new SerialPortInput();
            serialPort.Debug = true;
            serialPort.MessageReceived += ReceiveMessage;
            serialPort.ConnectedStateChanged += new SerialPortInput.ConnectedStateChangedEvent(serialport_ConnectedStateChanged);
            discoveryTimer = new Timer((object state) =>
            {
                Discovery();
            });
        }

        public string PortName
        {
            get { return portName; }
            set
            {
                portName = value;
                serialPort.SetPort(value, 115200);
            }
        }

        public bool IsConnected
        {
            get { return serialPort.IsConnected; }
        }

        public bool Connect()
        {
            return serialPort.Connect();
        }

        public void Disconnect()
        {
            serialPort.Disconnect();
        }
        //public void Dispose ()
        //{
        // Disconnect();
        //}
        public void Discovery()
        {
            serialPort.SendMessage(new byte[] { 0x01, 0x03, 0x00, 0x02, 0xFE });
        }

        public List<ZWaveMessage> PendingMessages
        {
            get { return pendingMessages; }
        }

        public void SendAck()
        {
            byte[] MSG_ACKNOWLEDGE = new byte[] { (byte)ZWaveMessageHeader.ACK };
            serialPort.SendMessage(MSG_ACKNOWLEDGE);
        }

        public byte SendMessage(ZWaveMessage message, bool disableCallback = false)
        {
            byte callbackId = 0x00;
            //
            lock (ackWait)
            {
                if (!disableCallback)
                {
                    //ackWait.WaitOne();
                    //ackWait.Reset();
                    ////
                    //// discard timed-out messages (prevent flooding)
                    ////
                    //TimeSpan ttl = new TimeSpan(DateTime.UtcNow.Ticks - msg.Timestamp.Ticks);
                    //if (ttl.TotalSeconds > 5)
                    //{
                    // return 0;
                    //}
                    //
                    if (message.ResendCount == 0)
                    {
                        // Insert the callback id into the message
                        callbackId = GetCallbackId();
                        message.Message[message.Message.Length - 2] = callbackId;
                        message.CallbackId = callbackId;
                    }
                }
                // Insert checksum
                message.Message[message.Message.Length - 1] = GenerateChecksum(message.Message);
                pendingMessages.Add(message);
                //
                serialPort.SendMessage(message.Message);
                //
                // wait for any previous message callback response
                int maxWait = 50; // 5 seconds max wait
                while (pendingMessages.Contains(message) && maxWait > 0)
                {
                    Thread.Sleep(100);
                    maxWait--;
                }
                pendingMessages.Remove(message);
                //
                // remove timed out messages (requeued messages after failure)
                //
                pendingMessages.RemoveAll(zm =>
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
                //ackWait.Set();
            }
            //
            return callbackId;
        }
        
        public byte ResendLastMessage(byte callbackId)
        {
            var message = pendingMessages.Find(zm => zm.CallbackId == callbackId);
            if (message != null)
            {
                pendingMessages.Remove(message);
                if (message.ResendCount < 3)
                {
                    message.ResendCount++;
                    SendMessage(message);
                }
                else // In case of Error if no more resend return NodeID 
                    return message.Message[4]; 
            }
            return 0; // Return 0 When the Resend is OK
        }
        
        public void ResendLastMessage()
        {
            if (pendingMessages.Count > 0)
            {
                var message = pendingMessages[pendingMessages.Count - 1];
                pendingMessages.Remove(message);
                if (message.ResendCount < 3)
                {
                    message.ResendCount++;
                    SendMessage(message);
                }
            }
        }
        
        public byte GetCallbackId()
        {
            lock (this.callbackLock)
            {
                if (++this.callbackIdSeq > 0xFF)
                {
                    this.callbackIdSeq = 1;
                }
                return this.callbackIdSeq;
            }
        }

        public String ByteArrayToString(byte[] message)
        {
            String returnValue = String.Empty;
            foreach (byte b in message)
            {
                returnValue += b.ToString("X2") + " ";
            }
            return returnValue.Trim();
        }

        private void HanldeErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Console.WriteLine("ZWaveLib ERROR: " + e.EventType.ToString() + " => " + e.ToString());
        }

        private static byte GenerateChecksum(byte[] data)
        {
            int offset = 1;
            byte returnValue = data[offset];
            for (int i = offset + 1; i < data.Length - 1; i++)
            {
                // Xor bytes
                returnValue ^= data[i];
            }
            // Not result
            returnValue = (byte)(~returnValue);
            return returnValue;
        }

        private static bool VerifyChecksum(byte[] data)
        {
            //byte[] data = { 0x01, 0x0F, 0x00, 0x04, 0x00, 0x32, 0x09, 0x60, 0x06, 0x03, 0x31, 0x05, 0x01, 0x2A, 0x02, 0xC0, 0x77, 0x18 };
            //{ 0x01, 0x0E, 0x00, 0x04, 0x00, 0x30, 0x08, 0x32, 0x02, 0x21, 0x74, 0x00, 0x00, 0x18, 0x6F, 0xDF };
            if (data.Length < 4) return true;
            int offset = 1;
            byte returnValue = data[offset];
            for (int i = offset + 1; i < data.Length - 1; i++)
            {
                // Xor bytes
                returnValue ^= data[i];
            }
            // Not result
            returnValue = (byte)(~returnValue);
            return (returnValue == data[data.Length - 1]);
        }

        private void ReceiveMessage(byte[] message)
        {
            ZWaveMessageHeader header = (ZWaveMessageHeader)((int)message[0]);
            //
            if (header == ZWaveMessageHeader.ACK)
            {
                ZWaveMessageReceived(
                    this,
                    new ZWaveMessageReceivedEventArgs(new byte[] { (byte)ZWaveMessageHeader.ACK })
                );
                if (message.Length > 1)
                {
                    byte[] msg = new byte[message.Length - 1];
                    Array.Copy(message, 1, msg, 0, msg.Length);
                    ReceiveMessage(msg);
                }
                return;
            }
            //
            int msgLength = 0;
            byte[] nextMessage = null;
            if (message.Length > 1)
            {
                msgLength = (int)message[1];
                if (message.Length > msgLength + 3)
                {
                    nextMessage = new byte[message.Length - msgLength - 2];
                    Array.Copy(message, msgLength + 2, nextMessage, 0, nextMessage.Length);
                    byte[] tmpmsg = new byte[msgLength + 2];
                    Array.Copy(message, 0, tmpmsg, 0, msgLength + 2);
                    message = tmpmsg;
                }
            }
            //
            //Console.WriteLine("=== > " + ByteArrayToString(message));
            //
            if (header == ZWaveMessageHeader.SOF)
            { // found SOF
                //
                byte[] cmdAck = new byte[] { 0x01, 0x04, 0x01, 0x13, 0x01, 0xE8 };
                if (message.SequenceEqual(cmdAck))
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
            }
            else if (header == ZWaveMessageHeader.CAN)
            {
                // RESEND
                ResendLastMessage();
                //
                ZWaveMessageReceived(
                    this,
                    new ZWaveMessageReceivedEventArgs(new byte[] { (byte)ZWaveMessageHeader.CAN })
                );
            }
            else
            {
                Console.WriteLine("ZWaveLib: unhandled message " + ByteArrayToString(message));
                // ZWaveMessageReceived(this, new ZWaveMessageReceivedEventArgs(new byte[] { (byte)ZWaveMessageHeader.NAK }));
            }
            if (nextMessage != null)
            {
                ReceiveMessage(nextMessage);
            }
        }

        private void serialport_ConnectedStateChanged(object sender, ConnectedStateChangedEventArgs statusargs)
        {
            if (statusargs.Connected && !isInitialized)
            {
                discoveryTimer.Change(5000, Timeout.Infinite);
            }
            else
            {
                isInitialized = false;
            }
        }
    }
} 