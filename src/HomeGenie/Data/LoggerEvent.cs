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

namespace HomeGenie.Data
{
    [Serializable()]
    public class LoggerEvent
    {
        public LoggerEvent()
        {
            // default constructor
        }

        public LoggerEvent(Module module, ModuleParameter parameter)
        {
            this.Domain = module.Domain;
            this.Address = module.Address;
            this.Description = module.Name;
            this.Parameter = parameter.Name;
            this.Value = parameter.GetData(); // get the raw object value
            this.Date = parameter.UpdateTime;
        }
        public string Domain { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public string Parameter { get; set; }
        public object Value { get; set; }
        public DateTime Date { get; set; }
    }
}
