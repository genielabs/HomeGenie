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
    
    /// <summary>
    /// Scheduler helper.\n
    /// Class instance accessor: **Scheduler**
    /// </summary>
    [Serializable]
    public class SchedulerHelper
    {

        private HomeGenieService homegenie;
        private string scheduleName;

        public SchedulerHelper(HomeGenieService hg)
        {
            homegenie = hg;
        }

        /// <summary>
        /// Select the schedule with the specified name.
        /// </summary>
        /// <param name="name">Name.</param>
        public SchedulerHelper WithName(string name)
        {
            scheduleName = name;
            return this;
        }

        /// <summary>
        /// Add/Modify the schedule with the previously selected name.
        /// </summary>
        /// <param name="cronExpression">Cron expression.</param>
        public SchedulerHelper SetSchedule(string cronExpression)
        {
            homegenie.ProgramManager.SchedulerService.AddOrUpdate(scheduleName, cronExpression);
            return this;
        }

        /// <summary>
        /// Sets the program id to run when the selected schedule occurs.
        /// </summary>
        /// <param name="programId">Program ID.</param>
        public SchedulerHelper SetProgram(string programId)
        {
            homegenie.ProgramManager.SchedulerService.SetProgram(scheduleName, programId);
            return this;
        }

        /// <summary>
        /// Determines whether the selected schedule is matching in this very moment.
        /// </summary>
        /// <returns><c>true</c> if the selected schedule is matching, otherwise, <c>false</c>.</returns>
        public bool IsScheduling()
        {
            var eventItem = homegenie.ProgramManager.SchedulerService.Get(scheduleName);
            if (eventItem != null)
            {
                return homegenie.ProgramManager.SchedulerService.IsScheduling(eventItem.CronExpression);
            }
            return false;
        }

        /// <summary>
        /// Determines whether the given cron expression is matching in this very moment.
        /// </summary>
        /// <returns><c>true</c> if the given cron expression is matching; otherwise, <c>false</c>.</returns>
        /// <param name="cronExpression">Cron expression.</param>
        public bool IsScheduling(string cronExpression)
        {
            return homegenie.ProgramManager.SchedulerService.IsScheduling(cronExpression);
        }

    }
}
