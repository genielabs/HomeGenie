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
    [Serializable()]
    public class SchedulerItem
    {
        public string Name { get; set; }
        public string CronExpression { get; set; }
        public string Description { get; set; }
        public string Data { get; set; }
        public bool IsEnabled { get; set; }
        public string Script { get; set; }
        public List<string> BoundDevices { get; set; }
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
