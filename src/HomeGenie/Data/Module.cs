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

