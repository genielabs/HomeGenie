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

using HomeGenie.Service;
using HomeGenie.Data;

namespace HomeGenie.Automation.Scripting
{

    /// <summary>
    /// Settings helper.\n
    /// Class instance accessor: **Settings**
    /// </summary>
    [Serializable]
    public class SettingsHelper
    {
        private HomeGenieService homegenie;

        public SettingsHelper(HomeGenieService hg)
        {
            homegenie = hg;
        }

        /// <summary>
        /// Gets the system settings parameter with the specified name.
        /// </summary>
        /// <param name="parameter">Parameter.</param>
        public ModuleParameter Parameter(string parameter)
        {
            var systemParameter = homegenie.Parameters.Find(mp => mp.Name == parameter);
            // create parameter if does not exists
            if (systemParameter == null)
            {
                systemParameter = new ModuleParameter() { Name = parameter };
                homegenie.Parameters.Add(systemParameter);
            }

            return systemParameter;
        }
    }
}

