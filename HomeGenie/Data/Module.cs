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

using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using HomeGenie.Service;

namespace HomeGenie.Data
{
    [Serializable()]
    public class Module : ICloneable
    {
        public string Name { get; set; }
        public string Description { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Types Type { get; set; } //physical control type (on/off, 0-100, Hot/Cold, InputSensor, etc.)
        [JsonConverter(typeof(StringEnumConverter))]
        public DeviceTypes DeviceType { get; set; } //will indicate actual device (lamp, fan, dimmer light, etc.)

        // location in actual physical Control-topology
        public string Domain { get; set; } // only Domain is used. Interface should be used instead?
        //public string Interface { get; set; }
        public string Address { get; set; }
        //
        public TsList<ModuleParameter> Properties { get; set; }
        //
        public string RoutingNode { get; set; } // "<ip>:<port>" || ""
        //
        public Module()
        {
            Name = "";
            Address = "";
            Description = "";
            Type = Types.Generic;
            DeviceType = DeviceTypes.Generic;
            Properties = new TsList<ModuleParameter>();
            RoutingNode = "";
        }

        public object Clone()
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();

            bf.Serialize(ms, this);

            ms.Position = 0;
            object obj = bf.Deserialize(ms);
            ms.Close();

            return obj;
        }

        public enum Types
        {
            Generic = -1,
            BinarySwitch,
            MultiLevelSwitch,
            Thermostat,
            InputSensor
        }

        public enum DeviceTypes
        {
            Generic = -1,
            Program,
            Switch,
            Light,
            Dimmer,
            Sensor,
            Temperature,
            Siren,
            Fan,
            Thermostat,
            Shutter,
            DoorWindow
            //siren, alarm, motion sensor, door sensor, thermal sensor, etc.
        }

    }
}

