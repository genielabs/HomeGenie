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
using CronExpressionDescriptor;
using Newtonsoft.Json;
using System.Xml.Serialization;
using HomeGenie.Data;

namespace HomeGenie.Automation.Scheduler
{
    /// <summary>
    /// Scheduler item.
    /// </summary>
    [Serializable()]
    public class SchedulerItem
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the cron expression.
        /// </summary>
        /// <value>The cron expression.</value>
        public string CronExpression { get; set; }
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public string Data { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is enabled.
        /// </summary>
        /// <value><c>true</c> if this instance is enabled; otherwise, <c>false</c>.</value>
        public bool IsEnabled { get; set; }
        /// <summary>
        /// Gets or sets the script.
        /// </summary>
        /// <value>The script.</value>
        public string Script { get; set; }
        /// <summary>
        /// Gets or sets the bound devices.
        /// </summary>
        /// <value>The bound devices.</value>
        public List<string> BoundDevices { get; set; }
        /// <summary>
        /// Gets or sets the bound modules.
        /// </summary>
        /// <value>The bound modules.</value>
        public List<ModuleReference> BoundModules { get; set; }

        // TODO: deprecate the following two
        public string LastOccurrence { get; set; }
        public string NextOccurrence { get; set; }

        // TODO: deprecate this field - left for compatibility with hg <= r521
        public string ProgramId { get; set; }

        [XmlIgnore,JsonIgnore]
        public SchedulerScriptingEngine ScriptEngine { get; set; }

        public SchedulerItem()
        {
            Name = "";
            CronExpression = "";
            ProgramId = "";
            IsEnabled = false;
            LastOccurrence = "-";
            NextOccurrence = "-";
            BoundDevices = new List<string>();
            BoundModules = new List<ModuleReference>();
        }
    }
}
