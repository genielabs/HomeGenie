/*
   Copyright 2012-2025 G-Labs (https://github.com/genielabs)

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: https://homegenie.it
 */

using System;
using System.Collections.Generic;
using HomeGenie.Service;

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

        private TsList<StatValue> historyValues;
        // historyLimit is expressed in minutes
        private int historyLimit = 60 * 24;
        private int historyLimitSize = 86400;
        private StatValue lastEvent, lastOn, lastOff;

        public ValueStatistics()
        {
            var initValue = new StatValue(0, DateTime.UtcNow);
            lastEvent = lastOn = lastOff = initValue;
            historyValues = new TsList<StatValue>();
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
        /// Gets or sets the history limit.
        /// </summary>
        /// <value>The history limit.</value>
        public int HistoryLimitSize
        {
            get { return historyLimitSize; }
            set { historyLimitSize = value; }
        }

        /// <summary>
        /// Gets the history.
        /// </summary>
        /// <value>The history.</value>
        public TsList<StatValue> History
        {
            get { return historyValues; }
            set { historyValues = value; }
        }

        /// <summary>
        /// Gets the current value.
        /// </summary>
        /// <value>The current.</value>
        public StatValue Current
        {
            get { return historyValues.Count > 0 ? historyValues[0] : null; }
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
            // "value" is the occurring event in this very moment,
            // so "Current" is holding previous value right now
            if (Current != null && Current.Value != value)
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
            // keep size within historyLimit (minutes)
            try
            {
                if (historyValues.Count > historyLimitSize)
                {
                    historyValues.RemoveRange(historyLimitSize, historyValues.Count - historyLimitSize);
                }
                while (historyValues.Count > 0 && (DateTime.UtcNow - historyValues[historyValues.Count - 1].Timestamp).TotalMinutes > historyLimit)
                {
                    historyValues.RemoveAll(sv => (DateTime.UtcNow - sv.Timestamp).TotalMinutes > historyLimit);
                }
                // leave this wrapped in a try..catch
            } catch { }
            // insert current value into history and so update "Current" to "value"
            historyValues.Insert(0, new StatValue(value, timestamp));
        }

        /// <summary>
        /// Get resampled statistic values by averaging values for a given time range increment (eg 60 minutes)
        /// </summary>
        internal List<StatValue> GetResampledValues(int sampleWidth) // in minutes
        {
            //historyValues.FindAll(sv => (DateTime.UtcNow - sv.Timestamp).TotalMinutes < sampleWidth);
            // TODO: to be implemented
            return null;
        }
    }
}
