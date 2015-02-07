﻿/*
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

using HomeGenie.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

using System.Data.SQLite;
using System.IO;
using HomeGenie.Service.Constants;

namespace HomeGenie.Service.Logging
{
    public class StatisticsEntry
    {
        public DateTime TimeStart;
        public DateTime TimeEnd;
        public string Domain;
        public string Address;
        public double Value;
    }


    public class StatisticsLogger
    {
        public static List<string> StatisticsFields = new List<string>() {
            "Conditions.",
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

        private Timer logInterval;
        private HomeGenieService homegenie;
        private SQLiteConnection dbConnection;

        //private object dbLock = new object();
        private readonly long dbSizeLimit = 5242880 * 2;
        //private static int STATISTICS_TIME_RESOLUTION_MINUTES = 5;
        private readonly int _statisticsTimeResolutionSeconds = 5 * 60;

        public StatisticsLogger(HomeGenieService hg)
        {
            homegenie = hg;
            dbSizeLimit = hg.SystemConfiguration.HomeGenie.Statistics.MaxDatabaseSizeMBytes * 1024 * 1024;
            _statisticsTimeResolutionSeconds = hg.SystemConfiguration.HomeGenie.Statistics.StatisticsTimeResolutionSeconds;

            logInterval = new Timer(TimeSpan.FromSeconds(_statisticsTimeResolutionSeconds).TotalMilliseconds);
            logInterval.Elapsed += logInterval_Elapsed;
        }

        public void Start()
        {
            logInterval.Start();
            OpenStatisticsDatabase();
        }

        public void Stop()
        {
            logInterval.Stop();
            CloseStatisticsDatabase();
        }

        public void DatabaseReset()
        {
            ResetStatisticsDatabase();
        }

        public List<string> GetParametersList(string domain, string address)
        {
            var parameterList = new List<string>();
            //lock (dbLock)
            {
                var dbCommand = dbConnection.CreateCommand();
                string query = "select distinct Parameter from ValuesHist";
                if (!string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(address))
                {
                    query += " WHERE Domain=@domain AND Address=@address ";
                    dbCommand.Parameters.Add(new SQLiteParameter("@domain", domain));
                    dbCommand.Parameters.Add(new SQLiteParameter("@address", address));
                }
                dbCommand.CommandText = query;
                var reader = dbCommand.ExecuteReader();
                while (reader.Read())
                {
                    parameterList.Add(reader.GetString(0));
                }
                reader.Close();
            }
            return parameterList;
        }

        public StatisticsEntry GetDateRange()
        {
            var start = DateTime.UtcNow;
            var end = DateTime.UtcNow;
            //lock (dbLock)
            {
                var dbCommand = dbConnection.CreateCommand();
                string query = "select min(TimeStart),max(TimeEnd) from ValuesHist";
                dbCommand.CommandText = query;
                try
                {
                    var reader = dbCommand.ExecuteReader();
                    if (reader.Read())
                    {
                        start = DateTime.Parse(reader.GetString(0));
                        end = DateTime.Parse(reader.GetString(1));
                    }
                    reader.Close();
                }
                catch (Exception)
                {
                    // TODO: add error logging
                }

            }
            return new StatisticsEntry() { TimeStart = start, TimeEnd = end };
        }

        public double GetTotalCounter(string parameterName, double timeScaleSeconds)
        {
            double value = 0;
            try
            {
                var dbCommand = dbConnection.CreateCommand();
                // TODO: protect against sqlinjection ?
                string query = "select Sum(AverageValue*( ((julianday(TimeEnd) - 2440587.5) * 86400.0) -((julianday(TimeStart) - 2440587.5) * 86400.0) )/" + timeScaleSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + ") as CounterValue from ValuesHist where Parameter = @parameterName;";
                dbCommand.Parameters.Add(new SQLiteParameter("@parameterName", parameterName));
                dbCommand.CommandText = query;
                var reader = dbCommand.ExecuteReader();
                //
                if (reader.Read())
                {
                    try
                    {
                        value = (double)reader.GetFloat(0);
                    }
                    catch
                    {
                    }
                }
                //
                reader.Close();
            } catch {
                // TODO: report/handle exception
            }
            return value;
        }

        public List<StatisticsEntry> GetHourlyCounter(
            string domain,
            string address,
            string parameterName,
            double timescaleseconds,
            DateTime startDate, DateTime endDate
        )
        {
            var values = new List<StatisticsEntry>();
            //lock (dbLock)
            {
                var dbCommand = dbConnection.CreateCommand();
                string filter = "";
                if (!string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(address))
                {
                    filter= " Domain=@domain AND Address=@address and ";
                    dbCommand.Parameters.Add(new SQLiteParameter("@domain", domain));
                    dbCommand.Parameters.Add(new SQLiteParameter("@address", address));
                }
                string query = "select TimeStart,TimeEnd,Domain,Address,Sum(AverageValue*( ((julianday(TimeEnd) - 2440587.5) * 86400.0) -((julianday(TimeStart) - 2440587.5) * 86400.0) )/" + timescaleseconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + ") as CounterValue from ValuesHist where " + filter + " Parameter = @parameterName AND " + GetParameterizedDateRangeFilter(ref dbCommand,startDate, endDate) + " group by Domain, Address, strftime('%H', TimeStart) order by TimeStart desc;";
                dbCommand.Parameters.Add(new SQLiteParameter("@parameterName", parameterName));
                dbCommand.CommandText = query;
                var reader = dbCommand.ExecuteReader();
                //
                while (reader.Read())
                {
                    var entry = new StatisticsEntry();
                    entry.TimeStart = DateTime.Parse(reader.GetString(0));
                    entry.TimeEnd = DateTime.Parse(reader.GetString(1));
                    entry.Domain = reader.GetString(2);
                    entry.Address = reader.GetString(3);
                    entry.Value = 0;
                    try
                    {
                        entry.Value = (double)reader.GetFloat(4);
                    }
                    catch
                    {
                        var value = reader.GetValue(4);
                        if (value != DBNull.Value && value != null) double.TryParse(
                                reader.GetString(4),
                                out entry.Value
                            );
                    }
                    //
                    values.Add(entry);
                }
                //
                reader.Close();
            }
            return values;
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
            var values = new List<StatisticsEntry>();
            //lock (dbLock)
            {
                var dbCommand = dbConnection.CreateCommand();
                string filter = "";
                var start = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " 00:00:00.000000");
                dbCommand.Parameters.Add(new SQLiteParameter("@start", start.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffff") ));
                //if (domain != "" && address != "") filter = "Domain ='" + domain + "' and Address = '" + address + "' and ";
                if (!string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(address))
                {
                    filter = " Domain=@domain AND Address=@address and ";
                    dbCommand.Parameters.Add(new SQLiteParameter("@domain", domain));
                    dbCommand.Parameters.Add(new SQLiteParameter("@address", address));
                }
                dbCommand.Parameters.Add(new SQLiteParameter("@parameterName", parameterName));
                // aggregated averages by hour
                string query = "select TimeStart,TimeEnd,Domain,Address," + aggregator + "(AverageValue) as Value from ValuesHist where " + filter + "Parameter = @parameterName and TimeStart >= @start group by Domain, Address, strftime('%H', TimeStart)  order by TimeStart asc;";
                dbCommand.CommandText = query;
                SQLiteDataReader reader = dbCommand.ExecuteReader();
                //
                while (reader.Read())
                {
                    var entry = new StatisticsEntry();
                    entry.TimeStart = DateTime.Parse(reader.GetString(0));
                    entry.TimeEnd = DateTime.Parse(reader.GetString(1));
                    entry.Domain = reader.GetString(2);
                    entry.Address = reader.GetString(3);
                    entry.Value = 0;
                    try
                    {
                        entry.Value = (double)reader.GetFloat(4);
                    }
                    catch
                    {
                        var value = reader.GetValue(4);
                        if (value != DBNull.Value && value != null) double.TryParse(
                                reader.GetString(4),
                                out entry.Value
                            );
                    }
                    //
                    values.Add(entry);
                }
                //
                reader.Close();
            }
            return values;
        }

        public List<StatisticsEntry> GetTodayDetail(
            string domain,
            string address,
            string parameterName,
            string aggregator = "Avg"
        )
        {
            var values = new List<StatisticsEntry>();
            //lock (dbLock)
            {
                var dbCommand = dbConnection.CreateCommand();
                string filter = "";
                string groupBy = "";
                var start = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " 00:00:00.000000");
                dbCommand.Parameters.Add(new SQLiteParameter("@start", start.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffff"))); 
                
                if (!string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(address))
                {
                    // detailed module stats. We set our own aggregator. (Detailed red line in chart)
                    filter = " Domain=@domain AND Address=@address and ";
                    dbCommand.Parameters.Add(new SQLiteParameter("@domain", domain));
                    dbCommand.Parameters.Add(new SQLiteParameter("@address", address));
                    aggregator = "AverageValue";
                }
                else
                {
                    // aggregated averages by hour
                    if (!string.IsNullOrEmpty(aggregator))
                    {
                        aggregator = aggregator + "(AverageValue)";
                    }
                    groupBy = "group by TimeStart";
                }
                string query = "select TimeStart,TimeEnd,Domain,Address," + aggregator + " as Value from ValuesHist where " + filter + " Parameter = @parameterName AND TimeStart >= @start " + groupBy + " order by TimeStart asc;";
                dbCommand.Parameters.Add(new SQLiteParameter("@parameterName", parameterName));
                dbCommand.CommandText = query;

                /*
                //if (domain != "" && address != "") filter = "Domain ='" + domain + "' and Address = '" + address + "' and ";
                // aggregated averages by hour
                string q = "select TimeStart,TimeEnd,Domain,Address," + aggregator + "(AverageValue) as Value from ValuesHist where " + filter + "Parameter = @parameterName and TimeStart >= @start group by TimeStart order by TimeStart asc;";
                // detailed module stats
                if (domain != "" && address != "")
                {
                    q = "select TimeStart,TimeEnd,Domain,Address,AverageValue as Value from ValuesHist where " + filter + "Parameter = '" + parameterName + "' and TimeStart >= '" + start.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffff") + "' order by TimeStart asc;";
                }
                dbCommand.CommandText = q;*/

                var reader = dbCommand.ExecuteReader();
                //
                while (reader.Read())
                {
                    // If nothing is found in filter during aggregate, we get a row of all DBNulls. Skip the entry.
                    // NOTE: We got an exception before this check if HG sends a request for a param that has no results 
                    //       for the Parameter/TimeStart filter. We got single row of all DBNulls. 
                    if (reader.IsDBNull(0))
                    {
                        continue;
                    }
                    var entry = new StatisticsEntry();
                    entry.TimeStart = DateTime.Parse(reader.GetString(0));
                    entry.TimeEnd = DateTime.Parse(reader.GetString(1));
                    entry.Domain = reader.GetString(2);
                    entry.Address = reader.GetString(3);
                    entry.Value = 0;
                    try
                    {
                        entry.Value = (double)reader.GetFloat(4);
                    }
                    catch
                    {
                        var value = reader.GetValue(4);
                        if (value != DBNull.Value && value != null) double.TryParse(
                                reader.GetString(4),
                                out entry.Value
                            );
                    }
                    //
                    values.Add(entry);
                }
                //
                reader.Close();
            }
            return values;
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
            var values = new List<StatisticsEntry>();
            //lock (dbLock)
            {
                var dbCommand = dbConnection.CreateCommand();
                string filter = "";

                if (!string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(address))
                {
                    filter = " Domain=@domain AND Address=@address and ";
                    dbCommand.Parameters.Add(new SQLiteParameter("@domain", domain));
                    dbCommand.Parameters.Add(new SQLiteParameter("@address", address));
                }
                string query = "select TimeStart,TimeEnd,Domain,Address," + aggregator + "(AverageValue) as Value from ValuesHist where " + filter + " Parameter = @parameterName AND " + GetParameterizedDateRangeFilter(ref dbCommand, startDate, endDate) + " group by Domain, Address, strftime('%H', TimeStart)  order by TimeStart asc;";
                dbCommand.Parameters.Add(new SQLiteParameter("@parameterName", parameterName));

                //if (domain != "" && address != "") filter = "Domain ='" + domain + "' and Address = '" + address + "' and ";
                //string query = "select TimeStart,TimeEnd,Domain,Address," + aggregator + "(AverageValue) as Value from ValuesHist where " + filter + "Parameter = '" + parameterName + "' AND " + GetDateRangeFilter(startDate, endDate) + " group by Domain, Address, strftime('%H', TimeStart)  order by TimeStart asc;";
                dbCommand.CommandText = query;
                var reader = dbCommand.ExecuteReader();
                //
                while (reader.Read())
                {
                    var entry = new StatisticsEntry();
                    entry.TimeStart = DateTime.Parse(reader.GetString(0));
                    entry.TimeEnd = DateTime.Parse(reader.GetString(1));
                    entry.Domain = reader.GetString(2);
                    entry.Address = reader.GetString(3);
                    entry.Value = 0;
                    try
                    {
                        entry.Value = (double)reader.GetFloat(4);
                    }
                    catch
                    {
                        var value = reader.GetValue(4);
                        if (value != DBNull.Value && value != null) double.TryParse(
                                reader.GetString(4),
                                out entry.Value
                            );
                    }
                    //
                    values.Add(entry);
                }
                //
                reader.Close();
            }
            return values;
        }

        private bool OpenStatisticsDatabase()
        {
            bool success = false;
            //lock (dbLock)
            {
                try
                {
                    dbConnection = new SQLiteConnection("URI=file:" + GetStatisticsDatabaseName());
                    dbConnection.Open();
                    success = true;
                }
                catch (Exception)
                {
                    // TODO: add error logging
                }
            }
            return success;
        }

        private void ResetStatisticsDatabase()
        {
            //lock (dbLock)
            {
                var dbCommand = dbConnection.CreateCommand();
                dbCommand.CommandText = "DELETE FROM ValuesHist";
                dbCommand.ExecuteNonQuery();
                dbCommand.CommandText = "VACUUM";
                dbCommand.ExecuteNonQuery();
            }
        }
        /// <summary>
        /// Removes older values to keep DB size within configured size limit. Currently just cuts out last half of dates.
        /// </summary>
        private void CleanOldValuesFromStatisticsDatabase()
        {
            // + NUM_DAYS = Find number of days stored in DB.
            var stat = GetDateRange();
            int numDays = DateTime.Now.Subtract(stat.TimeStart).Days;
            int numDaysRemove = (int)Math.Floor(numDays / 2d);
            // + NUM_RECORDS = Get number of records.
            // + NUM_RECORDS_PER_DAY = Divide number of records by days to get records per day. (Not needed yet)

            // +++ We ultiumately want to shrink DB size by 50% or so...
            //     Just divide NUM_DAYS by 2. That should handle most cases.
            CleanOldValuesFromStatisticsDatabase(numDaysRemove);

        }

        /// <summary>
        /// Removes older values to keep DB size within configured size limit.
        /// </summary>
        /// <param name="numberOfDays">Records older than this number of days are removed.</param>
        private void CleanOldValuesFromStatisticsDatabase(int numberOfDays)
        {
            if (numberOfDays > 0)
            {
                //lock (dbLock)
                {
                    var dbCommand = dbConnection.CreateCommand();
                    dbCommand.CommandText = "DELETE FROM ValuesHist WHERE TimeStart < DATEADD(dd,-" + numberOfDays + ",GETDATE());";
                    dbCommand.ExecuteNonQuery();
                    dbCommand.CommandText = "VACUUM";
                    dbCommand.ExecuteNonQuery();

                    HomeGenieService.LogEvent(
                                       Domains.HomeAutomation_HomeGenie,
                                       "Service.StatisticsLogger",
                                       "Cleaned old values from database.",
                                       "DayThreshold",
                                       numberOfDays.ToString()
                                   );
                }
            }
        }
        private void CloseStatisticsDatabase()
        {
            //lock (dbLock)
            {
                dbConnection.Close();
            }
        }

        private string GetDateRangeFilter(DateTime start, DateTime end)
        {
            var d1 = DateTime.Parse(start.ToString("yyyy-MM-dd") + " 00:00:00.000000");
            var d2 = DateTime.Parse(end.ToString("yyyy-MM-dd") + " 23:59:59.999999");
            var filter = "(TimeStart >= '" + d1.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffff") + "' AND TimeStart <= '" + d2.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffff") + "')";
            return filter;
        }

        private string GetParameterizedDateRangeFilter(ref SQLiteCommand dbCommand, DateTime start, DateTime end)
        {
            var d1 = DateTime.Parse(start.ToString("yyyy-MM-dd") + " 00:00:00.000000");
            var d2 = DateTime.Parse(end.ToString("yyyy-MM-dd") + " 23:59:59.999999");
            dbCommand.Parameters.Add(new SQLiteParameter("@timeStartMin", d1.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffff")));
            dbCommand.Parameters.Add(new SQLiteParameter("@timeStartMax", d2.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffff")));
            return "(TimeStart >= @timeStartMin  AND TimeStart <= @timeStartMax)";
        }

        private string GetStatisticsDatabaseName()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "homegenie_stats.db");
        }

        private string DateTimeToSQLite(DateTime datetime)
        {
            return datetime.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
        }


        private void logInterval_Elapsed(object sender, ElapsedEventArgs eventArgs)
        {
            var end = DateTime.UtcNow;
            var modules = (TsList<Module>)homegenie.Modules; //.Clone();
            foreach (var module in modules)
            {
                foreach (var parameter in module.Properties)
                {
                    // enntry counter
                    if (parameter.Statistics.Values.Count > 0)
                    {

                        var values = parameter.Statistics.Values.FindAll(sv => (sv.Timestamp.Ticks <= end.Ticks && sv.Timestamp.Ticks > parameter.Statistics.LastProcessedTimestap.Ticks));
                        //
                        if (values.Count > 0)
                        {
                            double average = (values.Sum(d => d.Value) / values.Count);
                            //
                            //TODO: check db file age/size for archiving old data
                            //
                            string dbName = GetStatisticsDatabaseName();
                            var fileInfo = new FileInfo(dbName);
                            if (fileInfo.Length > dbSizeLimit) // 5Mb limit for stats - temporary limitations to get rid of in the future
                            {
                                ResetStatisticsDatabase();
                                // TODO: Test method below, then use that instead of rsetting whole database.
                                //CleanOldValuesFromStatisticsDatabase();
                            }
                            //
                            try
                            {
                                var dbCommand = dbConnection.CreateCommand();
                                // "TimeStart","TimeEnd","Domain","Address","Parameter","AverageValue"
                                dbCommand.CommandText = "INSERT INTO ValuesHist VALUES ('" + DateTimeToSQLite(parameter.Statistics.LastProcessedTimestap) + "','" + DateTimeToSQLite(end) + "','" + module.Domain + "','" + module.Address + "','" + parameter.Name + "'," + average.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
                                dbCommand.ExecuteNonQuery();

                            }
                            catch (Exception ex)
                            {
                                HomeGenieService.LogEvent(
                                    Domains.HomeAutomation_HomeGenie,
                                    "Service.StatisticsLogger",
                                    ex.Message,
                                    "Exception.StackTrace",
                                    ex.StackTrace
                                );
                            }
                            //
                            parameter.Statistics.LastProcessedTimestap = end;
                            parameter.Statistics.Values.Clear();

                        }

                    }
                }
            }
        }


    }
}
