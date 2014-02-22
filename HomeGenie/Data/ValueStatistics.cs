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
    public class ValueStatistics
    {
        public class StatValue
        {
            public double Value;
            public DateTime Timestamp;

            public StatValue(double value, DateTime timestamp)
            {
                Value = value;
                Timestamp = timestamp;
            }
        }

        public DateTime LastProcessedTimestap;

        private List<StatValue> _values;

        public ValueStatistics()
        {
            LastProcessedTimestap = DateTime.UtcNow;
            _values = new List<StatValue>();
            _values.Add(new StatValue(0, LastProcessedTimestap));
        }

        public void AddValue(double value, DateTime timestamp)
        {
            _values.Add(new StatValue(value, timestamp));
        }

        public List<StatValue> Values
        {
            get { return _values; }
        }

        /// <summary>
        /// Get resampled statistic values by averaging values for a given time range increment (eg 60 minutes)
        /// </summary>
        public List<StatValue> GetResampledValues(int samplewidth) // in minutes
        {
            return null;
        }

    }
}
