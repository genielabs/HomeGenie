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

using HomeGenie.Data;
using MIG;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HomeGenie.Service.Handlers
{
    public class Interconnection
    {
        private HomeGenieService _hg;
        public Interconnection(HomeGenieService hg)
        {
            _hg = hg;
        }

        public void ProcessRequest(MIGClientRequest request, MIGInterfaceCommand migcmd)
        {
            switch (migcmd.command)
            {
                case "Events.Push":
                    //TODO: implemet security and trust mechanism 
                    string objstream = new StreamReader(request.InputStream).ReadToEnd();
                    ModuleEvent mev = JsonConvert.DeserializeObject<ModuleEvent>(objstream);
                    //
                    Module mod = _hg.Modules.Find(delegate(Module o)
                    {
                        return o.Domain == mev.Module.Domain && o.Address == mev.Module.Address;
                    });
                    if (mod == null)
                    {
                        mod = mev.Module;
                        _hg.Modules.Add(mod);
                    }
                    else
                    {
                        Utility.ModuleParameterSet(mod, mev.Parameter.Name, mev.Parameter.Value);
                    }
                    // "<ip>:<port>" remote endpoint port is passed as the first argument from the remote point itself
                    mod.RoutingNode = request.RequestOrigin + (migcmd.GetOption(0) != "" ? ":" + migcmd.GetOption(0) : "");
                    //
                    _hg.LogBroadcastEvent(mev.Module.Domain, mev.Module.Address, request.RequestOrigin, mev.Parameter.Name, mev.Parameter.Value);
                    _hg.RouteParameterChangedEvent(request.RequestOrigin, mod, mev.Parameter);
                    break;
            }
        }
    }

}
