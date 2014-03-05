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

using HomeGenie.Automation.Scheduler;
using HomeGenie.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HomeGenie.Automation.Scripting
{
    public class SchedulerHelper
    {

        private HomeGenieService homegenie;
        private string scheduleName;

        public SchedulerHelper(HomeGenieService hg)
        {
            homegenie = hg;
        }

        public SchedulerHelper WithName(string name)
        {
            scheduleName = name;
            return this;
        }

        public SchedulerHelper SetSchedule(string cronExpression)
        {
            homegenie.ProgramEngine.SchedulerService.AddOrUpdate(scheduleName, cronExpression);
            return this;
        }

        public SchedulerHelper SetProgram(string programId)
        {
            homegenie.ProgramEngine.SchedulerService.SetProgram(scheduleName, programId);
            return this;
        }

        public bool IsScheduling()
        {
            var eventItem = homegenie.ProgramEngine.SchedulerService.Get(scheduleName);
            if (eventItem != null)
            {
                return homegenie.ProgramEngine.SchedulerService.IsScheduling(eventItem.CronExpression);
            }
            return false;
        }

        public bool IsScheduling(string cronExpression)
        {
            return homegenie.ProgramEngine.SchedulerService.IsScheduling(cronExpression);
        }

    }
}
