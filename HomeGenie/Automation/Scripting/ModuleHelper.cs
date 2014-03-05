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
        private HomeGenie.Data.Module module = null;

        public ModuleHelper(HomeGenieService hg, Module module)
            : base(hg)
        {
            this.module = module;
        }

        public override List<HomeGenie.Data.Module> SelectedModules
        {
            get
            {
                var selectedModules = new List<Data.Module>();
                selectedModules.Add(module);
                return selectedModules;
            }
        }

        public bool Is(string name)
        {
            return (module.Name.ToLower() == name.ToLower());
        }

        public bool WasFound
        {
            get { return module != null; }
        }

        public bool IsInDomain(string domain)
        {
            return module.Domain.ToLower() == domain.ToLower();
        }

        public Module Instance
        {
            get { return module; }
        }

        public bool IsInGroup(string groupList)
        {
            bool retval = false;
            var groups = GetArgumentsList(groupList);
            foreach (string group in groups)
            {
                var theGroup = homegenie.Groups.Find(z => z.Name.ToLower() == group.Trim().ToLower());
                if (theGroup != null)
                {
                    for (int m = 0; m < theGroup.Modules.Count; m++)
                    {
                        if (module.Domain == theGroup.Modules[m].Domain && module.Address == theGroup.Modules[m].Address)
                        {
                            retval = true;
                            break;
                        }
                    }
                }
            }
            return retval;
        }

        public bool IsOfDeviceType(string typeList)
        {
            bool retval = false;
            var types = ModulesManager.GetArgumentsList(typeList);
            foreach (var t in types)
            {
                if (t.ToLower() == module.DeviceType.ToString().ToLower())
                {
                    retval = true;
                    break;
                }
            }
            return retval;
        }

        public bool HasFeature(string feature)
        {
            var parameter = Service.Utility.ModuleParameterGet(module, feature);
            return (parameter != null && parameter.Value != null && parameter.Value != "");
        }

        public bool HasParameter(string parameter)
        {
            return (Service.Utility.ModuleParameterGet(module, parameter) != null);
        }


        public ModuleParameter Parameter(string parameter)
        {
            ModuleParameter value = null;
            if (SelectedModules.Count > 0)
            {
                try
                {
                    value = Service.Utility.ModuleParameterGet(module, parameter);
                }
                catch { }
                // create parameter if does not exists
                if (value == null)
                {
                    value = Service.Utility.ModuleParameterSet(module, parameter, "");
                }
            }
            return value;
        }


    }
}
