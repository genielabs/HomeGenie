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
using HomeGenie.Service.Constants;
using Newtonsoft.Json;
using ExpressionEvaluation;
using HomeGenie.Service;

namespace HomeGenie.Automation.Scheduler
{
    public class SchedulerService
    {
        private const int MAX_EVAL_RECURSION = 4;
        private List<SchedulerItem> events = new List<SchedulerItem>();
        private Timer serviceChecker;
        private ProgramManager masterControlProgram;

        public SchedulerService(ProgramManager programEngine)
        {
            masterControlProgram = programEngine;
        }

        public void Start()
        {
            Stop();
            serviceChecker = new Timer(CheckScheduledEvents); //, null, 1000, 1000);
            serviceChecker.Change((60-DateTime.Now.Second)*1000, Timeout.Infinite);
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
            serviceChecker.Change((60-DateTime.Now.Second)*1000, Timeout.Infinite);
            var date = DateTime.Now;
            for (int i = 0; i < events.Count; i++)
            {
                var eventItem = events[i];
                if (eventItem.IsEnabled)
                {
                    // update next occurrence value
                    eventItem.NextOccurrence = GetNextEventOccurrence(date, eventItem.CronExpression);
                    // execute items only once instead of repeating for the whole minute
                    string currentOccurrence = date.ToString("yyyy-MM-dd HH:mm");
                    if (eventItem.LastOccurrence != currentOccurrence && IsScheduling(date, eventItem.CronExpression))
                    {
                        /*
                        masterControlProgram.HomeGenie.MigService.RaiseEvent(
                        this,
                        Domains.HomeAutomation_HomeGenie,
                        SourceModule.Scheduler,
                        "Scheduler Event Triggered",
                        Properties.SchedulerTriggeredEvent,
                        eventItem.Name);
                        */
                        // update last occurrence value
                        eventItem.LastOccurrence = currentOccurrence;
                        // execute associated task if any
                        if (!String.IsNullOrEmpty(eventItem.ProgramId))
                        {
                            var program = masterControlProgram.Programs.Find(p => p.Address.ToString() == eventItem.ProgramId || p.Name == eventItem.ProgramId);
                            if (program != null)
                            {
                                masterControlProgram.HomeGenie.MigService.RaiseEvent(this, Domains.HomeAutomation_HomeGenie, SourceModule.Scheduler, "Scheduler Event '" + eventItem.Name + "'", Properties.SchedulerTriggeredEvent, "'" + eventItem.Name + "' running '" + eventItem.ProgramId + "'");
                                masterControlProgram.Run(program, "");
                            }
                            else
                            {
                                masterControlProgram.HomeGenie.MigService.RaiseEvent(this, Domains.HomeAutomation_HomeGenie, SourceModule.Scheduler, "Scheduler Event '" + eventItem.Name + "'", Properties.SchedulerError, "No such program: '" + eventItem.ProgramId + "'");
                            }
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

        public SchedulerItem AddOrUpdate(string name, string cronExpression, string data = null, string description = null, string pid = null)
        {
            if (String.IsNullOrEmpty(name)) return null;
            //
            var eventItem = Get(name);
            bool justAdded = false;
            if (eventItem == null)
            {
                eventItem = new SchedulerItem();
                eventItem.Name = name;
                events.Add(eventItem);
                justAdded = true;
            }
            eventItem.CronExpression = cronExpression;
            if (description != null)
                eventItem.Description = description;
            if (data != null)
                eventItem.Data = data;
            if (pid != null)
                eventItem.ProgramId = pid;
            eventItem.LastOccurrence = "-";
            eventItem.NextOccurrence = GetNextEventOccurrence(DateTime.Now, eventItem.CronExpression);
            // by default newly added events are enabled
            if (justAdded)
            {
                eventItem.IsEnabled = true;
            }
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
                eventItem.NextOccurrence = GetNextEventOccurrence(DateTime.Now, eventItem.CronExpression);
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

        public bool IsScheduling(DateTime date, string cronExpression, int recursionCount = 0)
        {
            string buildExpression = "";
            int p = 0;
            while (p < cronExpression.Length)
            {
                char token = cronExpression[p];
                if (token == '(' || token == ')' || token == ';' || token == ':')
                {
                    buildExpression += token;
                    p++;
                    continue;
                }

                string currentExpression = token.ToString();
                p++;
                while (p < cronExpression.Length)
                {
                    token = cronExpression[p];
                    if (token != '(' && token != ')' && token != ';' && token != ':')
                    {
                        currentExpression += token;
                        p++;
                    }
                    else
                    {
                        break;
                    }
                }

                currentExpression = currentExpression.Trim(new char[] { ' ', '\t' });
                if (String.IsNullOrEmpty(currentExpression)) continue;

                bool isEntryActive = false;
                if (currentExpression.StartsWith("#"))
                {
                    isEntryActive = true;
                }
                else if (currentExpression.StartsWith("@"))
                {
                    // Check expresion from scheduled item with a given name
                    var eventItem = Get(currentExpression.Substring(1));
                    if (eventItem == null)
                    {
                        masterControlProgram.HomeGenie.MigService.RaiseEvent(
                            this,
                            Domains.HomeAutomation_HomeGenie,
                            SourceModule.Scheduler,
                            cronExpression,
                            Properties.SchedulerError,
                            JsonConvert.SerializeObject("Unknown event name '"+currentExpression+"'"));
                    }
                    else if (recursionCount >= MAX_EVAL_RECURSION)
                    {
                        recursionCount = 0;
                        masterControlProgram.HomeGenie.MigService.RaiseEvent(
                            this,
                            Domains.HomeAutomation_HomeGenie,
                            SourceModule.Scheduler,
                            cronExpression,
                            Properties.SchedulerError,
                            JsonConvert.SerializeObject("Too much recursion in expression '"+currentExpression+"'"));
                        eventItem.IsEnabled = false;
                    }
                    else
                    {
                        recursionCount++;
                        try
                        {
                            isEntryActive = (eventItem.IsEnabled && IsScheduling(date, eventItem.CronExpression, recursionCount));
                        } catch{ }
                        recursionCount--;
                        if (recursionCount < 0)
                            recursionCount = 0;
                    }
                }
                else
                {
                    isEntryActive = EvaluateCronEntry(date, currentExpression);
                }

                buildExpression += (isEntryActive ? "1" : "0");

            }

            buildExpression = buildExpression.Replace(":", "+");
            buildExpression = buildExpression.Replace(";", "*");

            bool success = false;
            try
            {
                ExpressionEval eval = new ExpressionEval();
                eval.Expression = buildExpression;
                success = eval.EvaluateBool();
            }
            catch (Exception ex)
            {
                masterControlProgram.HomeGenie.MigService.RaiseEvent(
                    this,
                    Domains.HomeAutomation_HomeGenie,  // before v1.1 it was: Domains.HomeAutomation_HomeGenie_Automation,
                    SourceModule.Scheduler, // before v1.1 it was: cronExpression,
                    cronExpression, // before v1.1 it was: "Scheduler Expression",
                    Properties.SchedulerError,
                    JsonConvert.SerializeObject(ex.Message));
            }
            return success;
        }

        public List<SchedulerItem> Items
        {
            get { return events; }
        }


        private bool EvaluateCronEntry(DateTime date, string cronExpression)
        {
            var cronSchedule = NCrontab.CrontabSchedule.TryParse(cronExpression);
            if (!cronSchedule.IsError)
            {
                var occurrence = cronSchedule.Value.GetNextOccurrence(date.AddMinutes(-1));
                string d1 = date.ToString("yyyy-MM-dd HH:mm");
                string d2 = occurrence.ToString("yyyy-MM-dd HH:mm");
                if (d1 == d2)
                {
                    return true;
                }
            }
            else
            {
                masterControlProgram.HomeGenie.MigService.RaiseEvent(
                    this,
                    Domains.HomeAutomation_HomeGenie,
                    SourceModule.Scheduler,
                    cronExpression,
                    Properties.SchedulerError,
                    JsonConvert.SerializeObject("Syntax error in expression '"+cronExpression+"'"));
            }
            return false;
        }

        // TODO: deprecate this method
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
                    if (eventItem != null && eventItem.IsEnabled && IsEventActive(eventItem.CronExpression, checkedStack))
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

        private string GetNextEventOccurrence(DateTime date, string cronExpression)
        {
            var cronSchedule = NCrontab.CrontabSchedule.TryParse(cronExpression);
            if (!cronSchedule.IsError)
            {
                var occurrence = cronSchedule.Value.GetNextOccurrence(date);
                return occurrence.ToString("yyyy-MM-dd HH:mm");
            }
            return "-";
        }

    }
}
