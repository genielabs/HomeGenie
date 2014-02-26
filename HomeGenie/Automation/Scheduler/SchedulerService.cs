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
        private List<SchedulerItem> _scheduleditems = new List<SchedulerItem>();
        private Timer _servicechecker;
        private ProgramEngine _mastercontrolprogram;

        public SchedulerService(ProgramEngine mcp)
        {
            _mastercontrolprogram = mcp;
        }

        public void Start()
        {
            Stop();
            _servicechecker = new Timer(_checkscheduleditems, null, 1000, 1000);
        }

        public void Stop()
        {
            if (_servicechecker != null)
            {
                _servicechecker.Dispose();
            }
        }

        private void _checkscheduleditems(object state)
        {
            for (int i = 0; i < _scheduleditems.Count; i++)
            {
                SchedulerItem item = _scheduleditems[i];
                // TODO: execute items only once instead of repeating for the whole minute
                string currentoccurrence = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                if (item.IsEnabled && item.LastOccurrence != currentoccurrence && IsScheduling(item.CronExpression))
                {
                    // update last/next occurrence values
                    item.LastOccurrence = currentoccurrence;
                    item.NextOccurrence = _getnextoccurrence(item.CronExpression);
                    // execute associated task if any
                    if (!String.IsNullOrEmpty(item.ProgramId))
                    {
                        ProgramBlock pb = _mastercontrolprogram.Programs.Find(p => p.Address.ToString() == item.ProgramId || p.Name == item.ProgramId);
                        if (pb != null)
                        {
                            _mastercontrolprogram.Run(pb, "");
                        }
                    }
                }
            }
        }

        public SchedulerItem Get(string name)
        {
            SchedulerItem item = _scheduleditems.Find(e => e.Name.ToLower() == name.ToLower());
            return item;
        }

        public SchedulerItem AddOrUpdate(string name, string cronexp)
        {
            if (String.IsNullOrEmpty(name)) return null;
            //
            SchedulerItem item = Get(name);
            if (item == null)
            {
                item = new SchedulerItem();
                item.Name = name;
                _scheduleditems.Add(item);
            }
            item.CronExpression = cronexp;
            item.LastOccurrence = "-";
            item.NextOccurrence = _getnextoccurrence(item.CronExpression);
            return item;
        }

        public bool SetProgram(string name, string pid)
        {
            SchedulerItem item = Get(name);
            if (item != null)
            {
                item.ProgramId = pid;
                return true;
            }
            return false;
        }

        public bool Enable(string name)
        {
            SchedulerItem item = Get(name);
            if (item != null)
            {
                item.IsEnabled = true;
                item.NextOccurrence = _getnextoccurrence(item.CronExpression);
                return true;
            }
            return false;
        }

        public bool Disable(string name)
        {
            SchedulerItem item = Get(name);
            if (item != null)
            {
                item.IsEnabled = false;
                item.LastOccurrence = "-";
                item.NextOccurrence = "-";
                return true;
            }
            return false;
        }

        public bool Remove(string name)
        {
            SchedulerItem item = Get(name);
            if (item == null)
            {
                return false;
            }
            _scheduleditems.Remove(item);
            return true;
        }

        public bool IsScheduling(string cronexp)
        {
            bool success = true;
            string[] exprs = cronexp.Split(';'); // <-- ';' is AND operator
            for (int e = 0; e < exprs.Length; e++)
            {
                string currentexpr = exprs[e].Trim();
                success = success && _matchexpression(currentexpr);
                if (!success) break;
            }
            return success;
        }

        public List<SchedulerItem> Items
        {
            get { return _scheduleditems; }
        }


        private bool _matchexpression(string cronexp, List<string> checkedstack = null)
        {
            if (checkedstack == null) checkedstack = new List<string>();
            //
            string[] exprs = cronexp.Split(':'); // <-- ':' is OR operator
            for (int e = 0; e < exprs.Length; e++)
            {
                string currentexpr = exprs[e].Trim();
                // avoid loops
                if (checkedstack.Contains(currentexpr))
                {
                    continue;
                }
                checkedstack.Add(currentexpr);
                if (currentexpr.StartsWith("@"))
                {
                    // Check expresion from scheduled item with a given name
                    SchedulerItem itemref = Get(currentexpr.Substring(1));
                    if (itemref.IsEnabled && _matchexpression(itemref.CronExpression, checkedstack))
                    {
                        return true;
                    }
                }
                else
                {
                    // Check current expression
                    ValueOrError<CrontabSchedule> cts = NCrontab.CrontabSchedule.TryParse(currentexpr);
                    if (!cts.IsError)
                    {
                        DateTime occurrence = cts.Value.GetNextOccurrence(DateTime.Now.AddMinutes(-1));
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

        private string _getnextoccurrence(string expr)
        {
            ValueOrError<CrontabSchedule> cts = NCrontab.CrontabSchedule.TryParse(expr);
            if (!cts.IsError)
            {
                DateTime occurrence = cts.Value.GetNextOccurrence(DateTime.Now);
                return occurrence.ToString("yyyy-MM-dd HH:mm");
            }
            return "-";
        }

    }
}
