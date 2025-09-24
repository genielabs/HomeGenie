/*
   Copyright 2012-2025 G-Labs (https://github.com/genielabs)

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: https://homegenie.it
 */

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

using Newtonsoft.Json;

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

        [XmlIgnore, JsonIgnore]
        public string LastOccurrence { get; set; }

        [XmlIgnore, JsonIgnore]
        public SchedulerScriptingEngine ScriptEngine { get; set; }

        public SchedulerItem()
        {
            Name = "";
            CronExpression = "";
            IsEnabled = false;
            LastOccurrence = "";
            BoundDevices = new List<string>();
            BoundModules = new List<ModuleReference>();
        }
    }
}
