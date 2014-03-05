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

using Newtonsoft.Json;
using System.Xml.Serialization;
using HomeGenie.Service;
using HomeGenie.Service.Logging;

namespace HomeGenie.Data
{

    [Serializable()]
    public class ModuleParameter
    {
        [NonSerialized]
        private ValueStatistics statistics;
        private string parameterValue;
        //
        public ModuleParameter()
        {
            // initialize 
            Name = "";
            Value = "";
            Description = "";
            UpdateTime = DateTime.Now;
            //
            LastValue = "";
            LastUpdateTime = DateTime.Now;
        }
        //
        [XmlIgnore]
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
                //TODO: find a better solution for "Meter.Watts" case
                //if (_value != value || Name == Properties.METER_WATTS || Name.StartsWith("ZWaveNode."))
                {
                    if ((!string.IsNullOrEmpty(parameterValue) && parameterValue != value)) // || Name == Properties.METER_WATTS || Name.StartsWith("ZWaveNode."))
                    {
                        LastValue = parameterValue;
                        LastUpdateTime = UpdateTime.ToUniversalTime();
                    }
                    UpdateTime = DateTime.UtcNow;
                    parameterValue = value;
                    //
                    // can we add this value for statistics?
                    double v;
                    if (value != "" && StatisticsLogger.IsValidField(this.Name) && double.TryParse(value.Replace(",", "."), System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out v))
                    {
                        Statistics.AddValue(v, this.UpdateTime);
                    }
                }
            }
        }
        public string Description { get; set; }
        public DateTime UpdateTime { get; /* protected */ set; }
        public bool NeedsUpdate { get; set; }
        //
        public double ValueIncrement
        {
            get
            {
                return (this.DecimalValue - this.LastDecimalValue);
            }
        }
        //
        public string LastValue { get; /* protected */ set; }
        public DateTime LastUpdateTime { get; /* protected */ set; }

        public double DecimalValue
        {
            get
            {

                double v = 0;
                if (this.Value != null && !double.TryParse(this.Value.Replace(",", "."), System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out v)) v = 0;
                return v;
            }
        }

        public double LastDecimalValue
        {
            get
            {
                double v;
                if (!double.TryParse(this.LastValue, out v)) v = 0;
                return v;
            }
        }

        public bool Is(string name)
        {
            return (this.Name.ToLower() == name.ToLower());
        }
    }

}

