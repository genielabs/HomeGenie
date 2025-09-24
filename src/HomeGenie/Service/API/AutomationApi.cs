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

namespace HomeGenie.Service.API
{
    public class Automation
    {
        public static class Groups
        {
            public const string LightsOn
                = "Groups.LightsOn";
            public const string LightsOff
                = "Groups.LightsOff";
        }

        // commonly used commands
        public static class Control
        {
            public const string On
                = "Control.On";
            public const string Off
                = "Control.Off";
            public const string Level
                = "Control.Level";
            public const string ColorHsb
                = "Control.ColorHsb";
            public const string Toggle
                = "Control.Toggle";
        }

        // TODO: create constants for all other API commands!!

    }

    public class Config
    {
        // TODO: ...
    }
}
