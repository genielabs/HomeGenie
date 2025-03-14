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
 *     Project Homepage: https://homegenie.it
 */

using System;
using System.Collections.Generic;

namespace HomeGenie.Data
{
    [Serializable()]
    public class Group
    {
        public string Name { get; set; }
        public string Wallpaper { get; set; }
        //
        public List<ModuleReference> Modules { get; set; }
        //
        public Group()
        {
            Name = "";
            Wallpaper = "";
            Modules = new List<ModuleReference>();
        }
    }
}

