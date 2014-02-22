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

namespace HomeGenie.Automation.Scripting
{
    public class AutomationStatesManager
    {
        private string _currentstate = "NotSelected.State";
        private Dictionary<string, bool> _statesdictionary;

        public AutomationStatesManager(Dictionary<string, bool> statesref)
        {
            _statesdictionary = statesref;
        }

        public AutomationStatesManager WithName(string name)
        {
            _currentstate = name;
            return this;
        }

        public bool IsOn
        {
            get
            {
                if (_statesdictionary.ContainsKey(_currentstate))
                {
                    return _statesdictionary[_currentstate];
                }
                return false;
            }
        }

        public bool IsOff
        {
            get
            {
                if (_statesdictionary.ContainsKey(_currentstate))
                {
                    return !_statesdictionary[_currentstate];
                }
                return true;
            }
        }

        public AutomationStatesManager Off()
        {
            if (_statesdictionary.ContainsKey(_currentstate))
            {
                _statesdictionary[_currentstate] = false;
            }
            return this;
        }

        public AutomationStatesManager On()
        {
            if (_statesdictionary.ContainsKey(_currentstate))
            {
                _statesdictionary[_currentstate] = true;
            }
            return this;
        }

        public AutomationStatesManager Toggle()
        {
            if (_statesdictionary.ContainsKey(_currentstate))
            {
                _statesdictionary[_currentstate] = !_statesdictionary[_currentstate];
            }
            return this;
        }

        public AutomationStatesManager Set(bool value)
        {
            if (_statesdictionary.ContainsKey(_currentstate))
            {
                _statesdictionary[_currentstate] = value;
            }
            else
            {
                _statesdictionary.Add(_currentstate, value);
            }
            return this;
        }
    }
}
