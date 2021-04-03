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
using System.Threading;
using System.Xml.Serialization;

using LiteDB;

using Newtonsoft.Json;

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
        private object data;
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
        [XmlIgnore, JsonIgnore, BsonIgnore]
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
        /// Gets the data object.
        /// </summary>
        /// <returns></returns>
        public object GetData()
        {
            return data;
        }

        /// <summary>
        /// If data is stored as a JSON serialized string, use this method to get the object instance specifying its type `T`.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>The data object as type `T`.</returns>
        /// <example>
        /// Example:
        /// <code>
        /// // Sets the data as JSON serialized object
        /// parameter.Value = "{ \"foo\": \"bar\", \"pippo\": \"pluto\" }";
        /// var data = parameter.GetData<Dictionary<string,string>>();
        /// foreach(var item in data) {
        ///     // ...
        /// }
        /// </code>
        /// </example>
        public T GetData<T>()
        {
            if (data is string)
            {
                try
                {
                    data = JsonConvert.DeserializeObject<T>(
                        Convert.ToString(data, CultureInfo.InvariantCulture),
                        new JsonSerializerSettings() {Culture = CultureInfo.InvariantCulture}
                    );
                }
                catch (Exception e)
                {
                    // ignored
                }
            }
            return (T)data;
        }

        /// <summary>
        /// Sets the data of this parameter.
        /// </summary>
        /// <param name="dataObject"></param>
        public void SetData(object dataObject)
        {
            UpdateTime = DateTime.UtcNow;
            data = dataObject;
            if (data!= null)
            {
                // is this a numeric value that can be added for statistics?
                string stringValue = Value;
                double v;
                if (!string.IsNullOrEmpty(stringValue) && double.TryParse(stringValue.Replace(",", "."), NumberStyles.Float | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out v))
                {
                    Statistics.AddValue(Name, v, UpdateTime);
                }
            }
        }

        /// <summary>
        /// Gets or sets the data of this parameter as string. If the value is a non-primitive object, set using the `setData` method, then the getter of `Value` will return the JSON serialized data.
        /// </summary>
        /// <value>The string value.</value>
        public string Value
        {
            get
            {
                bool isNumber = data is sbyte
                                || data is byte
                                || data is short
                                || data is ushort
                                || data is int
                                || data is uint
                                || data is long
                                || data is ulong
                                || data is float
                                || data is double
                                || data is decimal;
                if (isNumber || data is string)
                {
                    return Convert.ToString(data, CultureInfo.InvariantCulture);
                }
                return JsonConvert.SerializeObject(data, new JsonSerializerSettings(){ Culture = CultureInfo.InvariantCulture });
            }
            set { SetData(value); }
        }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        [BsonIgnore]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the type of the field.
        /// </summary>
        /// <value>The type of the field.</value>
        [BsonIgnore]
        public string FieldType { get; set; }
        
        [JsonIgnore, BsonIgnore]
        public int ParentId { get; set; }

        /// <summary>
        /// Gets the update time.
        /// </summary>
        /// <value>The update time.</value>
        public DateTime UpdateTime { get; set; }

        // TODO: deprecate this field
        [XmlIgnore, BsonIgnore]
        public bool NeedsUpdate { get; set; }

        /// <summary>
        /// Gets the decimal value.
        /// </summary>
        /// <value>The decimal value.</value>
        [XmlIgnore, JsonIgnore, BsonIgnore]
        public double DecimalValue
        {
            get
            {
                string stringValue = Value;
                double v = 0;
                if (!String.IsNullOrEmpty(stringValue) && !double.TryParse(stringValue.Replace(",", "."), NumberStyles.Float | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out v)) v = 0;
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
        [XmlIgnore, JsonIgnore, BsonIgnore]
        public double IdleTime
        {
            get
            {
                return (DateTime.UtcNow - UpdateTime).TotalSeconds;
            }
        }

    }

}
