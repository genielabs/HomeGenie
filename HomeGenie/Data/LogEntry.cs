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

namespace HomeGenie.Data
{
    [Serializable()]
    public class LogEntry
    {
        public DateTime Timestamp;
        public double UnixTimestamp
        {
            get
            {
                var uts = (Timestamp - new DateTime(1970, 1, 1, 0, 0, 0));
                return uts.TotalMilliseconds;
            }
        }
        public string Domain = "";
        public string Source = "";
        public string Description = "";
        public string Property = "";
        public string Value = "";

        public LogEntry()
        {
            Timestamp = DateTime.UtcNow;
        }

        public override string ToString()
        {
            string date = this.Timestamp.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");
            string logentrytxt = date + "\t" + this.Domain + "\t" + this.Source + "\t" + (this.Description == "" ? "-" : this.Description) + "\t" + this.Property + "\t" + this.Value;
            return logentrytxt;
        }

    }
}
