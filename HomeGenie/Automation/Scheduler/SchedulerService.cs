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

using NCrontab;
using System.Threading;

namespace HomeGenie.Automation.Scheduler
{
    public class SchedulerService
    {
        private List<SchedulerItem> events = new List<SchedulerItem>();
        private Timer serviceChecker;
        private ProgramEngine masterControlProgram;

        public SchedulerService(ProgramEngine programEngine)
        {
            masterControlProgram = programEngine;
        }

        public void Start()
        {
            Stop();
            serviceChecker = new Timer(CheckScheduledEvents, null, 1000, 1000);
        }

        public void Stop()
        {
            if (serviceChecker != null)
            {
                serviceChecker.Dispose();
            }
        }

        private void CheckScheduledEvents(object state)
        {
            for (int i = 0; i < events.Count; i++)
            {
                var eventItem = events[i];
                // TODO: execute items only once instead of repeating for the whole minute
                string currentoccurrence = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                if (eventItem.IsEnabled && eventItem.LastOccurrence != currentoccurrence && IsScheduling(eventItem.CronExpression))
                {
                    // update last/next occurrence values
                    eventItem.LastOccurrence = currentoccurrence;
                    eventItem.NextOccurrence = GetNextEventOccurrence(eventItem.CronExpression);
                    // execute associated task if any
                    if (!String.IsNullOrEmpty(eventItem.ProgramId))
                    {
                        var program = masterControlProgram.Programs.Find(p => p.Address.ToString() == eventItem.ProgramId || p.Name == eventItem.ProgramId);
                        if (program != null)
                        {
                            masterControlProgram.Run(program, "");
                        }
                    }
                }
            }
        }

        public SchedulerItem Get(string name)
        {
            var eventItem = events.Find(e => e.Name.ToLower() == name.ToLower());
            return eventItem;
        }

        public SchedulerItem AddOrUpdate(string name, string cronExpression)
        {
            if (String.IsNullOrEmpty(name)) return null;
            //
            var eventItem = Get(name);
            if (eventItem == null)
            {
                eventItem = new SchedulerItem();
                eventItem.Name = name;
                events.Add(eventItem);
            }
            eventItem.CronExpression = cronExpression;
            eventItem.LastOccurrence = "-";
            eventItem.NextOccurrence = GetNextEventOccurrence(eventItem.CronExpression);
            return eventItem;
        }

        public bool SetProgram(string name, string pid)
        {
            var eventItem = Get(name);
            if (eventItem != null)
            {
                eventItem.ProgramId = pid;
                return true;
            }
            return false;
        }

        public bool Enable(string name)
        {
            var eventItem = Get(name);
            if (eventItem != null)
            {
                eventItem.IsEnabled = true;
                eventItem.NextOccurrence = GetNextEventOccurrence(eventItem.CronExpression);
                return true;
            }
            return false;
        }

        public bool Disable(string name)
        {
            var eventItem = Get(name);
            if (eventItem != null)
            {
                eventItem.IsEnabled = false;
                eventItem.LastOccurrence = "-";
                eventItem.NextOccurrence = "-";
                return true;
            }
            return false;
        }

        public bool Remove(string name)
        {
            var eventItem = Get(name);
            if (eventItem == null)
            {
                return false;
            }
            events.Remove(eventItem);
            return true;
        }

        public bool IsScheduling(string cronExpression)
        {
            bool success = true;
            string[] exprs = cronExpression.Split(';'); // <-- ';' is AND operator
            for (int e = 0; e < exprs.Length; e++)
            {
                string currentexpr = exprs[e].Trim();
                success = success && IsEventActive(currentexpr);
                if (!success) break;
            }
            return success;
        }

        public List<SchedulerItem> Items
        {
            get { return events; }
        }


        private bool IsEventActive(string cronExpression, List<string> checkedStack = null)
        {
            if (checkedStack == null) checkedStack = new List<string>();
            //
            string[] expressionList = cronExpression.Split(':'); // <-- ':' is OR operator
            for (int e = 0; e < expressionList.Length; e++)
            {
                string currentExpression = expressionList[e].Trim();
                // avoid loops
                if (checkedStack.Contains(currentExpression))
                {
                    continue;
                }
                checkedStack.Add(currentExpression);
                if (currentExpression.StartsWith("@"))
                {
                    // Check expresion from scheduled item with a given name
                    var eventItem = Get(currentExpression.Substring(1));
                    if (eventItem.IsEnabled && IsEventActive(eventItem.CronExpression, checkedStack))
                    {
                        return true;
                    }
                }
                else
                {
                    // Check current expression
                    var cronSchedule = NCrontab.CrontabSchedule.TryParse(currentExpression);
                    if (!cronSchedule.IsError)
                    {
                        var occurrence = cronSchedule.Value.GetNextOccurrence(DateTime.Now.AddMinutes(-1));
                        string d1 = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                        string d2 = occurrence.ToString("yyyy-MM-dd HH:mm");
                        if (d1 == d2)
                        {
                            return true;
                        }
                    }
                }
            }
            //
            return false;
        }

        private string GetNextEventOccurrence(string cronExpression)
        {
            var cronSchedule = NCrontab.CrontabSchedule.TryParse(cronExpression);
            if (!cronSchedule.IsError)
            {
                var occurrence = cronSchedule.Value.GetNextOccurrence(DateTime.Now);
                return occurrence.ToString("yyyy-MM-dd HH:mm");
            }
            return "-";
        }

    }
}
