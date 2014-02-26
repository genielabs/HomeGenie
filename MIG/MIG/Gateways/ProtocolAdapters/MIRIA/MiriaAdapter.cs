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

namespace MIG.Gateways.ProtocolAdapters.MIRIA
{
    /// <summary>
    /// This class is meant to be used in new MIRIA service 
    /// that will be implemented by http://miria.codeplex.com
    /// It will replace old MIG ClientLibrary MIGLib.dll implementing
    /// same functionalities:
    /// http://mono-mig.svn.sourceforge.net/viewvc/mono-mig/MultiInputGateway/ClientLibraries/Mono/MIGlib/Client/MIGClient.cs?revision=84&view=markup
    /// http://mono-mig.svn.sourceforge.net/viewvc/mono-mig/MultiInputGateway/ClientLibraries/Mono/MIGlib/Client/Devices/Multitouch/MultitouchTuio.cs?revision=84&view=markup
    /// http://mono-mig.svn.sourceforge.net/viewvc/mono-mig/MultiInputGateway/ClientLibraries/Mono/MIGlib/Client/Devices/Wii/Remote/Remote.cs?revision=84&view=markup
    /// http://mono-mig.svn.sourceforge.net/viewvc/mono-mig/MultiInputGateway/ClientLibraries/Mono/MIGlib/Client/Devices/NiteKinect/Kinect.cs?revision=84&view=markup
    /// http://mono-mig.svn.sourceforge.net/viewvc/mono-mig/MultiInputGateway/OutputPlugins/Silverlight/Silverlight/
    /// </summary>
    class MiriaAdapter
    {



        /*
        private void _tcpgateway_ProcessRequest(int clientid, byte[] data)
        {
            System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
            string cmdline = enc.GetString(data).Trim(new char[] { '\0', ' ' });
            if (cmdline == "")
            {
                // _server.DisconnectClient(args.ClientId);
                return;
            }
            else if (cmdline.StartsWith("wiiremote "))
            {
                // send message to wiiremote input plugin
                //_host.GetInputPlugin("wiiremote").ProcessCLI(cmdline);
            }
        }
        */

    }
}
