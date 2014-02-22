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

using HomeGenie.Data;
using HomeGenie.Service;

namespace HomeGenie.Automation.Scripting
{
    public class EventsHelper
    {
        HomeGenieService _homegenie = null;
        int _myprogramid = -1;

        public EventsHelper(HomeGenieService hg, int programid)
        {
            _homegenie = hg;
            _myprogramid = programid;
        }

        public EventsHelper ModuleParameterIsChanging(Func<ModuleHelper, ModuleParameter, bool> handler)
        {
            ProgramBlock pb = _homegenie.ProgramEngine.Programs.Find(p => p.Address.ToString() == _myprogramid.ToString());
            pb.ModuleIsChangingHandler = handler;
            return this;

        }

        public EventsHelper ModuleParameterChange(Func<ModuleHelper, ModuleParameter, bool> handler)
        {
            ProgramBlock pb = _homegenie.ProgramEngine.Programs.Find(p => p.Address.ToString() == _myprogramid.ToString());
            pb.ModuleChangedHandler = handler;
            return this;
            
        }

        public EventsHelper WebServiceCallReceived(string apicall, Func<object, string> handler)
        {
            ProgramBlock pb = _homegenie.ProgramEngine.Programs.Find(p => p.Address.ToString() == _myprogramid.ToString());
            pb._registeredapicalls.Add(apicall);
            _homegenie.RegisterDynamicApi(apicall, handler);
            return this;
        }

        public EventsHelper WebServiceCallReceived(string apicall, Func<object, object> handler)
        {
            ProgramBlock pb = _homegenie.ProgramEngine.Programs.Find(p => p.Address.ToString() == _myprogramid.ToString());
            pb._registeredapicalls.Add(apicall);
            _homegenie.RegisterDynamicApi(apicall, handler);
            return this;
        }



    }
}
