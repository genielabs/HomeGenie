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

using LiteDB;

using HomeGenie.Service;

namespace HomeGenie.Automation.Scripting
{
    /// <summary>
    /// Data and database Helper class.\n
    /// Class instance accessor: **Data**
    /// </summary>
    [Serializable]
    public class DataHelper
    {
        
        /// <summary>
        /// Open and get a LiteDatabase instance. See LiteDB website http://www.litedb.org for documentation.
        /// </summary>
        /// <returns>LiteDatabase</returns>
        /// <param name="fileName">The database file name.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        ///     var db = Data.LiteDb("my_data.db");
        ///     ...
        /// </code></example>
        public LiteDatabase LiteDb(string fileName)
        {
            return new LiteDatabase(fileName);
        }

    }
}
