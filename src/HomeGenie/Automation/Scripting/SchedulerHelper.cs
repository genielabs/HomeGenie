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

using Innovative.SolarCalculator;

using HomeGenie.Automation.Scheduler;
using HomeGenie.Service;

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
        /// Selects the schedule with the specified name.
        /// </summary>
        /// <param name="name">Name.</param>
        public SchedulerHelper WithName(string name)
        {
            scheduleName = name;
            return this;
        }

        /// <summary>
        /// Gets the selected schedule instance.
        /// </summary>
        public SchedulerItem Get()
        {
            return homegenie.ProgramManager.SchedulerService.Get(scheduleName);
        }

        /// <summary>
        /// Adds/Modifies the schedule with the previously selected name.
        /// </summary>
        /// <param name="cronExpression">Cron expression.</param>
        public SchedulerHelper SetSchedule(string cronExpression)
        {
            homegenie.ProgramManager.SchedulerService.AddOrUpdate(scheduleName, cronExpression);
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
                return homegenie.ProgramManager.SchedulerService.IsScheduling(DateTime.Now, eventItem.CronExpression);
            }
            return false;
        }

        /// <summary>
        /// Determines whether the given cron expression is matching at this very moment.
        /// </summary>
        /// <returns><c>true</c> if the given cron expression is matching; otherwise, <c>false</c>.</returns>
        /// <param name="cronExpression">Cron expression.</param>
        public bool IsScheduling(string cronExpression)
        {
            return homegenie.ProgramManager.SchedulerService.IsScheduling(DateTime.Now, cronExpression);
        }

        /// <summary>
        /// Determines whether the given cron expression is a matching occurrence at the given date/time.
        /// </summary>
        /// <returns><c>true</c> if the given cron expression is matching; otherwise, <c>false</c>.</returns>
        /// <param name="date">Date.</param>
        /// <param name="cronExpression">Cron expression.</param>
        public bool IsOccurrence(DateTime date, string cronExpression)
        {
            return homegenie.ProgramManager.SchedulerService.IsScheduling(date, cronExpression);
        }

        /// <summary>
        /// Solar Times data.
        /// </summary>
        /// <returns>SolarTime data.</returns>
        /// <param name="date">Date.</param>
        public SolarTimes SolarTimes(DateTime date)
        {
            return new SolarTimes(date, homegenie.ProgramManager.SchedulerService.Location["latitude"].Value, homegenie.ProgramManager.SchedulerService.Location["longitude"].Value);
        }
    }
}
