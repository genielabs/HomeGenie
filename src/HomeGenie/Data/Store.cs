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

using HomeGenie.Service;
using HomeGenie.Data;

namespace HomeGenie
{
    [Serializable()]
    public class Store
    {
        public string Name;
        public string Description;
        public TsList<ModuleParameter> Data;

        public Store()
        {
            Name = "";
            Description = "";
            Data = new TsList<ModuleParameter>();
        }

        public Store(string name, string description = "")
        {
            Name = name;
            Description = description;
            Data = new TsList<ModuleParameter>();
        }
    }
}
