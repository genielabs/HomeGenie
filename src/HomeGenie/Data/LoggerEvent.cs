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
 *     Project Homepage: https://homegenie.it
 */

using System;

namespace HomeGenie.Data
{
    [Serializable()]
    public class LoggerEvent
    {
        public LoggerEvent()
        {
            // default constructor
        }

        public LoggerEvent(Module module, ModuleParameter parameter)
        {
            this.Domain = module.Domain;
            this.Address = module.Address;
            this.Description = module.Name;
            this.Parameter = parameter.Name;
            this.Value = parameter.GetData(); // get the raw object value
            this.Date = parameter.UpdateTime;
        }
        public string Domain { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public string Parameter { get; set; }
        public object Value { get; set; }
        public DateTime Date { get; set; }
    }
}
