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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using MIG;

using HomeGenie.Service;

namespace HomeGenie.Data
{
    /// <summary>
    /// Module instance.
    /// </summary>
    [Serializable()]
    public class Module : ICloneable
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the type of the device.
        /// </summary>
        /// <value>The type of the device.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public ModuleTypes DeviceType { get; set; }

        /// <summary>
        /// Gets or sets the domain.
        /// </summary>
        /// <value>The domain.</value>
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        /// <value>The address.</value>
        public string Address { get; set; }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <value>The properties.</value>
        public TsList<ModuleParameter> Properties { get; set; }

        // TODO: deprecate 'Stores' field!!! (DataHelper/LiteDb can be used now to store data for a module) 
        [JsonIgnore]
        public TsList<Store> Stores { get; set; }

        public Module()
        {
            Name = "";
            Address = "";
            Description = "";
            DeviceType = ModuleTypes.Generic;
            Properties = new TsList<ModuleParameter>();
            Stores = new TsList<Store>();
        }

        public object Clone()
        {
            try
            {
                var stream = new MemoryStream();
                var formatter = new BinaryFormatter();

                formatter.Serialize(stream, this);

                stream.Position = 0;
                object obj = formatter.Deserialize(stream);
                stream.Close();

                return obj;
            }
            catch (Exception e)
            {
                // fallback to standard JSON serialization
                Console.WriteLine(e.Message);
                return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(this));
            }
        }

    }
}

