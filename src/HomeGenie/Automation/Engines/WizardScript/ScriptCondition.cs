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

namespace HomeGenie.Automation.Engines.WizardScript
{
    [Serializable()]
    public enum ConditionType
    {
        None = 0, // unused
        OnSwitchTrue,
        OnSwitchFalse,
        Once,
        OnTrue,
        OnFalse
    }

    [Serializable()]
    public class ScriptCondition
    {
        public string Domain { get; set; }
        public string Target { get; set; }
        public string Property { get; set; }
        public ComparisonOperator ComparisonOperator { get; set; }
        public string ComparisonValue { get; set; }
    }
}
