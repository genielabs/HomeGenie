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
using System.IO;
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
        HomeGenieService homegenie = null;
        int myProgramId;

        public DataHelper(HomeGenieService hg, int programId)
        {
            homegenie = hg;
            myProgramId = programId;
        }

        /// <summary>
        /// Gets the path of program's data folder.
        /// </summary>
        /// <param name="fixedName">Get a shareable folder with the given fixed name.</param>
        /// <returns></returns>
        public string GetFolder(string fixedName = null)
        {
            string dataFolder = Path.Combine(Utility.GetDataFolder(), "programs", String.IsNullOrEmpty(fixedName) ? myProgramId.ToString() : fixedName);
            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }
            return Utility.GetRelativePath(AppDomain.CurrentDomain.BaseDirectory, dataFolder);
        }
        
        /// <summary>
        /// Opens and gets a LiteDatabase instance. See LiteDB website http://www.litedb.org for documentation.
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
            if (!fileName.EndsWith(".db"))
            {
                fileName += ".db";
            }
            if (Path.GetFileNameWithoutExtension(fileName) + ".db" != fileName)
            {
                throw new ArgumentException("Invalid database name");
            }
            return new LiteDatabase(Path.Combine(GetFolder(), fileName));
        }

        /// <summary>
        /// Sets additional file or folder to be added to the system backup file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool AddToSystemBackup(string path)
        {
            var programBlock = homegenie.ProgramManager.GetProgram(myProgramId);
            if (programBlock != null)
            {
                path = Utility.GetRelativePath(AppDomain.CurrentDomain.BaseDirectory, path);
                if (!programBlock.BackupFiles.Exists(bf => bf == path))
                {
                    programBlock.BackupFiles.Add(path);
                    return true;
                } 
            }
            return false;
        }

        /// <summary>
        /// Removes an additional file or folder from the system backup file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool RemoveFromSystemBackup(string path)
        {
            path = Utility.GetRelativePath(AppDomain.CurrentDomain.BaseDirectory, path);
            var programBlock = homegenie.ProgramManager.GetProgram(myProgramId);
            return programBlock != null && programBlock.BackupFiles.Remove(path);
        }
    }
}
