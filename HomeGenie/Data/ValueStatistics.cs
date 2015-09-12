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

        private List<StatValue> statValues;
        private TsList<StatValue> historyValues = new TsList<StatValue>();
        private int historyLimit = 10;
        private StatValue lastEvent, lastOn, lastOff;

        public ValueStatistics()
        {
            LastProcessedTimestap = DateTime.UtcNow;
            statValues = new List<StatValue>();
            statValues.Add(new StatValue(0, LastProcessedTimestap));
            lastEvent = lastOn = lastOff = new StatValue(0, LastProcessedTimestap);
            historyValues.Add(lastEvent);
        }

        public void AddValue(string fieldName, double value, DateTime timestamp)
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
                if (value == 0)
                {
                    lastOn = lastEvent;
                    lastOff = new StatValue(value, timestamp);
                }
                else if (Current.Value == 0)
                {
                    lastOff = lastEvent;
                    lastOn = new StatValue(value, timestamp);
                }
            }
            // insert current value into history and so update "Current" to "value"
            historyValues.Insert(0, new StatValue(value, timestamp));
            // keeep size within historyLimit
            while (historyValues.Count > historyLimit)
            {
                historyValues.RemoveAt(historyValues.Count - 1);
            }
        }

        public int HistoryLimit
        {
            get { return historyLimit; }
            set { historyLimit = value; }
        }
        
        public TsList<StatValue> History
        {
            get { return historyValues; }
        }

        public StatValue Current
        {
            get { return historyValues[0]; }
        }

        public StatValue Last
        {
            get { return lastEvent; }
        }

        public StatValue LastOn
        {
            get { return lastOn; }
        }
        
        public StatValue LastOff
        {
            get { return lastOff; }
        }

        /// <summary>
        /// Get resampled statistic values by averaging values for a given time range increment (eg 60 minutes)
        /// </summary>
        public List<StatValue> GetResampledValues(int sampleWidth) // in minutes
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
