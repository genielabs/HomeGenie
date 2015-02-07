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

using HomeGenie.Service.Constants;
using HomeGenie.Service.Logging;
using MIG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HomeGenie.Service.Handlers
{
    public class Statistics
    {
        private HomeGenieService homegenie;
        public Statistics(HomeGenieService hg)
        {
            homegenie = hg;
        }

        public void ProcessRequest(MIGClientRequest request, MIGInterfaceCommand migCommand)
        {
            string domain = "";
            string address = "";
            string[] filterList;
            DateTime dateStart, dateEnd;

            switch (migCommand.Command)
            {
            case "Global.CounterTotal":
                var counter = homegenie.Statistics.GetTotalCounter(migCommand.GetOption(0), 3600);
                migCommand.Response = JsonHelper.GetSimpleResponse(counter.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
                break;

            case "Global.TimeRange":
                var totalRange = homegenie.Statistics.GetDateRange();
                migCommand.Response = "[{ StartTime : '" + DateToJavascript(totalRange.TimeStart).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "', EndTime : '" + DateToJavascript(totalRange.TimeEnd).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "' }]";
                break;

            case "Database.Reset":
                homegenie.Statistics.DatabaseReset();
                break;
            case "Configuration.Get":
                // Just one at the moment.
                migCommand.Response = "[{ StatisticsUIRefreshSeconds : '" + homegenie.SystemConfiguration.HomeGenie.Statistics.StatisticsUIRefreshSeconds + "' }]";
                break;
            case "Parameter.List":
                filterList = migCommand.GetOption(0).Split(':');
                if (filterList.Length == 2)
                {
                    domain = filterList[0];
                    address = filterList[1];
                }
                migCommand.Response = "[";
                foreach (string statParameter in homegenie.Statistics.GetParametersList(domain, address))
                {
                    migCommand.Response += "	'" + statParameter + "',\n";
                }
                migCommand.Response = migCommand.Response.TrimEnd(',', '\n');
                migCommand.Response += "\n]";
                break;

            case "Parameter.Counter":
                filterList = migCommand.GetOption(1).Split(':');
                if (filterList.Length == 2)
                {
                    domain = filterList[0];
                    address = filterList[1];
                }
                //
                migCommand.Response = "[";
                migCommand.Response += "[ ";
                //
                var hoursAverage = new List<StatisticsEntry>();
                dateStart = JavascriptToDate(long.Parse(migCommand.GetOption(1)));
                dateEnd = JavascriptToDate(long.Parse(migCommand.GetOption(2)));
                hoursAverage = homegenie.Statistics.GetHourlyCounter(domain, address, migCommand.GetOption(0), 3600, dateStart, dateEnd);
                //
                for (int h = 0; h < 24; h++)
                {
                    StatisticsEntry firstEntry = null;
                    if (hoursAverage != null && hoursAverage.Count > 0)
                    {
                        firstEntry = hoursAverage.Find(se => se.TimeStart.ToLocalTime().Hour == h);
                    }
                    //
                    if (firstEntry != null)
                    {
                        double sum = 0;
                        sum = (double)(hoursAverage.FindAll(se => se.TimeStart.ToLocalTime().Hour == h).Sum(se => se.Value));
                        // date is normalized to the current date, time info is preserved from original data entry
                        var date = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + h.ToString("00") + ":00:00");
                        migCommand.Response += "[" + DateToJavascript(date).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + sum.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                    }
                    else
                    {
                        var date = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + h.ToString("00") + ":00:00");
                        migCommand.Response += "[" + DateToJavascript(date).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ",0.000],";
                    }
                }
                migCommand.Response = migCommand.Response.TrimEnd(',');
                migCommand.Response += " ]";
                migCommand.Response += "]";
                break;

            case "Parameter.StatsHour":
                filterList = migCommand.GetOption(1).Split(':');
                if (filterList.Length == 2)
                {
                    domain = filterList[0];
                    address = filterList[1];
                }
                //
                migCommand.Response = "[";
                //
                dateStart = JavascriptToDate(long.Parse(migCommand.GetOption(1)));
                dateEnd = JavascriptToDate(long.Parse(migCommand.GetOption(2)));
                var hoursAverages = new List<StatisticsEntry>[5];
                hoursAverages[0] = homegenie.Statistics.GetHourlyStats(domain, address, migCommand.GetOption(0), "Min", dateStart, dateEnd);
                hoursAverages[1] = homegenie.Statistics.GetHourlyStats(domain, address, migCommand.GetOption(0), "Max", dateStart, dateEnd);
                hoursAverages[2] = homegenie.Statistics.GetHourlyStats(domain, address, migCommand.GetOption(0), "Avg", dateStart, dateEnd);
                hoursAverages[3] = homegenie.Statistics.GetHourlyStatsToday(domain, address, migCommand.GetOption(0), "Avg");
                if (migCommand.GetOption(0).StartsWith(Properties.METER_ANY))
                {
                    hoursAverages[4] = homegenie.Statistics.GetTodayDetail(domain, address, migCommand.GetOption(0), "Sum");
                }
                else
                {
                    hoursAverages[4] = homegenie.Statistics.GetTodayDetail(domain, address, migCommand.GetOption(0), "Avg");
                }
                //
                for (int x = 0; x < 4; x++)
                {
                    migCommand.Response += "[ ";
                    for (int h = 0; h < 24; h++)
                    {
                        StatisticsEntry firstEntry = null;
                        if (hoursAverages[x] != null && hoursAverages[x].Count > 0)
                        {
                            if (migCommand.GetOption(0).StartsWith(Properties.METER_ANY))
                            {
                                firstEntry = hoursAverages[x].Find(se => se.TimeStart.ToLocalTime().Hour == h && se.Value > 0);
                            }
                            else
                            {
                                firstEntry = hoursAverages[x].Find(se => se.TimeStart.ToLocalTime().Hour == h);
                            }
                        }
                        //
                        if (firstEntry != null)
                        {
                            double sum = 0;
                            switch (x)
                            {
                                case 0:
                                    if (migCommand.GetOption(0).StartsWith(Properties.METER_ANY))
                                    {
                                        sum = (double)(hoursAverages[x].FindAll(se => se.TimeStart.ToLocalTime().Hour == h && se.Value > 0).Min(se => se.Value));
                                    }
                                    else
                                    {
                                        sum = (double)(hoursAverages[x].FindAll(se => se.TimeStart.ToLocalTime().Hour == h).Min(se => se.Value));
                                    }
                                    break;
                                case 1:
                                    sum = (double)(hoursAverages[x].FindAll(se => se.TimeStart.ToLocalTime().Hour == h).Max(se => se.Value));
                                    break;
                                case 2:
                                    if (migCommand.GetOption(0).StartsWith(Properties.METER_ANY))
                                    {
                                        sum = (double)(hoursAverages[x].FindAll(se => se.TimeStart.ToLocalTime().Hour == h && se.Value > 0).Average(se => se.Value));
                                    }
                                    else
                                    {
                                        sum = (double)(hoursAverages[x].FindAll(se => se.TimeStart.ToLocalTime().Hour == h).Average(se => se.Value));
                                    }
                                    break;
                                case 3:
                                    if (migCommand.GetOption(0).StartsWith(Properties.METER_ANY))
                                    {
                                        sum = (double)(hoursAverages[x].FindAll(se => se.TimeStart.ToLocalTime().Hour == h && se.Value > 0).Average(se => se.Value));
                                    }
                                    else
                                    {
                                        sum = (double)(hoursAverages[x].FindAll(se => se.TimeStart.ToLocalTime().Hour == h).Average(se => se.Value));
                                    }
                                    break;
                            }
                            // date is normalized to the current date, time info is preserved from original data entry
                            var date = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + h.ToString("00") + ":00:00");
                            migCommand.Response += "[" + DateToJavascript(date).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + sum.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                        }
                        else
                        {
                            var date = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + h.ToString("00") + ":00:00");
                            migCommand.Response += "[" + DateToJavascript(date).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ",0.000],";
                        }
                    }
                    migCommand.Response = migCommand.Response.TrimEnd(',');
                    migCommand.Response += " ],";

                }
                //
                migCommand.Response += "[ ";
                foreach (var entry in hoursAverages[4])
                {
                    var date = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + entry.TimeStart.ToLocalTime().ToString("HH:mm:ss.ffffff"));
                    migCommand.Response += "[" + DateToJavascript(date).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + entry.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                }
                migCommand.Response = migCommand.Response.TrimEnd(',');
                migCommand.Response += " ]";
                //
                migCommand.Response += "]";
                break;
            }

        }

        private DateTime JavascriptToDate(long timestamp)
        {
            return new DateTime((timestamp * 10000) + 621355968000000000);
        }

        private double DateToJavascript(DateTime date)
        {
            return ((date.Ticks - 621355968000000000) / 10000D);
        }

    }
}
