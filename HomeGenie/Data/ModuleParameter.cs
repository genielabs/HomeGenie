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
using System.Globalization;

using Newtonsoft.Json;
using System.Xml.Serialization;
using HomeGenie.Service;
using HomeGenie.Service.Logging;
using System.Threading;

namespace HomeGenie.Data
{

    /// <summary>
    /// Module parameter.
    /// </summary>
    [Serializable()]
    public class ModuleParameter
    {
        [NonSerialized]
        private ValueStatistics statistics;
        [NonSerialized]
        private DateTime requestUpdateTimestamp = DateTime.UtcNow;
        private string parameterValue;
        //
        public ModuleParameter()
        {
            // initialize 
            Name = "";
            Value = "";
            Description = "";
            FieldType = "";
            UpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the statistics.
        /// </summary>
        /// <value>The statistics.</value>
        [XmlIgnore, JsonIgnore]
        public ValueStatistics Statistics
        {
            get
            {
                if (statistics == null) statistics = new ValueStatistics();
                return statistics;
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public string Value
        {
            get
            {
                return parameterValue;
            }
            set
            {
                UpdateTime = DateTime.UtcNow;
                parameterValue = value;
                // is this a numeric value that can be added for statistics?
                double v;
                if (!string.IsNullOrEmpty(value) && double.TryParse(value.Replace(",", "."), NumberStyles.Float | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out v))
                {
                    Statistics.AddValue(Name, v, this.UpdateTime);
                }
            }
        }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the type of the field.
        /// </summary>
        /// <value>The type of the field.</value>
        [JsonIgnore]
        public string FieldType { get; set; }

        /// <summary>
        /// Gets the update time.
        /// </summary>
        /// <value>The update time.</value>
        public DateTime UpdateTime { get; set; }

        [XmlIgnore,JsonIgnore]
        public bool NeedsUpdate { get; set; }

        /// <summary>
        /// Gets the decimal value.
        /// </summary>
        /// <value>The decimal value.</value>
        [XmlIgnore, JsonIgnore]
        public double DecimalValue
        {
            get
            {

                double v = 0;
                if (this.Value != null && !double.TryParse(this.Value.Replace(",", "."), NumberStyles.Float | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out v)) v = 0;
                return v;
            }
        }

        /// <summary>
        /// Determines whether this instance has the given name.
        /// </summary>
        /// <returns><c>true</c> if this instance is name; otherwise, <c>false</c>.</returns>
        /// <param name="name">Name.</param>
        public bool Is(string name)
        {
            return (this.Name.ToLower() == name.ToLower());
        }

        public void RequestUpdate()
        {
            requestUpdateTimestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Waits until this parameter is updated.
        /// </summary>
        /// <returns><c>true</c>, if it was updated, <c>false</c> otherwise.</returns>
        /// <param name="timeoutSeconds">Timeout seconds.</param>
        public bool WaitUpdate(double timeoutSeconds)
        {
            var lastUpdate = UpdateTime;
            while (lastUpdate.Ticks == UpdateTime.Ticks && (DateTime.UtcNow - requestUpdateTimestamp).TotalSeconds < timeoutSeconds)
                Thread.Sleep(250);
            return lastUpdate.Ticks != UpdateTime.Ticks;
        }

        /// <summary>
        /// Gets the idle time (time elapsed since last update).
        /// </summary>
        /// <value>The idle time.</value>
        [XmlIgnore, JsonIgnore]
        public double IdleTime
        {
            get 
            {
                return (DateTime.UtcNow - UpdateTime).TotalSeconds;
            }
        }

    }

}

