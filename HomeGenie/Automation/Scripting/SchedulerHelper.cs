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

		private HomeGenieService _homegenie;
        private string _schedulename;

        public SchedulerHelper(HomeGenieService hg)
		{
			_homegenie = hg;
		}

        public SchedulerHelper WithName(string name)
        {
            _schedulename = name;
            return this;
        }

        public SchedulerHelper SetSchedule(string cronexpr)
        {
            _homegenie.ProgramEngine.SchedulerService.AddOrUpdate(_schedulename, cronexpr);
            return this;
        }

        public SchedulerHelper SetProgram(string pid)
        {
            _homegenie.ProgramEngine.SchedulerService.SetProgram(_schedulename, pid);
            return this;
        }

        public bool IsScheduling()
        {
            SchedulerItem item = _homegenie.ProgramEngine.SchedulerService.Get(_schedulename);
            if (item != null)
            {
                return _homegenie.ProgramEngine.SchedulerService.IsScheduling(item.CronExpression);
            }
            return false;
        }

        public bool IsScheduling(string expr)
        {
            return _homegenie.ProgramEngine.SchedulerService.IsScheduling(expr);
        }

    }
}
