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
using HomeGenie.Service;
using HomeGenie.Service.Logging;

namespace HomeGenie.Data
{
    /// <summary>
    /// Value statistics.
    /// </summary>
    public class ValueStatistics
    {
        /// <summary>
        /// Stat value.
        /// </summary>
        public class StatValue
        {
            /// <summary>
            /// Gets the value.
            /// </summary>
            /// <value>The value.</value>
            public readonly double Value;
            /// <summary>
            /// Gets the timestamp.
            /// </summary>
            /// <value>The timestamp.</value>
            public readonly DateTime Timestamp;

            /// <summary>
            /// Gets the unix timestamp.
            /// </summary>
            /// <value>The unix timestamp.</value>
            public double UnixTimestamp
            {
                get
                {
                    var uts = (Timestamp - new DateTime(1970, 1, 1, 0, 0, 0));
                    return uts.TotalMilliseconds;
                }
            }

            public StatValue(double value, DateTime timestamp)
            {
                Value = value;
                Timestamp = timestamp;
            }
        }

        private List<StatValue> statValues;
        private TsList<StatValue> historyValues;
        // historyLimit is expressed in minutes
        private int historyLimit = 60 * 24;
        private StatValue lastEvent, lastOn, lastOff;

        public ValueStatistics()
        {
            LastProcessedTimestap = DateTime.UtcNow;
            statValues = new List<StatValue>();
            statValues.Add(new StatValue(0, LastProcessedTimestap));
            lastEvent = lastOn = lastOff = new StatValue(0, LastProcessedTimestap);
            historyValues = new TsList<StatValue>();
            historyValues.Add(lastEvent);
        }

        /// <summary>
        /// Gets or sets the history limit.
        /// </summary>
        /// <value>The history limit.</value>
        public int HistoryLimit
        {
            get { return historyLimit; }
            set { historyLimit = value; }
        }

        /// <summary>
        /// Gets the history.
        /// </summary>
        /// <value>The history.</value>
        public TsList<StatValue> History
        {
            get { return historyValues; }
        }

        /// <summary>
        /// Gets the current value.
        /// </summary>
        /// <value>The current.</value>
        public StatValue Current
        {
            get { return historyValues[0]; }
        }

        /// <summary>
        /// Gets the last value.
        /// </summary>
        /// <value>The last.</value>
        public StatValue Last
        {
            get { return lastEvent; }
        }

        /// <summary>
        /// Gets the last on value (value != 0).
        /// </summary>
        /// <value>The last on.</value>
        public StatValue LastOn
        {
            get { return lastOn; }
        }

        /// <summary>
        /// Gets the last off value (value == 0).
        /// </summary>
        /// <value>The last off.</value>
        public StatValue LastOff
        {
            get { return lastOff; }
        }

        internal void AddValue(string fieldName, double value, DateTime timestamp)
        {
            if (StatisticsLogger.IsValidField(fieldName))
            {
                // add value for StatisticsLogger use
                statValues.Add(new StatValue(value, timestamp));
            }
            // "value" is the occurring event in this very moment, 
            // so "Current" is holding previous value right now
            if (Current.Value != value)
            {
                lastEvent = new StatValue(Current.Value, Current.Timestamp);
                if (value == 0 && lastEvent.Value > 0)
                {
                    lastOn = lastEvent;
                    lastOff = new StatValue(value, timestamp);
                }
                else if (value > 0 && lastEvent.Value == 0)
                {
                    lastOff = lastEvent;
                    lastOn = new StatValue(value, timestamp);
                }
            }
            // insert current value into history and so update "Current" to "value"
            historyValues.Insert(0, new StatValue(value, timestamp));
            // keeep size within historyLimit (minutes)
            while ((DateTime.UtcNow - historyValues[historyValues.Count - 1].Timestamp).TotalMinutes > historyLimit)
            {
                historyValues.RemoveAll(sv => (DateTime.UtcNow - sv.Timestamp).TotalMinutes > historyLimit);
            }
        }

        /// <summary>
        /// Get resampled statistic values by averaging values for a given time range increment (eg 60 minutes)
        /// </summary>
        internal List<StatValue> GetResampledValues(int sampleWidth) // in minutes
        {
            // TODO: to be implemented
            return null;
        }

        // These fields are used by StatisticsLogger
        internal DateTime LastProcessedTimestap;
        internal List<StatValue> Values
        {
            get { return statValues; }
        }

    }
}
