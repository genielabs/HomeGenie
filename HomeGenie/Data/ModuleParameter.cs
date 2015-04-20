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
            UpdateTime = DateTime.UtcNow;
        }
        //
        [XmlIgnore, JsonIgnore]
        public ValueStatistics Statistics
        {
            get
            {
                if (statistics == null) statistics = new ValueStatistics();
                return statistics;
            }
        }
        //
        public string Name { get; set; }
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
                //
                // can we add this value for statistics?
                double v;
                if (!string.IsNullOrEmpty(value) && double.TryParse(value.Replace(",", "."), NumberStyles.Float | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out v))
                {
                    Statistics.AddValue(Name, v, this.UpdateTime);
                }
            }
        }
        public string Description { get; set; }
        public DateTime UpdateTime { get; /* protected */ set; }
        [XmlIgnore]
        public bool NeedsUpdate { get; set; }

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

        public bool Is(string name)
        {
            return (this.Name.ToLower() == name.ToLower());
        }

        public void RequestUpdate()
        {
            requestUpdateTimestamp = DateTime.UtcNow;
        }

        public bool WaitUpdate(double timeoutSeconds)
        {
            var lastUpdate = UpdateTime;
            while (lastUpdate.Ticks == UpdateTime.Ticks && (DateTime.UtcNow - requestUpdateTimestamp).TotalSeconds < timeoutSeconds)
                Thread.Sleep(250);
            return lastUpdate.Ticks != UpdateTime.Ticks;
        }

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

