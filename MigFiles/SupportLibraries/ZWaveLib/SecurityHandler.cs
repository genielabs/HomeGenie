using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using ZWaveLib.Values;
using ZWaveLib.Devices;

namespace ZWaveLib
{
    public class SecurityHandler
    {
        private Stopwatch c_nonceTimer = new Stopwatch();
        private AES_work enc;

        internal byte[] NetworkKey;
        //internal byte[] lNetworkKey = new byte[] { 0x0F, 0x1E, 0x2D, 0x3C, 0x4B, 0x5A, 0x69, 0x78, 0x87, 0x96, 0xA5, 0xB4, 0xC3, 0xD2, 0xE1, 0xF0 };
        internal byte[] lNetworkKey = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };
        //        internal byte[] lNetworkKey = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        byte[] c_currentNonce = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA };
        internal byte[] d_currentNonce = null;

        private byte[] encryptKey = null;
        private byte[] authKey = null;

        public bool adding_node = false;
        public bool schemeRequestSent = false;
        private List<SecutiryPayload> secure_payload = new List<SecutiryPayload>();
        private bool waitingForNonce = false;
        private bool networkKeySet = false;
        private Stopwatch d_nonceTimer = new Stopwatch();

        private int m_sequenceCounter = 0;


        private void init(ZWaveNode node)
        {
            //ZWaveNode node = null;
            if (enc == null)
                enc = new AES_work();
            SetupNetworkKey(node);

        }

        public void getScheme(ZWaveNode node){

            if (adding_node)
            {
                schemeRequestSent = true;
                node.SendRequest(new byte[]{
                    (byte)CommandClass.Security,
                    (byte)SecurityCommand.COMMAND_SCHEME_GET,
                    0
                });
            }
        }

        public void SendNonceReport(ZWaveNode node)
        {
            byte[] message = new byte[10];

            message[0] = (byte)CommandClass.Security;
            message[1] = (byte)SecurityCommand.COMMAND_NONCE_REPORT;

            Array.Copy(c_currentNonce, 0, message, 2, 8);

            node.SendRequest(message);
            //nodeHost.ZWaveMessage(message, true, true);
            //            c_nonceTimer.Restart();
            c_nonceTimer.Reset();
        }

        private void SetupNetworkKey(ZWaveNode node)
        {
            byte[] iNetworkKey = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            byte[] EncryptPassword = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA };
            byte[] AuthPassword = new byte[] { 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55 };

            //            byte[] NetworkKey;


            if (adding_node && !networkKeySet)
            {
                NetworkKey = iNetworkKey;
                Utility.logMessage("In SetupNetworkKey  - in node inclusion mode.");
            }
            else
            {
                NetworkKey = lNetworkKey;
            }

            encryptKey = enc.ECB_EncryptMessage(NetworkKey, EncryptPassword);
            authKey = enc.ECB_EncryptMessage(NetworkKey, AuthPassword);

            // Utility.logMessage("In SetupNetworkKey  - Network key - " + Utility.ByteArrayToString(NetworkKey));
            // Utility.logMessage("In SetupNetworkKey  - Encrypt key - " + Utility.ByteArrayToString(encryptKey));
            // Utility.logMessage("In SetupNetworkKey  - Authent key - " + Utility.ByteArrayToString(authKey));

            //            NetworkKey = lNetworkKey;

        }

        public void sendSupportedGet(ZWaveNode node)
        {
            var message = ZWaveMessage.CreateRequest(node.Id, new byte[] { 
			    (byte)CommandClass.Security,
			    (byte)SecurityCommand.COMMAND_SUPPORTED_GET
            });

            encryptAndSend(node, message);

        }

        public void ProcessNonceReport(ZWaveNode node, byte[] message, int start) {
            d_nonceTimer.Restart();
            //                    Logger.Log(LogLevel.REPORT, "In sendRequestNonce - d_nonceTimer restart - " + d_nonceTimer.ElapsedMilliseconds + " - SecurityHandler");

            ProcessNonceReport(message, start);
            EncryptMessage(node, message);
            waitingForNonce = false;
            // if we still have items in the queue request a new nonce
            if (secure_payload.Count > 0)
            {
                sendRequestNonce(node);
            }
        }

        // use with care
        public bool encryptAndSend(ZWaveNode node, byte[] message)
        {
            Utility.logMessage("In encryptAndSend - SecurityHandler - " + Utility.ByteArrayToString(message));
            sendMsg(node, message);
            return true;
        }

        private void sendMsg(ZWaveNode node, byte[] message)
        {

            Utility.logMessage("In sendMsg - SecurityHandler");

            if (message.Length < 7)
            {
                Utility.logMessage("Message too short");
            }

            if (message[3] != 0x13)
            {
                Utility.logMessage("Invalid Message type");
            }

            int length = message[5];

            if (length > 28)
            {
                SecutiryPayload t_payload = new SecutiryPayload();
                t_payload.length = 28;
                t_payload.part = 1;
                byte[] t_message = new byte[t_payload.length];
                System.Array.Copy(message, 6, t_message, 0, t_payload.length);
                t_payload.message = t_message;
                QueuePayload(node, t_payload);

                SecutiryPayload t_payload2 = new SecutiryPayload();
                t_payload2.length = length - 28;
                t_payload2.part = 2;
                byte[] t_message2 = new byte[t_payload.length];
                System.Array.Copy(message, 34, t_message2, 0, t_payload2.length);
                t_payload2.message = t_message2;
                QueuePayload(node, t_payload2);
            }
            else
            {
                SecutiryPayload t_payload = new SecutiryPayload();
                t_payload.length = length;
                t_payload.part = 0;
                byte[] t_message = new byte[t_payload.length];
                System.Array.Copy(message, 6, t_message, 0, t_payload.length);
                t_payload.message = t_message;
                QueuePayload(node, t_payload);
            }
        }

        private void QueuePayload(ZWaveNode node, SecutiryPayload payload)
        {
            lock (secure_payload)
            {
                secure_payload.Add(payload);
                if (d_nonceTimer.ElapsedMilliseconds > 10000)
                    waitingForNonce = false;
                if (!waitingForNonce)
                {
                    sendRequestNonce(node);
                }
            }
        }

        private bool sendRequestNonce(ZWaveNode node)
        {
            Utility.logMessage("In sendRequestNonce - SecurityHandler");
            //            if ((waitingForNonce || d_nonceTimer.ElapsedMilliseconds < 10000) && d_currentNonce != null)
            if (waitingForNonce)
                return false;
            Utility.logMessage("In sendRequestNonce - not waiting for Nonce - SecurityHandler");
            waitingForNonce = true;

            node.SendRequest(new byte[]{
                (byte)CommandClass.Security,
                (byte)SecurityCommand.COMMAND_NONCE_GET
            });

            return true;
        }

        // IN the mesage to be Encrypted
        // OUT - true - message processed and sent - proceed to next one
        //     - false - we need to wait for the nonce report to come
        private bool EncryptMessage(ZWaveNode node, byte[] message)
        {
            if (enc == null)
                init(node);

            Utility.logMessage("In EncryptMessage - secure_payload [" + secure_payload.Count + "]  - " + d_nonceTimer.ElapsedMilliseconds);

            // if we get true we need to wait for the new Nonce
            // if we get false we need to proceed
            //            if (sendRequestNonce())
            //                return false;
            if (d_nonceTimer.ElapsedMilliseconds > 10000)
                return false;

            SecutiryPayload payload = null;
            lock (secure_payload)
            {
                if (secure_payload.Count > 0)
                {
                    payload = secure_payload.First();
                    secure_payload.Remove(payload);
                }
            }

            if (payload != null)
            {
                int len = payload.length + 20;
                /*                node.ZWaveMessage(new byte[] { 
                                    node.NodeId,
                                    (byte)len,
                                    (byte)CommandClass.COMMAND_CLASS_SECURITY,
                                    (byte)SecurityCommand.COMMAND_MESSAGE_ENCAP,
                                    (byte)0xAA,
                                    (byte)0xAA,
                                    (byte)0xAA,
                                    (byte)0xAA,
                                    (byte)0xAA,
                                    (byte)0xAA,
                                    (byte)0xAA,
                                    (byte)0xAA
                                });
                 */
                byte[] t_message = new byte[len];

                int i = 0;
                //                t_message[i] = node.NodeId;
                //                i++; t_message[i] = (byte)len;
                t_message[i] = (byte)CommandClass.Security;
                i++; t_message[i] = (byte)SecurityCommand.COMMAND_MESSAGE_ENCAP;

                byte[] initializationVector = new byte[16];
                for (int a = 0; a < 8; a++)
                {
                    initializationVector[a] = (byte)0xAA;
                    i++;
                    t_message[i] = initializationVector[a];
                }

                //                d_currentNonce = new byte[] { 0x88, 0x80, 0x64, 0xe5, 0xf6, 0xa2, 0xd2, 0xd9 };
                //                payload.message = new byte[] {0x62, 0x02};

                Array.Copy(d_currentNonce, 0, initializationVector, 8, 8);

                int sequence = 0;

                if (payload.part == 1)
                {
                    ++m_sequenceCounter;
                    sequence = m_sequenceCounter & (byte)0x0f;
                    sequence |= (byte)0x10;
                }
                else if (payload.part == 2)
                {
                    ++m_sequenceCounter;
                    sequence = m_sequenceCounter & (byte)0x0f;
                    sequence |= (byte)0x30;
                }

                byte[] plaintextmsg = new byte[payload.length + 1];
                plaintextmsg[0] = (byte)sequence;
                for (int a = 0; a < payload.length; a++)
                {
                    plaintextmsg[a + 1] = payload.message[a];
                }

                byte[] encryptedPayload = new byte[30];

                encryptedPayload = enc.OFB_EncryptMessage(encryptKey, initializationVector, plaintextmsg);

//                Utility.logMessage("authKey " + Utility.ByteArrayToString(authKey));
//                Utility.logMessage("EncryptKey " + Utility.ByteArrayToString(encryptKey));
                Utility.logMessage("Input Packet: " + Utility.ByteArrayToString(plaintextmsg));
//                Utility.logMessage("IV " + Utility.ByteArrayToString(initializationVector));
//                Utility.logMessage("encryptedPayload " + Utility.ByteArrayToString(encryptedPayload));

                if (1 == 0)
                {

                    //Nobody Messes with the IV
                    for (int a = 0; a < 8; a++)
                    {
                        initializationVector[a] = (byte)0xAA;
                    }

                    //                    for (int a = 0; a < 8; a++)
                    //                    {
                    //                        initializationVector[8 + a] = message[msg + a];
                    //                    }

                    Array.Copy(d_currentNonce, 0, initializationVector, 8, 8);

                    byte[] tmpoutput = new byte[16];

                    tmpoutput = enc.OFB_EncryptMessage(encryptKey, initializationVector, encryptedPayload);
                    Utility.logMessage("Encrypted Packet: " + Utility.ByteArrayToString(encryptedPayload));
                    Utility.logMessage("IV " + Utility.ByteArrayToString(initializationVector));
                    Utility.logMessage("EncryptKey " + Utility.ByteArrayToString(encryptKey));
                    Utility.logMessage("Decrypted " + Utility.ByteArrayToString(tmpoutput));
                }

                for (int a = 0; a < payload.length + 1; ++a)
                {
                    i++;
                    t_message[i] = encryptedPayload[a];
                }

                i++; t_message[i] = d_currentNonce[0];

                /*
                 * Nobody Messes with the IV
                for (int a = 0; a < 8; a++)
                {
                    initializationVector[a] = (byte)0xAA;
                }

                for (int a = 0; a < 8; a++)
                {
                    initializationVector[8 + a] = message[msg + a];
                }
                */

                //byte[] mac = new byte[8];
                //GenerateAuthentication
                int start = 1;
                byte[] mac = GenerateAuthentication(t_message, start, t_message.Length + 2 - start - 1, 0x01, node.Id, initializationVector, enc);
                for (int a = 0; a < 8; ++a)
                {
                    i++;
                    t_message[i] = mac[a];
                }

//                Utility.logMessage("Auth " + Utility.ByteArrayToString(mac));
//                Utility.logMessage("Outgoing " + Utility.ByteArrayToString(t_message));

                node.SendRequest(t_message);
                Utility.logMessage("In EncryptMessage - message sent");

                if ((networkKeySet == false) && payload.message[0] == 0x98 && payload.message[1] == 0x06)
                {
                    networkKeySet = true;
                    adding_node = false;
                    SetupNetworkKey(node);
                }

                return true;
            }
            return true;

        }

        private void ProcessNonceReport(byte[] message, int start)
        {
            if (d_currentNonce == null)
                d_currentNonce = new byte[8];
            Array.Copy(message, start + 1, d_currentNonce, 0, 8);
        }



        public bool DecryptMessage(ZWaveNode node, byte[] message, int start)
        {
            if (enc == null)
                init(node);

            //            byte[] message = new byte[] {0x01, 0x2c, 0x00, 0x04, 0x00, 0x0f, 0x26, 0x98, 0x81, 0x66, 0xba, 0xca, 0x63, 0xed, 0xff, 0x84, 0xf8, 0xff, 0x31, 0xbc, 0x1f, 0xd4, 0x2f, 0x39, 0x67, 0x30, 0x32, 0x0b, 0x2b, 0x6d, 0xfb, 0xe7, 0x18, 0x81, 0xf5, 0x68, 0xaa, 0xd3, 0xff, 0xe7, 0xaa, 0xdd, 0xeb, 0xec, 0x97, 0xe5};
            Utility.logMessage("In DecryptMessage - SecurityHandler");
            if (c_nonceTimer.ElapsedMilliseconds > 10000)
            {
                Utility.logMessage("Received the nonce  too late'" + c_nonceTimer.ElapsedMilliseconds + "' > 10000");
                //sendRequestNonce();
                return false;
            }


            //            Utility.logMessage("Message to be decrypted: " + Utility.ByteArrayToString(message));

            byte[] iv = getVIFromPacket_inbound(message, start + 1);
            //            byte[] decryptpacket = new byte[32];
            //            Array.Clear(decryptpacket, 0, 32);
            //            int length = message[6];
            int _length = message.Length;
            int encryptedpackagesize = _length - 11 - 8; //19 + 11 + 8
            byte[] encryptedpacket = new byte[encryptedpackagesize];


            Array.Copy(message, 8 + start + 1, encryptedpacket, 0, encryptedpackagesize);
            /*for (int i = encryptedpackagesize; i < 32; i++) {
                encryptedpacket[i] = 0;
            }
            */

            byte[] decryptedpacket = enc.OFB_EncryptMessage(encryptKey, iv, encryptedpacket);
//            Utility.logMessage("Message          " + Utility.ByteArrayToString(message));
//            Utility.logMessage("IV               " + Utility.ByteArrayToString(iv));
//            Utility.logMessage("Encrypted Packet " + Utility.ByteArrayToString(encryptedpacket));
            Utility.logMessage("Decrypted Packet " + Utility.ByteArrayToString(decryptedpacket));

            byte[] mac = GenerateAuthentication(message, start, _length, node.Id, 0x01, iv, enc);

            byte[] e_mac = new byte[8];
            Array.Copy(message, start + 8 + encryptedpackagesize + 2, e_mac, 0, 8);
            //            if (!Array.Equals(mac, e_mac)) {
            if (!Enumerable.SequenceEqual(mac, e_mac))
            {
                Utility.logMessage("Computed mac " + Utility.ByteArrayToString(mac) + " does not match the provider mac " + Utility.ByteArrayToString(e_mac) + ". Dropping.");
                if (secure_payload.Count > 1)
                    sendRequestNonce(node);
                return false;
            }

            if (decryptedpacket[1] == (byte)CommandClass.Security && 1 == 0)
            {
                byte[] msg = new byte[decryptedpacket.Length - 1];
                Array.Copy(decryptedpacket, 1, msg, 0, msg.Length);

                Utility.logMessage("Processing Internally: " + Utility.ByteArrayToString(msg));

//                HandleBasicMessage(msg, msg[1]);
                //HandleBasicReport(msg);
                //                Utility.logMessage("---------------------------HandleBasicReport-------------------------------");
            }
            else
            {
                byte[] msg = new byte[decryptedpacket.Length - 2 + 8];
                Array.Clear(msg, 0, 7);
                Array.Copy(decryptedpacket, 1, msg, 7, msg.Length - 7);

                msg[6] = (byte)(msg.Length - 7);

                Utility.logMessage("Forwarding: " + Utility.ByteArrayToString(msg));

                /* send to the Command Class for Proecssing */
                Utility.logMessage("Received External Command Class: " + Utility.ByteArrayToString(new byte[] { decryptedpacket[1] }));
                node.MessageRequestHandler(node.pController, msg);
            }
            Utility.logMessage("In DecryptMessage - Finished");

            return true;
        }

        private byte[] getVIFromPacket_inbound(byte[] message, int start)
        {
            byte[] iv = new byte[16];

            Array.Copy(message, start, iv, 0, 8);
            Array.Copy(c_currentNonce, 0, iv, 8, 8);

            return iv;
        }

        private byte[] GenerateAuthentication(byte[] data, int start, int length, byte sendingNode, byte receivingNode, byte[] iv, AES_work enc)
        {
            // data should stat at 4
            byte[] buffer = new byte[256];
            byte[] tmpauth = new byte[16];
            int ib = 0,
                it = 0;
            buffer[ib] = data[start + 0];
            ib++; buffer[ib] = sendingNode;
            ib++; buffer[ib] = receivingNode;
            ib++; buffer[ib] = (byte)(length - 19);
            Array.Copy(data, start + 9, buffer, 4, buffer[3]);

            byte[] buff = new byte[length - 19 + 4];
            Array.Copy(buffer, buff, length - 19 + 4);
//            Utility.logMessage("Raw Auth (minus IV)" + Utility.ByteArrayToString(buff));

            // tmpauth = enc.ECB_EncryptMessage(encryptKey, iv);
            tmpauth = enc.ECB_EncryptMessage(authKey, iv);

            byte[] encpck = new byte[16];
            Array.Clear(encpck, 0, 16);

            int block = 0;

            for (int i = 0; i < buff.Length; i++)
            {
                encpck[block] = buff[i];
                block++;

                if (block == 16)
                {
                    for (int j = 0; j < 16; j++)
                    {
                        tmpauth[j] = (byte)(encpck[j] ^ tmpauth[j]);
                        encpck[j] = 0;
                    }
                    block = 0;
                    tmpauth = enc.ECB_EncryptMessage(authKey, tmpauth);
                }
            }

            /* any left over data that isn't a full block size*/
            if (block > 0)
            {
                for (int i = 0; i < 16; i++)
                {
                    tmpauth[i] = (byte)(encpck[i] ^ tmpauth[i]);
                }
                tmpauth = enc.ECB_EncryptMessage(authKey, tmpauth);
            }

            byte[] auth = new byte[8];
            Array.Copy(tmpauth, auth, 8);
//            Utility.logMessage("Computed Auth " + Utility.ByteArrayToString(auth));

            return auth;
        }

    }
}
