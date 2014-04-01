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
using System.Linq;
using System.Text;

using HomeGenie.Data;
using System.Reflection;
using System.Diagnostics;

namespace HomeGenie.Service.Logging
{

    /// <summary>
    /// A Logging class implementing the Singleton pattern and an internal Queue to be flushed perdiodically
    /// </summary>
    public class SystemLogger : IDisposable
    {
        private static SystemLogger instance;
        private static Queue<LogEntry> logQueue;
        private static int maxLogAge = (60 * 60 * 24) * 1; // one day
        private static int queueSize = 50;
        private static FileStream logStream;
        private static StreamWriter logWriter;
        private static DateTime lastFlushed = DateTime.Now;

        /// <summary>
        /// Private constructor to prevent instance creation
        /// </summary>
        public SystemLogger()
        {
        }

        /// <summary>
        /// An LogWriter instance that exposes a single instance
        /// </summary>
        public static SystemLogger Instance
        {
            get
            {
                // If the instance is null then create one and init the Queue
                if (instance == null)
                {
                    instance = new SystemLogger();
                    logQueue = new Queue<LogEntry>();
                }
                return instance;
            }
        }

        /// <summary>
        /// The single instance method that writes to the log file
        /// </summary>
        /// <param name="message">The message to write to the log</param>
        public void WriteToLog(LogEntry logEntry)
        {
            // Lock the queue while writing to prevent contention for the log file
            logQueue.Enqueue(logEntry);
            // If we have reached the Queue Size then flush the Queue
            if (logQueue.Count >= queueSize || DoPeriodicFlush())
            {
                FlushLog();
            }
        }

        private bool DoPeriodicFlush()
        {
            var logAge = DateTime.Now - lastFlushed;
            if (logAge.TotalSeconds >= maxLogAge)
            {
                lastFlushed = DateTime.Now;
                //TODO: rename file with timestamp, compress it and open a new one
                // or simply keep max 2 days renaming old one to <logfile>.old
                CloseLog();
                //
                var assembly = Assembly.GetExecutingAssembly();
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                string logFile = assembly.ManifestModule.Name.ToLower().Replace(".exe", ".log");
                string logPath = Path.Combine(logDir, logFile);
                string logFileBackup = logPath + ".bak";
                if (File.Exists(logFileBackup))
                {
                    File.Delete(logFileBackup);
                }
                File.Move(logPath, logFileBackup);
                //
                OpenLog();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Flushes the Queue to the physical log file
        /// </summary>
        public void FlushLog()
        {
            try
            {
                while (logQueue.Count > 0)
                {
                    var entry = logQueue.Dequeue();
                    logWriter.WriteLine(entry.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: LogWriter could not flush log - " + e.Message + "\n" + e.StackTrace);
            }
        }

        public void OpenLog()
        {
            CloseLog();
            //
            var assembly = Assembly.GetExecutingAssembly();
            var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = versionInfo.FileVersion;
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
            string logFile = assembly.ManifestModule.Name.ToLower().Replace(".exe", ".log");
            string logPath = Path.Combine(logDir, logFile);
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            logStream = File.Open(logPath, FileMode.Append, FileAccess.Write);
            logWriter = new StreamWriter(logStream);
            logWriter.WriteLine("#Version: 1.0");
            logWriter.WriteLine("#Software: " + assembly.ManifestModule.Name.Replace(".exe", "") + " " + version);
            logWriter.WriteLine("#Start-Date: " + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz"));
            logWriter.WriteLine("#Fields: datetime\tsource-domain\tsource-id\tdescription\tproperty\tvalue\n");
            logQueue.Clear();
        }

        public void CloseLog()
        {
            if (IsLogEnabled)
            {
                FlushLog();
                logWriter.Close();
                logWriter = null;
                logStream.Close();
                logStream = null;
            }
        }

        public bool IsLogEnabled
        {
            get { return (logStream != null && logWriter != null); }
        }

        public void Dispose()
        {
            if (instance != null)
            {
                CloseLog();
            }
            instance = null;
        }
    }

}
