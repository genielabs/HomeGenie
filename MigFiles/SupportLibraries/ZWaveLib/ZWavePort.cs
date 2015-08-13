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
using ZWaveLib.Values;

namespace ZWaveLib
{

    public class ZWavePort
    {

        #region Private fields

        private string portName = "";
        private SerialPortInput serialPort;

        private byte callbackIdSeq = 2;
        private object callbackLock = new object();
        private object sendLock = new object();

        private List<ZWaveMessage> pendingMessages = new List<ZWaveMessage>();

        private bool isInitialized;
        private Timer discoveryTimer;

        #endregion Private fields
        
        #region Public fields

        public delegate void ZWaveMessageReceivedEvent(object sender, ZWaveMessageReceivedEventArgs zwaveargs);
        public ZWaveMessageReceivedEvent ZWaveMessageReceived;

        #endregion Public fields

        #region Lifecycle

        public ZWavePort()
        {
            serialPort = new SerialPortInput();
            serialPort.Debug = true;
            serialPort.MessageReceived += ReceiveMessage;
            serialPort.ConnectedStateChanged += new SerialPortInput.ConnectedStateChangedEvent(serialport_ConnectedStateChanged);
            // TODO: discovery should be moved to Controller.cs
            discoveryTimer = new Timer((object state) =>
            {
                Discovery();
            });
        }

        #endregion Lifecycle

        #region Public members

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
            byte[] MSG_ACKNOWLEDGE = new byte[] { (byte)MessageHeader.ACK };
            serialPort.SendMessage(MSG_ACKNOWLEDGE);
        }
        
        public void SendNack()
        {
            byte[] MSG_NOACKNOWLEDGE = new byte[] { (byte)MessageHeader.NAK };
            serialPort.SendMessage(MSG_NOACKNOWLEDGE);
        }

        public byte SendMessage(ZWaveMessage message, bool disableCallback = false)
        {
            byte callbackId = 0x00;
            lock (sendLock)
            {
                // Insert checksum
                if (disableCallback)
                {
                    message.Message[message.Message.Length - 1] = GenerateChecksum(message.Message);
                    serialPort.SendMessage(message.Message);
                }
                else
                {
                    if (message.ResendCount == 0)
                    {
                        // Insert the callback id into the message
                        callbackId = GetCallbackId();
                        message.Message[message.Message.Length - 2] = callbackId;
                        message.CallbackId = callbackId;
                    }
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
                }
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
                Thread.Sleep(100);
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
                else
                {
                    // In case of timeout (max retries exceeded) return NodeID 
                    return message.Message[4]; 
                }
            }
            // Return 0 if resending was succesful
            return 0;
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
                    this.callbackIdSeq = 2;
                }
                return this.callbackIdSeq;
            }
        }

        #endregion Public members

        #region Private members

        private void HanldeErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Utility.DebugLog(DebugMessageType.Error, e.EventType.ToString() + " => " + e.ToString());
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
            uint checksum = 0xff;
            for( int i = 1; i < (data.Length - 1); ++i)
            {
                checksum ^= data[i];
            }        
            return (checksum == data[data.Length - 1]);
        }

        private void ReceiveMessage(byte[] message)
        {
            MessageHeader header = (MessageHeader)((int)message[0]);
            if (header == MessageHeader.ACK)
            {
                this.SendAck();
                ZWaveMessageReceived(this, new ZWaveMessageReceivedEventArgs(new byte[] { (byte)MessageHeader.ACK }));
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
            if (header == MessageHeader.SOF)
            {
                byte[] cmdAck = new byte[] { 0x01, 0x04, 0x01, 0x13, 0x01, 0xE8 };
                if (message.SequenceEqual(cmdAck))
                {
                    // TODO: ?!?
                }
                else if (VerifyChecksum(message))
                {
                    this.SendAck();
                    ZWaveMessageReceived(this, new ZWaveMessageReceivedEventArgs(message));
                }
                else
                {
                    this.SendNack();
                    Utility.DebugLog(DebugMessageType.Warning, "Bad checksum message " + Utility.ByteArrayToString(message));
                }
            }
            else if (header == MessageHeader.CAN)
            {
                // Resend
                ResendLastMessage();
                ZWaveMessageReceived(this, new ZWaveMessageReceivedEventArgs(new byte[] { (byte)MessageHeader.CAN }));
            }
            else
            {
                Utility.DebugLog(DebugMessageType.Warning, "Unhandled message " + Utility.ByteArrayToString(message));
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

        #endregion Private members

    }

} 