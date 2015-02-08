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
using System.Linq;
using System.Text;
using ZWaveLib.Devices.Values;

namespace ZWaveLib.Devices.ProductHandlers.Generic
{
    public class Meter : IZWaveDeviceHandler
    {
        protected ZWaveNode nodeHost;

        public void SetNodeHost(ZWaveNode node)
        {
            nodeHost = node;
            nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterEvent.MeterWatt, 0);
        }

        public virtual bool CanHandleProduct(ManufacturerSpecific productspecs)
        {
            return false; // generic types must return false here
        }

        public virtual bool HandleRawMessageRequest(byte[] message)
        {
            return false;
        }

        public virtual bool HandleBasicReport(byte[] message)
        {
            bool processed = false;
            //MeterValue value = MeterValue.Parse(message);

            //
            byte commandClass = message[7];
            byte commandType = message[8];
            //

            processed = TryHandleMessage(commandClass, commandType, message);

            return processed;
        }

        


        /*
         * Important Note: When the command class at message[7] is METER and command at message[8] is METER_REPORT, it is NOT a multi-instance. It's a standard 
         * report. All multi-instance types will be identified as such at message[7]. A multi-instance/ENCAP will store it's encapsulated commandclass in 
         * message[11]. To avoid confusion, we should handle METER classes as standard reports.
         * */

        /*
        public virtual bool HandleMultiInstanceReport(byte[] message)
        {
            //UNHANDLED: 01 14 00 04 08 04 0E 32 02 21 74 00 00 1E BB 00 00 00 00 00 00 2D
            //                       ^^        |  |
            //                                 +--|------> 0x31 Command Class Meter
            //                                    +------> 0x02 Meter Report
            bool processed = false;
            //
            byte commandClass = message[7];
            byte commandType = message[8];
            //
            if (commandClass == (byte)CommandClass.Meter && commandType == (byte)Command.MeterReport)
            {
                // TODO: should check meter report type (Electric, Gas, Water) and value precision scale
                // TODO: the code below parse always as Electric type
                EnergyValue energy = EnergyValue.Parse(message);
                nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, energy.EventType, energy.Value);
                processed = true;
            }
            return processed;
        }
        */


        public virtual bool HandleMultiInstanceReport(byte[] message)
        {
            //UNHANDLED: 01 14 00 04 08 04 0E 32 02 21 74 00 00 1E BB 00 00 00 00 00 00 2D
            //                       ^^        |  |
            //                                 +--|------> 0x31 Command Class Meter
            //                                    +------> 0x02 Meter Report

            //
            byte commandClass = message[7];
            byte commandType = message[8];
            //
            if (commandClass == (byte)CommandClass.MultiInstance)
            {
                // v1 ENCAP: Uses instance;
                // v2 ENCAP: uses Start/End POint.

                // NOTE: MultiChannel is the name for the v2 of MutilInstance per SPEC.
                if (commandType == (byte)Command.MultiInstaceV2Encapsulated)
                {
                    //dataStart = 15;
                    byte sourceEndPoint = message[9];
                    byte destEndPoint = message[10];

                    /* We now have the ENCAP part of the frame. Next step is to determine format of inner frame 
                    * based on cmd being called.
                    * */
                    byte encappedCmdClass = message[11];
                    byte encappedCmd = message[12];

                    return TryHandleMultiInstanceMessage(encappedCmdClass, encappedCmd, message, sourceEndPoint);

                }
                else if (commandType == (byte)Command.MultiInstanceReport) //MultiInstanceCmd_Encap
                {
                    // Instance only used for MULTIINSTANCE (v1).
                    byte instance = message[9];
                    byte encappedCmdClass = message[10];
                    byte encappedCmd = message[11];

                    return TryHandleMultiInstanceMessage(encappedCmdClass, encappedCmd, message, instance);
                }

            }
            else
            {
                // TODO: COMMAND was not a METER, so ERROR or not handled!
            }

            #region "Doc"

            /* For reference, here are the actual command classes from SPEC:
             * 
             * MultiInstance is used for v1.
             * MultiChannel is used for v2 and is backward compatible with MultiInstance.
             * 
        MultiInstanceCmd_Get                = 0x04,
        MultiInstanceCmd_Report             = 0x05,
        MultiInstanceCmd_Encap              = 0x06,
    
        // Version 2
        MultiChannelCmd_EndPointGet         = 0x07,
        MultiChannelCmd_EndPointReport          = 0x08,
        MultiChannelCmd_CapabilityGet           = 0x09,
        MultiChannelCmd_CapabilityReport        = 0x0a,
        MultiChannelCmd_EndPointFind            = 0x0b,
        MultiChannelCmd_EndPointFindReport      = 0x0c,
        MultiChannelCmd_Encap               = 0x0d,
             * */



            //
            // QUESTION: What if MULTICHANNEL comm class is NOT encapped??? 
            // ANSWER:   We only support: MULTI_INSTANCE_REPORT and ENCAPP. MULTI_INSTANCE_REPORT just lists all endpoints, so is useless here.
            //
            // MULTI_INSTANCE_REPORT:  The Multi Instance Report Command reports the number of instances of a given Command Class in a device.
            // + SubCmdClass[7-0]
            // + RESERVED[7] Instances[6-0]
            /*
                 Please be aware that the identifiers for the new Multi Channel command class is the same as the Multi
Instance command class and the new Multi Channel Association command class identifier is the same
as the Multi Instance Association command class. For this reason the two new command classes will
start with version 2. In this way this new command class will be backward compatible with any existing
products implementing the Multi Instance command class.
There are two exceptions to the backward capability:
1. The number of instances are changed from 255 in version 1 to 127 in version 2
2. Multi Instance devices cannot control Multi Channels where the end points are not identical
                 
                 This structure means any controller that needs to control a device that either implements Multi Channel
command class or Multi Channel Association command class MUST interview the device for the version
before using these classes. If the Version Command Class is not supported or version 1 is reported the
controller MUST use the Multi Instance and Multi Instance Association command classes. 
                 
                 * */
            #endregion

            return false;
        }

        public bool TryHandleMessage(byte cmdClass, byte cmd, byte[] message)
        {
            return TryHandleMultiInstanceMessage(cmdClass, cmd, message, 0);
        }

        public bool TryHandleMultiInstanceMessage(byte cmdClass, byte cmd, byte[] message, int instance)
        {
            bool processed = false;
            if (cmdClass == (byte)CommandClass.Meter)
            {
                if (cmd == (byte)Command.MeterReport)
                {
                    MeterValue value = MeterValue.Parse(message);
                    // RAISE: Previous Value
                    // RAISE: Time Delta
                    // ^^^^^ Should be combined into a single class.

                    nodeHost.RaiseUpdateParameterEvent(nodeHost, instance, value.EventType, value.Value);

                    // TODO: Below needs arg to indicate it is a previous value, not current.
                    //nodeHost.RaiseUpdateParameterEvent(nodeHost, instance, value.EventType, value.PreviousValue);
                    processed = true;
                }
                else
                {
                    // TODO: REPORT Error: CommandClass METER only supports Command METER_REPORT.
                }
            }
            else
            {
                // TODO: REPORT error: cmdClass not supported in MeterValue.
            }
            return processed;
        }

    }
}
