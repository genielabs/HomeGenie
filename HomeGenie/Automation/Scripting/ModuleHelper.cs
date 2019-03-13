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

using HomeGenie.Data;
using HomeGenie.Service;

namespace HomeGenie.Automation.Scripting
{
    /// <summary>
    /// Module Helper class.\n
    /// This class is a module instance wrapper and it is used as return value of ModulesManager.Get() method.
    /// </summary>
    [Serializable]
    public class ModuleHelper : ModulesManager
    {
        private HomeGenie.Data.Module module = null;

        public ModuleHelper(HomeGenieService hg, Module module)
            : base(hg)
        {
            this.module = module;
        }

        public override TsList<HomeGenie.Data.Module> SelectedModules
        {
            get
            {
                var selectedModules = new TsList<Data.Module>();
                selectedModules.Add(module);
                return selectedModules;
            }
        }

        /// <summary>
        /// Determines whether this module has the given name.
        /// </summary>
        /// <returns><c>true</c> if this module has the given name; otherwise, <c>false</c>.</returns>
        /// <param name="name">Name.</param>
        public bool Is(string name)
        {
            return (module.Name.ToLower() == name.ToLower());
        }

        [Obsolete("Use 'Exists' instead")]
        public bool WasFound
        {
            get { return module != null; }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="HomeGenie.Automation.Scripting.ModuleHelper"/> has a valid module instance.
        /// </summary>
        /// <value><c>true</c> if module instance is valid; otherwise, <c>false</c>.</value>
        public bool Exists
        {
            get { return module != null; }
        }

        /// <summary>
        /// Determines whether this module belongs to the specified domain.
        /// </summary>
        /// <returns><c>true</c> if this module belongs to the specified domain; otherwise, <c>false</c>.</returns>
        /// <param name="domain">Domain.</param>
        public bool IsInDomain(string domain)
        {
            return module.Domain.ToLower() == domain.ToLower();
        }

        /// <summary>
        /// Gets the underlying module instance.
        /// </summary>
        /// <value>The instance.</value>
        public Module Instance
        {
            get { return module; }
        }

        /// <summary>
        /// Determines whether this module is in the specified groupList.
        /// </summary>
        /// <returns><c>true</c> if this instance is the specified groupList; otherwise, <c>false</c>.</returns>
        /// <param name="groupList">Comma separated group names.</param>
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

        /// <summary>
        /// Determines whether this module is of one of the types specified in typeList.
        /// </summary>
        /// <returns><c>true</c> if this module is of one of device types specified in typeList; otherwise, <c>false</c>.</returns>
        /// <param name="typeList">Comma seprated type list.</param>
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

        /// <summary>
        /// Determines whether this module has the specified feature active.
        /// </summary>
        /// <returns><c>true</c> if this module has the specified feature active; otherwise, <c>false</c>.</returns>
        /// <param name="feature">Feature.</param>
        public bool HasFeature(string feature)
        {
            var parameter = Service.Utility.ModuleParameterGet(module, feature);
            return (parameter != null && !String.IsNullOrWhiteSpace(parameter.Value));
        }

        /// <summary>
        /// Determines whether this module has the specified parameter.
        /// </summary>
        /// <returns><c>true</c> if this module has the specified parameter; otherwise, <c>false</c>.</returns>
        /// <param name="parameter">Parameter.</param>
        public bool HasParameter(string parameter)
        {
            return (Service.Utility.ModuleParameterGet(module, parameter) != null);
        }

        /// <summary>
        /// Gets the specified module parameter.
        /// </summary>
        /// <param name="parameter">Parameter.</param>
        public ModuleParameter Parameter(string parameter)
        {
            ModuleParameter value = null;
            if (this.module != null)
            {
                try
                {
                    value = Service.Utility.ModuleParameterGet(this.module, parameter);
                }
                catch { }
                // create parameter if does not exists
                if (value == null)
                {
                    value = Service.Utility.ModuleParameterSet(this.module, parameter, "");
                }
            }
            return value;
        }
                
        public StoreHelper Store(string storeName)
        {
            StoreHelper storage = null;
            if (this.module != null)
            {
                storage = new StoreHelper(this.module.Stores, storeName);
            }
            return storage;
        }

        /// <summary>
        /// Raise a module parameter event and set the parameter with the specified value. 
        /// </summary>
        /// <returns>ModuleHelper.</returns>
        /// <param name="parameter">Parameter name.</param>
        /// <param name="value">The new parameter value to set.</param>
        /// <param name="description">Event description.</param>
        public ModuleHelper RaiseEvent(string parameter, string value, string description)
        {
            try
            {
                var actionEvent = homegenie.MigService.GetEvent(
                    this.Instance.Domain,
                    this.Instance.Address,
                    description,
                    parameter,
                    value
                );
                homegenie.RaiseEvent(this, actionEvent);
            }
            catch (Exception ex)
            {
                HomeGenieService.LogError(
                    this.Instance.Domain,
                    this.Instance.Address,
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }
            return this;
        }
    }
}
