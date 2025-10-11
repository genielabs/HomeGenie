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

namespace HomeGenie.Automation
{
    public class ProgramFeature
    {
        public string FieldType { get; set; }
        //
        public string ForDomains { get; set; }
        public string ForTypes { get; set; }
        //
        public string Property { get; set; }
        public string Description { get; set; }

        public ProgramFeature()
        {
            FieldType = "checkbox";
            ForDomains = "";
            ForTypes = "";
            Property = "";
            Description = "";
        }
    }
}
