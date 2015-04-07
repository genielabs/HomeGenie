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

using ZWaveLib.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZWaveLib.Devices;

namespace ZWaveLib.CommandClasses
{

    public class Security : ICommandClass
    {
        private ZWaveNode hostNode = null;
        private bool schemeagreed = false;
       
        public CommandClass GetClassId()
        {
            return CommandClass.Security;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            hostNode = node;
            bool handled = false;
            byte cmdType = 0xFF;

            if (message != null)
            {
                cmdType = message[1];
            }

            ZWaveEvent nodeEvent = null;

            handled = HandleBasicMessage(message, cmdType);
            
            return nodeEvent;
        }

        private bool HandleBasicMessage(byte[] message, byte cmdType) {
            bool handled = false;
            int start = 1;
            switch (cmdType)
            {
                case 0xFF:

                    // Send COMMAND_SCHEME_GET to start the secure device inclusion
                    hostNode.security.getScheme(hostNode);

                    handled = true;
                    break;
                case (byte)SecurityCommand.NonceGet:
                    Utility.logMessage("Received COMMAND_NONCE_GET for node: " + hostNode.Id);

                    /* the Device wants to send us a Encrypted Packet, and thus requesting for our latest NONCE */
                    hostNode.security.SendNonceReport(hostNode);

                    handled = true;
                    break;
                case (byte)SecurityCommand.MessageEncap:
                    Utility.logMessage("Received COMMAND_MESSAGE_ENCAP for node: " + hostNode.Id);

                    /* We recieved a Encrypted single packet from the Device. Decrypt it. */
                    hostNode.security.DecryptMessage(hostNode, message, start);

                    handled = true;
                    break;
                case (byte)SecurityCommand.NonceReport:
                    Utility.logMessage("Received COMMAND_NONCE_REPORT for node: " + hostNode.Id);

                    /* we recieved a NONCE from a device, so assume that there is something in a queue to send out */
                    hostNode.security.ProcessNonceReport(hostNode, message, start);

                    handled = true;
                    break;
                case (byte)SecurityCommand.MessageEncapNonceGet:
                    Utility.logMessage("Received COMMAND_MESSAGE_ENCAP_NONCE_GET for node: " + hostNode.Id);

                    /* we recieved a encrypted packet from the device, and the device is also asking us to send a
			         * new NONCE to it, hence there must be multiple packets.*/
                    hostNode.security.DecryptMessage(hostNode, message, start);

                    /* Regardless of the success/failure of Decrypting, send a new NONCE */
                    hostNode.security.SendNonceReport(hostNode);

                    handled = true;
                    break;
                case (byte)SecurityCommand.NetworkKeySet:
                    Utility.logMessage("Received COMMAND_NETWORK_KEY_SET for node: " + hostNode.Id + ", " + (start + 1));

                    /* we shouldn't get a NetworkKeySet from a node if we are the controller
			         * as we send it out to the Devices
			        */

                    handled = true;
                    break;
                case (byte)SecurityCommand.NetworkKeyVerify:
                    Utility.logMessage("Received COMMAND_NETWORK_KEY_VERIFY for node: " + hostNode.Id + ", " + (start + 1));

                    /*
                     * if we can decrypt this packet, then we are assured that our NetworkKeySet is successfull
			         * and thus should set the Flag referenced in SecurityCmd_SchemeReport
			        */
                    byte[] msg = ZWaveMessage.CreateRequest(hostNode.Id, new byte[] { 
                        (byte)CommandClass.Security,
                        (byte)SecurityCommand.SupportedGet
                    });

                    hostNode.security.encryptAndSend(hostNode, msg);

                    handled = true;
                    break;
                case (byte)SecurityCommand.SupportedReport:
                    Utility.logMessage("Received COMMAND_SUPPORTED_REPORT for node: " + hostNode.Id);
                    /* this is a list of CommandClasses that should be Encrypted.
			         * and it might contain new command classes that were not present in the NodeInfoFrame
			         * so we have to run through, mark existing Command Classes as SetSecured (so SendMsg in the Driver
			         * class will route the unecrypted messages to our SendMsg) and for New Command
			         * Classes, create them, and of course, also do a SetSecured on them.
			         *
			         * This means we must do a SecurityCmd_SupportedGet request ASAP so we dont have
			         * Command Classes created after the Discovery Phase is completed!
			         */

                    hostNode.SetSecuredClasses(message);

                    handled = true;
                    break;
                case (byte)SecurityCommand.SchemeInherit:
                    Utility.logMessage("Received COMMAND_SCHEME_INHERIT for node: " + hostNode.Id);
                    /* only used in a Controller Replication Type enviroment. */

                    break;
                case (byte)SecurityCommand.SchemeReport:
                    Utility.logMessage("Received COMMAND_SCHEME_REPORT for node: " + hostNode.Id + ", " + (start + 1));
                    int schemes = message[start + 1];
                    if (schemeagreed)
                    {
                        handled = true;
                        break;
                    }

                    if (schemes == (byte)SecurityScheme.SchemeZero)
                    {

                        byte[] t_msg = new byte[18];
                        t_msg[0] = (byte)CommandClass.Security;
                        t_msg[1] = (byte)SecurityCommand.NetworkKeySet;

                        Array.Copy(hostNode.security.PrivateNetworkKey, 0, t_msg, 2, 16);
                        byte[] f_msg = ZWaveMessage.CreateRequest(hostNode.Id, t_msg);
                        hostNode.security.encryptAndSend(hostNode, f_msg);

                        schemeagreed = true;
                    }
                    else
                    {
                        Utility.logMessage("   No common security scheme.  The device will continue as an unsecured node.");
                    }

                    handled = true;
                    break;
                default:
                    Utility.logMessage("Unknown security Command " + (byte)cmdType + " - message " + message);
                    break;
            }

            return handled;
        }

    
    }
}

