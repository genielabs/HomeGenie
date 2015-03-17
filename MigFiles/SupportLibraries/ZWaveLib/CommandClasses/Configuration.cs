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
using System.Threading;

namespace ZWaveLib.CommandClasses
{
    public class Configuration : ICommandClass
    {
        public CommandClass GetClassId()
        {
            return CommandClass.Configuration;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;
            byte cmdType = message[1];
            if (message.Length > 4 && cmdType == (byte)Command.ConfigurationReport)
            {
                byte paramId = message[2];
                byte paramLength = message[3];
                //
                var nodeConfigParamsLength = GetConfigParamsData(node);
                if (!nodeConfigParamsLength.ContainsKey(paramId))
                {
                    nodeConfigParamsLength.Add(paramId, paramLength);
                }
                else
                {
                    // this shouldn't change on read... but you never know! =)
                    nodeConfigParamsLength[paramId] = paramLength;
                }
                //
                byte[] bval = new byte[4];
                // extract bytes value
                Array.Copy(message, 4, bval, 4 - (int)paramLength, (int)paramLength);
                uint paramValue = bval[0];
                Array.Reverse(bval);
                // convert it to uint
                paramValue = BitConverter.ToUInt32(bval, 0);
                nodeEvent = new ZWaveEvent(node, EventParameter.Configuration, paramValue, paramId);
            }
            return nodeEvent;
        }

        public static void Set(ZWaveNode node, byte parameter, Int32 paramValue)
        {
            int valueLength = 1;
            var nodeConfigParamsLength = GetConfigParamsData(node);
            if (!nodeConfigParamsLength.ContainsKey(parameter))
            {
                Get(node, parameter);
                int retries = 0;
                while (!nodeConfigParamsLength.ContainsKey(parameter) && retries++ <= 5)
                {
                    Thread.Sleep(1000);
                }
            }
            if (nodeConfigParamsLength.ContainsKey(parameter))
            {
                valueLength = nodeConfigParamsLength[parameter];
            }
            //
            byte[] value32 = BitConverter.GetBytes(paramValue);
            Array.Reverse(value32);
            //
            byte[] msg = new byte[4 + valueLength];
            msg[0] = (byte)CommandClass.Configuration;
            msg[1] = (byte)Command.ConfigurationSet;
            msg[2] = parameter;
            msg[3] = (byte)valueLength;
            switch (valueLength)
            {
                case 1:
                Array.Copy(value32, 3, msg, 4, 1);
                break;
                case 2:
                Array.Copy(value32, 2, msg, 4, 2);
                break;
                case 4:
                Array.Copy(value32, 0, msg, 4, 4);
                break;
            }
            node.SendRequest(msg);
        }

        public static void Get(ZWaveNode node, byte parameter)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.Configuration, 
                (byte)Command.ConfigurationGet,
                parameter
            });
        }

        private static Dictionary<byte, int> GetConfigParamsData(ZWaveNode node)
        {
            if (!node.Data.ContainsKey("ConfigParamsLength"))
            {
                node.Data.Add("ConfigParamsLength", new Dictionary<byte, int>());
            }
            return (Dictionary<byte, int>)node.Data["ConfigParamsLength"];
        }

    }
}

