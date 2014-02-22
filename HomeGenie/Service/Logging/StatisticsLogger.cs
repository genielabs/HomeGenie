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
        public static List<string> StatisticsFields = new List<string>() { "Conditions.", "Sensor.", "Meter.", "PowerMonitor.", "Statistics." };
        public static bool IsValidField(string field)
        {
            bool valid = false;
            foreach (string f in StatisticsFields)
            {
                if (field.StartsWith(f))
                {
                    valid = true;
                    break;
                }
            }
            return valid;
        }

        private Timer _loginterval;
        private HomeGenieService _homegenie;
        private SQLiteConnection _dbconnection;

        private object _dblock = new object();
        private long _dbsizelimit = 5242880 * 2;

        private static int STATISTICS_TIME_RESOLUTION_MINUTES = 5;

        public StatisticsLogger(HomeGenieService hg)
        {
            _homegenie = hg;
            _loginterval = new Timer(TimeSpan.FromMinutes(STATISTICS_TIME_RESOLUTION_MINUTES).TotalMilliseconds);
            _loginterval.Elapsed += _loginterval_Elapsed;
        }

        public void Start()
        {
            _loginterval.Start();
            _openStatisticsDatabase();
        }

        public void Stop()
        {
            _loginterval.Stop();
            _closeStatisticsDatabase();
        }

        public void DatabaseReset()
        {
            _resetStatisticsDatabase();
        }

        public List<string> GetParametersList(string domain, string address)
        {
            string filter = "";
            if (domain != "" && address != "") filter = " where Domain ='" + domain + "' and Address = '" + address + "'";
            List<string> plist = new List<string>();
            lock (_dblock)
            {
                SQLiteCommand dbcmd = _dbconnection.CreateCommand();
                string q = "select distinct Parameter from ValuesHist" + filter;
                dbcmd.CommandText = q;
                SQLiteDataReader reader = dbcmd.ExecuteReader();
                while (reader.Read())
                {
                    plist.Add(reader.GetString(0));
                }
                reader.Close();
            }
            return plist;
        }

        public StatisticsEntry GetStartDate()
        {
            DateTime startdate = DateTime.UtcNow;
            DateTime enddate = DateTime.UtcNow;
            lock (_dblock)
            {
                SQLiteCommand dbcmd = _dbconnection.CreateCommand();
                string q = "select min(TimeStart),max(TimeEnd) from ValuesHist";
                dbcmd.CommandText = q;
                try
                {
                    SQLiteDataReader reader = dbcmd.ExecuteReader();
                    if (reader.Read())
                    {
                        startdate = DateTime.Parse(reader.GetString(0));
                        enddate = DateTime.Parse(reader.GetString(1));
                    }
                    reader.Close();
                }
                catch (Exception)
                {
                    // TODO: add error logging
                }

            }
            return new StatisticsEntry() { TimeStart = startdate, TimeEnd = enddate };
        }

        public double GetTotalCounter(string param, double timescaleseconds)
        {
            double value = 0;
            lock (_dblock)
            {
                SQLiteCommand dbcmd = _dbconnection.CreateCommand();
                // TODO: protect against sqlinjection ?
                string q = "select Sum(AverageValue*( ((julianday(TimeEnd) - 2440587.5) * 86400.0) -((julianday(TimeStart) - 2440587.5) * 86400.0) )/" + timescaleseconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + ") as CounterValue from ValuesHist where Parameter = '" + param + "';";
                dbcmd.CommandText = q;
                SQLiteDataReader reader = dbcmd.ExecuteReader();
                //
                if (reader.Read())
                {
                    try
                    {
                        value = (double)reader.GetFloat(0);
                    }
                    catch { }
                }
                //
                reader.Close();
            }
            return value;
        }

        public List<StatisticsEntry> GetHourlyCounter(string domain, string address, string param, double timescaleseconds)
        {
            List<StatisticsEntry> values = new List<StatisticsEntry>();
            lock (_dblock)
            {
                SQLiteCommand dbcmd = _dbconnection.CreateCommand();
                string filter = "";
                if (domain != "" && address != "") filter = "Domain ='" + domain + "' and Address = '" + address + "' and ";
                string q = "select TimeStart,TimeEnd,Domain,Address,Sum(AverageValue*( ((julianday(TimeEnd) - 2440587.5) * 86400.0) -((julianday(TimeStart) - 2440587.5) * 86400.0) )/" + timescaleseconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + ") as CounterValue from ValuesHist where " + filter + "Parameter = '" + param + "' group by Domain, Address, strftime('%H', TimeStart) order by TimeStart desc;";
                dbcmd.CommandText = q;
                SQLiteDataReader reader = dbcmd.ExecuteReader();
                //
                while (reader.Read())
                {
                    StatisticsEntry se = new StatisticsEntry();
                    se.TimeStart = DateTime.Parse(reader.GetString(0));
                    se.TimeEnd = DateTime.Parse(reader.GetString(1));
                    se.Domain = reader.GetString(2);
                    se.Address = reader.GetString(3);
                    se.Value = 0;
                    try
                    {
                        se.Value = (double)reader.GetFloat(4);
                    }
                    catch
                    {
                        var val = reader.GetValue(4);
                        if (val != DBNull.Value && val != null) double.TryParse(reader.GetString(4), out se.Value);
                    }
                    //
                    values.Add(se);
                }
                //
                reader.Close();
            }
            return values;
        }

        public List<StatisticsEntry> GetHourlyStats24(string domain, string address, string param, string aggregator)
        {
            List<StatisticsEntry> values = new List<StatisticsEntry>();
            lock (_dblock)
            {
                SQLiteCommand dbcmd = _dbconnection.CreateCommand();
                string filter = "";
                DateTime startdate = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " 00:00:00.000000");
                if (domain != "" && address != "") filter = "Domain ='" + domain + "' and Address = '" + address + "' and ";
                // aggregated averages by hour
                string q = "select TimeStart,TimeEnd,Domain,Address," + aggregator + "(AverageValue) as Value from ValuesHist where " + filter + "Parameter = '" + param + "' and TimeStart >= '" + startdate.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffff") + "' group by Domain, Address, strftime('%H', TimeStart)  order by TimeStart asc;";
                dbcmd.CommandText = q;
                SQLiteDataReader reader = dbcmd.ExecuteReader();
                //
                while (reader.Read())
                {
                    StatisticsEntry se = new StatisticsEntry();
                    se.TimeStart = DateTime.Parse(reader.GetString(0));
                    se.TimeEnd = DateTime.Parse(reader.GetString(1));
                    se.Domain = reader.GetString(2);
                    se.Address = reader.GetString(3);
                    se.Value = 0;
                    try
                    {
                        se.Value = (double)reader.GetFloat(4);
                    }
                    catch
                    {
                        var val = reader.GetValue(4);
                        if (val != DBNull.Value && val != null) double.TryParse(reader.GetString(4), out se.Value);
                    }
                    //
                    values.Add(se);
                }
                //
                reader.Close();
            }
            return values;
        }


        public List<StatisticsEntry> GetTodayDetail(string domain, string address, string param, string aggregator = "Avg")
        {
            List<StatisticsEntry> values = new List<StatisticsEntry>();
            lock (_dblock)
            {
                SQLiteCommand dbcmd = _dbconnection.CreateCommand();
                string filter = "";
                DateTime startdate = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " 00:00:00.000000");
                if (domain != "" && address != "") filter = "Domain ='" + domain + "' and Address = '" + address + "' and ";
                // aggregated averages by hour
                string q = "select TimeStart,TimeEnd,Domain,Address," + aggregator + "(AverageValue) as Value from ValuesHist where " + filter + "Parameter = '" + param + "' and TimeStart >= '" + startdate.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffff") + "' group by TimeStart order by TimeStart asc;";
                // detailed module stats
                if (domain != "" && address != "")
                {
                    q = "select TimeStart,TimeEnd,Domain,Address,AverageValue as Value from ValuesHist where " + filter + "Parameter = '" + param + "' and TimeStart >= '" + startdate.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffff") + "' order by TimeStart asc;";
                }
                dbcmd.CommandText = q;
                SQLiteDataReader reader = dbcmd.ExecuteReader();
                //
                while (reader.Read())
                {
                    StatisticsEntry se = new StatisticsEntry();
                    se.TimeStart = DateTime.Parse(reader.GetString(0));
                    se.TimeEnd = DateTime.Parse(reader.GetString(1));
                    se.Domain = reader.GetString(2);
                    se.Address = reader.GetString(3);
                    se.Value = 0;
                    try
                    {
                        se.Value = (double)reader.GetFloat(4);
                    }
                    catch
                    {
                        var val = reader.GetValue(4);
                        if (val != DBNull.Value && val != null) double.TryParse(reader.GetString(4), out se.Value);
                    }
                    //
                    values.Add(se);
                }
                //
                reader.Close();
            }
            return values;
        }
        
        public List<StatisticsEntry> GetHourlyStats(string domain, string address, string param, string aggregator)
        {
            List<StatisticsEntry> values = new List<StatisticsEntry>();
            lock (_dblock)
            {
                SQLiteCommand dbcmd = _dbconnection.CreateCommand();
                string filter = "";
                if (domain != "" && address != "") filter = "Domain ='" + domain + "' and Address = '" + address + "' and ";
                string q = "select TimeStart,TimeEnd,Domain,Address," + aggregator + "(AverageValue) as Value from ValuesHist where " + filter + "Parameter = '" + param + "' group by Domain, Address, strftime('%H', TimeStart)  order by TimeStart asc;";
                dbcmd.CommandText = q;
                SQLiteDataReader reader = dbcmd.ExecuteReader();
                //
                while (reader.Read())
                {
                    StatisticsEntry se = new StatisticsEntry();
                    se.TimeStart = DateTime.Parse(reader.GetString(0));
                    se.TimeEnd = DateTime.Parse(reader.GetString(1));
                    se.Domain = reader.GetString(2);
                    se.Address = reader.GetString(3);
                    se.Value = 0;
                    try
                    {
                        se.Value = (double)reader.GetFloat(4);
                    }
                    catch
                    {
                        var val = reader.GetValue(4);
                        if (val != DBNull.Value && val != null) double.TryParse(reader.GetString(4), out se.Value);
                    }
                    //
                    values.Add(se);
                }
                //
                reader.Close();
            }
            return values;
        }

        private bool _openStatisticsDatabase()
        {
            bool success = false;
            lock (_dblock)
            {
                try
                {
                    _dbconnection = new SQLiteConnection("URI=file:" + _getStatisticsDatabaseName());
                    _dbconnection.Open();
                    success = true;
                }
                catch (Exception)
                {
                    // TODO: add error logging
                }
            }
            return success;
        }

        private void _resetStatisticsDatabase()
        {
            lock (_dblock)
            {
                SQLiteCommand dbcmd = _dbconnection.CreateCommand();
                dbcmd.CommandText = "DELETE FROM ValuesHist";
                dbcmd.ExecuteNonQuery();
                dbcmd.CommandText = "VACUUM";
                dbcmd.ExecuteNonQuery();
            }
        }

        private void _closeStatisticsDatabase()
        {
            lock (_dblock)
            {
                _dbconnection.Close();
            }
        }


        private string _getStatisticsDatabaseName()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "homegenie_stats.db");
        }

        private string _dateTimeSQLite(DateTime datetime)
        {
            return datetime.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
        }


        private void _loginterval_Elapsed(object sender, ElapsedEventArgs e)
        {
            DateTime logend = DateTime.UtcNow;
            TsList<Module> modules = (TsList<Module>)_homegenie.Modules; //.Clone();
            foreach (Module m in modules)
            {
                foreach (ModuleParameter mp in m.Properties)
                {
                    // enntry counter
                    if (mp.Statistics.Values.Count > 0)
                    {

                        List<ValueStatistics.StatValue> values = mp.Statistics.Values.FindAll(sv => (sv.Timestamp.Ticks <= logend.Ticks && sv.Timestamp.Ticks > mp.Statistics.LastProcessedTimestap.Ticks));
                        //
                        if (values.Count > 0)
                        {
                            TimeSpan trange = new TimeSpan(logend.Ticks - mp.Statistics.LastProcessedTimestap.Ticks);
                            double average = (values.Sum(d => d.Value) / values.Count);
                            //
                            //TODO: check db file age/size for archiving old data
                            //
                            string dbname = _getStatisticsDatabaseName();
                            FileInfo fi = new FileInfo(dbname);
                            if (fi.Length > _dbsizelimit) // 5Mb limit for stats - temporary limitations to get rid of in the future
                            {
                                _resetStatisticsDatabase();
                            }
                            //
                            try
                            {

                                SQLiteCommand dbcmd = _dbconnection.CreateCommand();

                                // "TimeStart","TimeEnd","Domain","Address","Parameter","AverageValue"
                                dbcmd.CommandText = "INSERT INTO ValuesHist VALUES ('" + _dateTimeSQLite(mp.Statistics.LastProcessedTimestap) + "','" + _dateTimeSQLite(logend) + "','" + m.Domain + "','" + m.Address + "','" + mp.Name + "'," + average.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
                                dbcmd.ExecuteNonQuery();

                            }
                            catch (Exception ex)
                            {
                                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "Service.StatisticsLogger", ex.Message, "Exception.StackTrace", ex.StackTrace);
                            }
                            //
                            mp.Statistics.LastProcessedTimestap = logend;
                            //Console.WriteLine("Average value: " + average);
                            //Console.WriteLine("Time range: " + trange.TotalSeconds);
                            mp.Statistics.Values.Clear();

                        }

                    }
                }
            }
        }


    }
}
