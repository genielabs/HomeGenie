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
using System.Threading;
using System.Net;

namespace HomeGenie.Service.Handlers
{
    public class Interconnection
    {
        private HomeGenieService homegenie;

        public Interconnection(HomeGenieService hg)
        {
            homegenie = hg;
        }

        public void ProcessRequest(MigClientRequest request)
        {
            var context = request.Context.Data as HttpListenerContext;
            var requestOrigin = context.Request.RemoteEndPoint.Address.ToString();

            var migCommand = request.Command;
            switch (migCommand.Command)
            {
            case "Events.Push":
                //TODO: implemet security and trust mechanism 
                var stream = request.RequestText;
                var moduleEvent = JsonConvert.DeserializeObject<ModuleEvent>(
                    stream,
                    new JsonSerializerSettings(){ Culture = System.Globalization.CultureInfo.InvariantCulture }
                );
                // prefix remote event domain with HGIC:<remote_node_address>.<domain>
                moduleEvent.Module.Domain = "HGIC:" + requestOrigin.Replace(".", "_") + "." + moduleEvent.Module.Domain;
                //
                var module = homegenie.Modules.Find(delegate(Module o) {
                    return o.Domain == moduleEvent.Module.Domain && o.Address == moduleEvent.Module.Address;
                });
                if (module == null)
                {
                    module = moduleEvent.Module;
                    homegenie.Modules.Add(module);
                }
                Utility.ModuleParameterSet(module, moduleEvent.Parameter.Name, moduleEvent.Parameter.Value);
                // "<ip>:<port>" remote endpoint port is passed as the first argument from the remote point itself
                module.RoutingNode = requestOrigin + (migCommand.GetOption(0) != "" ? ":" + migCommand.GetOption(0) : "");
                //
                homegenie.RaiseEvent(
                    moduleEvent.Module.Domain,
                    moduleEvent.Module.Address,
                    requestOrigin,
                    moduleEvent.Parameter.Name,
                    moduleEvent.Parameter.Value
                );
                HomeGenieService.RoutedEvent eventData = new HomeGenieService.RoutedEvent() {
                    Sender = requestOrigin,
                    Module = module,
                    Parameter = moduleEvent.Parameter
                };
                ThreadPool.QueueUserWorkItem(new WaitCallback(homegenie.RouteParameterChangedEvent), eventData);
                break;
            }
        }
    }

}
