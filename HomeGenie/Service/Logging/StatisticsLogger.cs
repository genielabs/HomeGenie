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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Timers;

using SQLite;

using HomeGenie.Data;
using HomeGenie.Service.Constants;

namespace HomeGenie.Service.Logging
{
    public class StatisticsEntry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public DateTime TimeStart { get; set; }
        public DateTime TimeEnd { get; set; }
        [Indexed]
        public string Domain { get; set; }
        public string Address { get; set; }
        public string Parameter { get; set; }
        public double Value { get; set; }
        public string CustomData { get; set; }
    }

    public class StatisticsLogger
    {
        public const string STATISTICS_DB_FILE = "homegenie_stats.db";
        // common SQL filters
        private const string SQL_DATE_RANGE_FILTER = "(TimeStart >= ?  AND TimeStart <= ?)";
        private const string SQL_DOMAIN_ADDRESS_FILTER = " AND (Domain = ? AND Address = ?)";
 
        // only parameters listed here are actually inserted in the DB
        public static List<string> StatisticsFields = new List<string>() {
            "Sensor.",
            "Meter.",
            "PowerMonitor.",
            "Statistics."
        };
        public static bool IsValidField(string field)
        {
            bool isValid = false;
            foreach (string f in StatisticsFields)
            {
                if (field.StartsWith(f))
                {
                    isValid = true;
                    break;
                }
            }
            return isValid;
        }

        private HomeGenieService homegenie;
        private SQLiteConnection dbConnection;
        private Timer logInterval;

        private long dbSizeLimit = 2097152;
        private readonly int _statisticsTimeResolutionSeconds;

        public StatisticsLogger(HomeGenieService hg)
        {
            homegenie = hg;
            dbSizeLimit = hg.SystemConfiguration.HomeGenie.Statistics.MaxDatabaseSizeMBytes * 1024 * 1024;
            _statisticsTimeResolutionSeconds = hg.SystemConfiguration.HomeGenie.Statistics.StatisticsTimeResolutionSeconds;

        }

        /// <summary>
        /// Start this instance.
        /// </summary>
        public void Start()
        {
            OpenStatisticsDatabase();
            if (logInterval == null)
            {
                logInterval = new Timer(TimeSpan.FromSeconds(_statisticsTimeResolutionSeconds).TotalMilliseconds);
                logInterval.Elapsed += logInterval_Elapsed;
            }
            logInterval.Start();
        }

        /// <summary>
        /// Stop this instance.
        /// </summary>
        public void Stop()
        {
            if (logInterval != null)
            {
                logInterval.Elapsed -= logInterval_Elapsed;
                logInterval.Stop();
                logInterval.Dispose();
                logInterval = null;
            }
            CloseStatisticsDatabase();
        }

        /// <summary>
        /// Gets or sets the size limit.
        /// </summary>
        /// <value>The size limit.</value>
        public long SizeLimit
        {
            get { return dbSizeLimit; }
            set { dbSizeLimit = value; }
        }

        /// <summary>
        /// Resets the database.
        /// </summary>
        public void ResetDatabase()
        {
            CloseStatisticsDatabase();
            File.Delete(GetStatisticsDatabaseName());
            OpenStatisticsDatabase();
            dbConnection.CreateTable<StatisticsEntry>();
        }

        /// <summary>
        /// Gets the parameters list.
        /// </summary>
        /// <returns>The parameters list.</returns>
        /// <param name="domain">Domain.</param>
        /// <param name="address">Address.</param>
        public List<string> GetParametersList(string domain, string address)
        {
            string query = "select distinct Parameter from StatisticsEntry";
            if (!string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(address))
            {
                query += " WHERE Domain = ? AND Address = ?";
            }
            ;
            return dbConnection
                .CreateCommand(query, domain, address)
                .ExecuteQuery<StatisticsEntry>()
                .Select(x => x.Parameter).ToList();
        }
        
        /// <summary>
        /// Gets the date range.
        /// </summary>
        /// <returns>The date range.</returns>
        public StatisticsEntry GetDateRange()
        {
            var table = dbConnection.Table<StatisticsEntry>();
            var timeEnd = table.Max(x => x.TimeEnd);
            var timeStart = table.Min(x => x.TimeStart);
            return new StatisticsEntry()
            {
                TimeStart = timeStart,
                TimeEnd = timeEnd
            };
        }

        /// <summary>
        /// Gets the total counter.
        /// </summary>
        /// <returns>The total counter.</returns>
        /// <param name="parameterName">Parameter name.</param>
        /// <param name="timeScaleSeconds">Time scale seconds.</param>
        public double GetTotalCounter(string parameterName, double timeScaleSeconds)
        {
            try
            {
                string query = "select Sum(Value*( ((julianday(TimeEnd) - 2440587.5) * 86400.0)-((julianday(TimeStart) - 2440587.5) * 86400.0) )/" + timeScaleSeconds.ToString(CultureInfo.InvariantCulture) + ") as CounterValue from StatisticsEntry where Parameter = ?";
                return dbConnection
                    .CreateCommand(query, parameterName)
                    .ExecuteScalar<double>();
            } catch {
                // TODO: report/handle exception
            }
            return 0;
        }

        /// <summary>
        /// Gets the hourly counter.
        /// </summary>
        /// <returns>The hourly counter.</returns>
        /// <param name="domain">Domain.</param>
        /// <param name="address">Address.</param>
        /// <param name="parameterName">Parameter name.</param>
        /// <param name="timescaleSeconds">Timescaleseconds.</param>
        /// <param name="startDate">Start date.</param>
        /// <param name="endDate">End date.</param>
        public List<StatisticsEntry> GetHourlyCounter(
            string domain,
            string address,
            string parameterName,
            double timescaleSeconds,
            DateTime startDate, DateTime endDate
        )
        {
            string filter = "";
            if (!string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(address))
            {
                filter = SQL_DOMAIN_ADDRESS_FILTER;
            }
            string query = "select TimeStart,TimeEnd,Domain,Address,Sum(Value*( ((julianday(TimeEnd) - 2440587.5) * 86400.0) -((julianday(TimeStart) - 2440587.5) * 86400.0) )/" + timescaleSeconds.ToString(CultureInfo.InvariantCulture) + ") as Value from StatisticsEntry where Parameter = ? AND " + SQL_DATE_RANGE_FILTER + filter + " group by Domain, Address, strftime('%H', TimeStart) order by TimeStart desc;";
            return dbConnection
                .CreateCommand(query, parameterName, startDate, endDate, domain, address)
                .ExecuteQuery<StatisticsEntry>();            
        }

        /// <summary>
        /// This is for the current day's AVERAGES part: (TODAY_AVG)
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="address"></param>
        /// <param name="parameterName"></param>
        /// <param name="aggregator"></param>
        /// <returns></returns>
        public List<StatisticsEntry> GetHourlyStatsToday(
            string domain,
            string address,
            string parameterName,
            string aggregator
        )
        {
            var start = DateTime.Parse(DateTime.UtcNow.ToString("yyyy-MM-dd") + " 00:00:00.000000");
            string filter = "";
            if (!string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(address))
            {
                filter = SQL_DOMAIN_ADDRESS_FILTER;
            }
            // aggregated averages by hour
            string query = "select TimeStart,TimeEnd,Domain,Address," + aggregator + "(Value) as Value from StatisticsEntry where Parameter = ? and TimeStart >= ?" + filter + " group by Domain, Address, strftime('%H', TimeStart)  order by TimeStart asc;";
            return dbConnection
                .CreateCommand(query, parameterName, start, domain, address)
                .ExecuteQuery<StatisticsEntry>();
        }

        /// <summary>
        /// Gets the today detail.
        /// </summary>
        /// <returns>The today detail.</returns>
        /// <param name="domain">Domain.</param>
        /// <param name="address">Address.</param>
        /// <param name="parameterName">Parameter name.</param>
        /// <param name="aggregator">Aggregator.</param>
        public List<StatisticsEntry> GetTodayDetail(
            string domain,
            string address,
            string parameterName,
            string aggregator = "Avg"
        )
        {
            var start = DateTime.Parse(DateTime.UtcNow.ToString("yyyy-MM-dd") + " 00:00:00.000000");
            string filter = "";
            string groupBy = "";
            if (!string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(address))
            {
                // detailed module stats. We set our own aggregator. (Detailed red line in chart)
                filter = SQL_DOMAIN_ADDRESS_FILTER;
                aggregator = "Value";
            }
            else
            {
                // aggregated averages by hour
                if (!string.IsNullOrEmpty(aggregator))
                {
                    aggregator = aggregator + "(Value)";
                }
                groupBy = " group by TimeStart";
            }
            string query = "select TimeStart,TimeEnd,Domain,Address," + aggregator + " as Value from StatisticsEntry where Parameter = ? AND TimeStart >= ?" + filter + groupBy + " order by TimeStart asc;";
            return dbConnection
                .CreateCommand(query, parameterName, start, domain, address)
                .ExecuteQuery<StatisticsEntry>();
        }

        /// <summary>
        /// This is for the overall AVERAGES part: (MIN), (MAX), (AVG)
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="address"></param>
        /// <param name="parameterName"></param>
        /// <param name="aggregator"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public List<StatisticsEntry> GetHourlyStats(
            string domain,
            string address,
            string parameterName,
            string aggregator,
            DateTime startDate, DateTime endDate
        )
        {
            string filter = "";
            if (!string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(address))
            {
                filter = SQL_DOMAIN_ADDRESS_FILTER;
            }
            string query = "";
            if (aggregator != "")
            {
                if(aggregator == "All")
                    query = "select Id,TimeStart,TimeEnd,Domain,Address,Parameter,Value,CustomData from StatisticsEntry where Parameter = ? AND " + SQL_DATE_RANGE_FILTER + filter + " order by CustomData, Domain, Address, TimeStart asc;";
                else
                    query = "select Id,TimeStart,TimeEnd,Domain,Address,CustomData," + aggregator + "(Value) as Value from StatisticsEntry where Parameter = ? AND " + SQL_DATE_RANGE_FILTER + filter + " group by Domain, Address, strftime('%H', TimeStart)  order by TimeStart asc;";
            }
            else
                query = "select Id,TimeStart,TimeEnd,Domain,Address,Parameter,Value,CustomData from StatisticsEntry where Parameter = ? AND " + SQL_DATE_RANGE_FILTER + filter + " order by TimeStart asc;";

            return dbConnection
                .CreateCommand(query, parameterName, startDate, endDate, domain, address)
                .ExecuteQuery<StatisticsEntry>();
        }

        /// <summary>
        /// Remove statistics data for a given parameter at the specified time.
        /// </summary>
        /// <returns>The stat.</returns>
        /// <param name="occurrenceTs">Date of occurrence to remove.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        public void DeleteData(DateTime occurrenceTs, string parameterName)
        {
            string query = "DELETE FROM StatisticsEntry WHERE (TimeStart BETWEEN '"+DateTimeToSQLite(occurrenceTs.AddMilliseconds(-500))+"' AND '"+DateTimeToSQLite(occurrenceTs.AddMilliseconds(500))+"')";
            dbConnection
                .CreateCommand(query).ExecuteNonQuery();
        }

        /// <summary>
        /// Opens the statistics database.
        /// </summary>
        /// <returns><c>true</c>, if statistics database was opened, <c>false</c> otherwise.</returns>
        internal bool OpenStatisticsDatabase()
        {
            bool success = false;
            if (dbConnection == null)
            {
                try
                {
                    dbConnection = new SQLiteConnection(GetStatisticsDatabaseName());
                    success = true;
                }
                catch (Exception ex)
                {
                    HomeGenieService.LogError(
                        Domains.HomeAutomation_HomeGenie, 
                        "Service.StatisticsLogger", 
                        "Database Error", 
                        "Exception.StackTrace", 
                        String.Format("{0}: {1}", ex.Message, ex.StackTrace)
                    );
                }
            }
            return success;
        }

        /// <summary>
        /// Closes the statistics database.
        /// </summary>
        internal void CloseStatisticsDatabase()
        {
            if (dbConnection != null)
            {
                dbConnection.Close();
                dbConnection.Dispose();
                dbConnection = null;
            }
        }

        /// <summary>
        /// Gets the name of the statistics database.
        /// </summary>
        /// <returns>The statistics database name.</returns>
        private string GetStatisticsDatabaseName()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, StatisticsLogger.STATISTICS_DB_FILE);
        }

        private string DateTimeToSQLite(DateTime datetime)
        {
            return datetime.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
        }

        private void logInterval_Elapsed(object sender, ElapsedEventArgs eventArgs)
        {
            var end = DateTime.UtcNow;
            var modules = (TsList<Module>)homegenie.Modules; //.DeepClone();
            for (int m = 0; m < modules.Count; m++)
            {
                var module = modules[m];
                for (int p = 0; p < module.Properties.Count; p++)
                {
                    var parameter = module.Properties[p];
                    if (parameter.Statistics.Values.Count > 0)
                    {
                        var values = parameter.Statistics.Values.FindAll(sv => (sv.Timestamp.Ticks <= end.Ticks && sv.Timestamp.Ticks > parameter.Statistics.LastProcessedTimestamp.Ticks));
                        if (values.Count > 0)
                        {
                            double average = (values.Sum(d => d.Value) / values.Count);
                            try
                            {
                                string dbName = GetStatisticsDatabaseName();
                                var fileInfo = new FileInfo(dbName);
                                if (fileInfo.Length > dbSizeLimit)
                                {
                                    // Delete oldest 24 hours of data
                                    var dateRange = GetDateRange();
                                    dbConnection.CreateCommand(
                                        "DELETE FROM StatisticsEntry WHERE TimeStart < ?", DateTimeToSQLite(dateRange.TimeStart.AddHours(24))
                                    ).ExecuteNonQuery();
                                    dbConnection
                                        .CreateCommand("VACUUM")
                                        .ExecuteNonQuery();
                                }

                                var statEntry = new StatisticsEntry()
                                {
                                    TimeStart = parameter.Statistics.LastProcessedTimestamp,
                                    TimeEnd = end,
                                    Domain = module.Domain,
                                    Address = module.Address,
                                    Parameter = parameter.Name,
                                    Value = average,
                                    CustomData = module.Name
                                };
                                dbConnection.Insert(statEntry);
                            }
                            catch (Exception ex)
                            {
                                HomeGenieService.LogError(
                                    Domains.HomeAutomation_HomeGenie, 
                                    "Service.StatisticsLogger", 
                                    "Database Error", 
                                    "Exception.StackTrace", 
                                    String.Format("{0}: {1}", ex.Message, ex.StackTrace)
                                );
                                // try close/reopen (perhaps some locking issue)
                                CloseStatisticsDatabase();
                                OpenStatisticsDatabase();
                            }
                            //
                            // reset statistics history sample
                            //
                            parameter.Statistics.LastProcessedTimestamp = end;
                            parameter.Statistics.Values.Clear();
                        }
                    }
                }
            }
        }

    }
}
