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
