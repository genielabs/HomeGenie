/*
   Copyright 2012-2025 G-Labs (https://github.com/genielabs)

   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU Affero General Public License as
   published by the Free Software Foundation, either version 3 of the
   License, or (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU Affero General Public License for more details.

   You should have received a copy of the GNU Affero General Public License
   along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: https://homegenie.it
 */

using System;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using HomeGenie.Service;
using MIG.Interfaces.HomeAutomation.Commons;

namespace HomeGenie.Data
{
    /// <summary>
    /// Module instance.
    /// </summary>
    [XmlInclude(typeof(VirtualModule))]
    [Serializable()]
    public class Module
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

        public Module Clone()
        {
            var module = new Module()
            {
                Domain = Domain,
                Address = Address,
                DeviceType = DeviceType,
                Name = Name,
                Description = Description,
                Properties = new TsList<ModuleParameter>(Properties),
                Stores = new TsList<Store>(Stores)
            };
            return module;
        }

    }
}

