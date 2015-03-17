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

namespace ZWaveLib.Handlers
{
    public class Association : ICommandClass
    {
        public class AssociationResponse
        {
            public byte Max = 0;
            public byte Count = 0;
            public byte GroupId = 0;
            public string NodeList = "";
        }

        public CommandClassType GetCommandClassId()
        {
            return CommandClassType.Association;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;
            byte cmdType = message[1];
            if (message.Length > 5 && cmdType == (byte)CommandType.AssociationReport)
            {
                byte groupId = message[2];
                byte associationMax = message[3];
                byte associationCount = message[4]; // it is always zero ?!?
                string associationNodes = "";
                if (message.Length > 4)
                {
                    for (int a = 5; a < message.Length; a++)
                    {
                        associationNodes += message[a] + ",";
                    }
                }
                associationNodes = associationNodes.TrimEnd(',');
                //
                var associationResponse = new AssociationResponse() {
                    Max = associationMax,
                    Count = associationCount,
                    NodeList = associationNodes,
                    GroupId = groupId
                };
                nodeEvent = new ZWaveEvent(node, EventParameter.Association, associationResponse, 0);
            }
            return nodeEvent;
        }

        public static void Set(ZWaveNode node, byte groupid, byte targetNodeId)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClassType.Association, 
                (byte)CommandType.AssociationSet, 
                groupid, 
                targetNodeId 
            });
        }

        public static void Get(ZWaveNode node, byte groupId)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClassType.Association, 
                (byte)CommandType.AssociationGet, 
                groupId 
            });
        }

        public static void Remove(ZWaveNode node, byte groupId, byte targetNodeId)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClassType.Association, 
                (byte)CommandType.AssociationRemove, 
                groupId, 
                targetNodeId 
            });
        }

    }
}

