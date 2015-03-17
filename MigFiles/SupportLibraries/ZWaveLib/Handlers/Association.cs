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

        public byte GetCommandClassId()
        {
            return 0x85;
        }

        public ZWaveEvent GetEvent(ZWaveNode node, byte[] message)
        {
            ZWaveEvent nodeEvent = null;
            byte cmdType = message[1];
            if (message.Length > 5 && cmdType == (byte)Command.AssociationReport)
            {
                byte groupId = message[2];
                byte maxAssociations = message[3];
                byte numAssociations = message[4]; // it is always zero ?!?
                string assocNodes = "";
                if (message.Length > 4)
                {
                    for (int a = 5; a < message.Length; a++)
                    {
                        assocNodes += message[a] + ",";
                    }
                }
                assocNodes = assocNodes.TrimEnd(',');
                //
                var associationRespose = new AssociationResponse() {
                    Max = maxAssociations,
                    Count = numAssociations,
                    NodeList = assocNodes,
                    GroupId = groupId
                };
                nodeEvent = new ZWaveEvent(node, EventParameter.Association, associationRespose, 0);
            }
            return nodeEvent;
        }

        public static void Set(ZWaveNode node, byte groupid, byte targetnodeid)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.Association, 
                (byte)Command.AssociationSet, 
                groupid, 
                targetnodeid 
            });
        }

        public static void Get(ZWaveNode node, byte groupid)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.Association, 
                (byte)Command.AssociationGet, 
                groupid 
            });
        }

        public static void Remove(ZWaveNode node, byte groupid, byte targetnodeid)
        {
            node.SendRequest(new byte[] { 
                (byte)CommandClass.Association, 
                (byte)Command.AssociationRemove, 
                groupid, 
                targetnodeid 
            });
        }

    }
}

