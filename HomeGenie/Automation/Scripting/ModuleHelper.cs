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
    public class ModuleHelper : ModulesManager
    {
        private HomeGenie.Data.Module _module = null;


        public ModuleHelper(HomeGenieService hg, Module module)
            : base(hg)
        {
            this._module = module;
        }

        public override List<HomeGenie.Data.Module> SelectedModules
        {
            get
            {
                List<Data.Module> ls = new List<Data.Module>();
                ls.Add(_module);
                return ls;
            }
        }
        public bool Is(string name)
        {
            return (_module.Name.ToLower() == name.ToLower());
        }
        public bool WasFound
        {
            get { return _module != null; }
        }

        public bool IsInDomain(string domain)
        {
            return _module.Domain.ToLower() == domain.ToLower();
        }

        public Module Instance
        {
            get { return _module; }
        }



        public bool IsInGroup(string ingroup)
        {
            bool retval = false;
            List<string> groups = _getArgumentsList(ingroup);
            foreach (string grp in groups)
            {
                Group theGroup = _homegenie.Groups.Find(z => z.Name.ToLower() == grp.Trim().ToLower());
                if (theGroup != null)
                {
                    for (int m = 0; m < theGroup.Modules.Count; m++)
                    {
                        if (_module.Domain == theGroup.Modules[m].Domain && _module.Address == theGroup.Modules[m].Address)
                        {
                            retval = true;
                            break;
                        }
                    }
                }
            }
            return retval;
        }
        public bool IsOfDeviceType(string type)
        {
            bool retval = false;
            List<string> types = ModulesManager._getArgumentsList(type);
            foreach (var t in types)
            {
                if (t.ToLower() == _module.DeviceType.ToString().ToLower())
                {
                    retval = true;
                    break;
                }
            }
            return retval;
        }







        public bool HasFeature(string feature)
        {
            ModuleParameter f = Service.Utility.ModuleParameterGet(_module, feature);
            return (f != null && f.Value != null && f.Value != "");
        }

        public bool HasParameter(string parameter)
        {
            return (Service.Utility.ModuleParameterGet(_module, parameter) != null);
        }


        public ModuleParameter Parameter(string parameter)
        {
            ModuleParameter value = null;
            if (SelectedModules.Count > 0)
            {
                try
                {
                    value = Service.Utility.ModuleParameterGet(_module, parameter);
                }
                catch { }
                // create parameter if does not exists
                if (value == null)
                {
                    value = Service.Utility.ModuleParameterSet(_module, parameter, "");
                }
            }
            return value;
        }


    }
}
