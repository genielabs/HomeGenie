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

namespace HomeGenie.Automation.Scheduler
{
    [Serializable()]
    public class SchedulerItem
    {
        public string Name { get; set; }
        public string CronExpression { get; set; }
        public string Description 
        {
            get 
            { 
                string d = "";
                try
                {
                    d = ExpressionDescriptor.GetDescription(this.CronExpression);
                    d = Char.ToLowerInvariant(d[0]) + d.Substring(1);
                }
                catch { }
                return d;
            } 
        }
        public string ProgramId { get; set; }
        public bool IsEnabled { get; set; }
        public string LastOccurrence { get; set; }
        public string NextOccurrence { get; set; }

        public SchedulerItem()
        {
            Name = "";
            CronExpression = "";
            ProgramId = "";
            IsEnabled = false;
            LastOccurrence = "-";
            NextOccurrence = "-";
        }
    }
}
