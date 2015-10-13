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

        public void ProcessRequest(MigClientRequest request)
        {
            var migCommand = request.Command;

            string response = "";
            string domain = "";
            string address = "";
            int domainSeparator = 0;
            DateTime dateStart, dateEnd;

            switch (migCommand.Command)
            {
            case "Global.CounterTotal":
                var counter = homegenie.Statistics.GetTotalCounter(migCommand.GetOption(0), 3600);
                request.ResponseData = new ResponseText(counter.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
                break;
            case "Global.TimeRange":
                var totalRange = homegenie.Statistics.GetDateRange();
                request.ResponseData = "{ \"StartTime\" : \"" + DateToJavascript(totalRange.TimeStart).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\", \"EndTime\" : \"" + DateToJavascript(totalRange.TimeEnd).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\" }";
                break;
            case "Database.Reset":
                homegenie.Statistics.ResetDatabase();
                break;
            case "Configuration.Get":
                // Just one at the moment.
                request.ResponseData = "{ \"StatisticsUIRefreshSeconds\" : \"" + homegenie.SystemConfiguration.HomeGenie.Statistics.StatisticsUIRefreshSeconds + "\" }";
                break;
            case "Parameter.List":
                domainSeparator = migCommand.GetOption(0).LastIndexOf(":");
                if (domainSeparator > 0)
                {
                    domain = migCommand.GetOption(0).Substring(0, domainSeparator);
                    address = migCommand.GetOption(0).Substring(domainSeparator + 1);
                }
                response = "[";
                foreach (string statParameter in homegenie.Statistics.GetParametersList(domain, address))
                {
                    response += "	\"" + statParameter + "\",\n";
                }
                response = response.TrimEnd(',', '\n');
                response += "\n]";
                request.ResponseData = response;
                break;

            case "Parameter.Counter":
                domainSeparator = migCommand.GetOption(1).LastIndexOf(":");
                if (domainSeparator > 0)
                {
                    domain = migCommand.GetOption(1).Substring(0, domainSeparator);
                    address = migCommand.GetOption(1).Substring(domainSeparator + 1);
                }
                //
                response = "[";
                response += "[ ";
                //
                var hoursAverage = new List<StatisticsEntry>();
                dateStart = JavascriptToDate(long.Parse(migCommand.GetOption(2)));
                dateEnd = JavascriptToDate(long.Parse(migCommand.GetOption(3)));
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
                        response += "[" + DateToJavascript(date).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + sum.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                    }
                    else
                    {
                        var date = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + h.ToString("00") + ":00:00");
                        response += "[" + DateToJavascript(date).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ",0.000],";
                    }
                }
                response = response.TrimEnd(',');
                response += " ]";
                response += "]";
                request.ResponseData = response;
                break;

            case "Parameter.StatsHour":
                domainSeparator = migCommand.GetOption(1).LastIndexOf(":");
                if (domainSeparator > 0)
                {
                    domain = migCommand.GetOption(1).Substring(0, domainSeparator);
                    address = migCommand.GetOption(1).Substring(domainSeparator + 1);
                }
                //
                response = "[";
                //
                dateStart = JavascriptToDate(long.Parse(migCommand.GetOption(2)));
                dateEnd = JavascriptToDate(long.Parse(migCommand.GetOption(3)));
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
                    response += "[ ";
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
                            response += "[" + DateToJavascript(date).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + sum.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                        }
                        else
                        {
                            var date = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + h.ToString("00") + ":00:00");
                            response += "[" + DateToJavascript(date).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ",0.000],";
                        }
                    }
                    response = response.TrimEnd(',');
                    response += " ],";

                }
                //
                response += "[ ";
                foreach (var entry in hoursAverages[4])
                {
                    var date = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + entry.TimeStart.ToLocalTime().ToString("HH:mm:ss.ffffff"));
                    response += "[" + DateToJavascript(date).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + entry.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                }
                response = response.TrimEnd(',');
                response += " ]";
                //
                response += "]";
                request.ResponseData = response;
                break;
            case "Parameter.StatsDay":
                domainSeparator = migCommand.GetOption(1).LastIndexOf(":");
                if (domainSeparator > 0)
                {
                    domain = migCommand.GetOption(1).Substring(0, domainSeparator);
                    address = migCommand.GetOption(1).Substring(domainSeparator + 1);
                }
                //
                response = "[";
                //
                dateStart = JavascriptToDate(long.Parse(migCommand.GetOption(2)));
                dateEnd = JavascriptToDate(long.Parse(migCommand.GetOption(3)));
                var daysAverages = new List<StatisticsEntry>[2];
                daysAverages[0] = homegenie.Statistics.GetHourlyStats(domain, address, migCommand.GetOption(0), "", dateStart, dateEnd);
                daysAverages[1] = homegenie.Statistics.GetHourlyStats(domain, address, migCommand.GetOption(0), "MaxDay", dateStart, dateEnd);
                response += "[ ";
                foreach (var entry in daysAverages[0])
                {
                    response += "[" + DateToJavascriptLocal(entry.TimeStart).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + entry.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                }
                response = response.TrimEnd(',');
                response += " ],[ ";
                foreach (var entry in daysAverages[1])
                {
                    response += "[" + DateToJavascriptLocal(entry.TimeStart).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + entry.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                }
                response = response.TrimEnd(',');
                response += " ]";
                //
                response += "]";
                request.ResponseData = response;
                break;
            case "Parameter.StatsDetail":
                response = "[";
                //
                dateStart = JavascriptToDate(long.Parse(migCommand.GetOption(2)));
                dateEnd = JavascriptToDate(long.Parse(migCommand.GetOption(3)));
                var daysMultiples = new List<StatisticsEntry>[1];
                daysMultiples[0] = homegenie.Statistics.GetHourlyStats(domain, address, migCommand.GetOption(0), "All", dateStart, dateEnd);
                response += "[ ";
                var moduleName = "";
                foreach (var entry in daysMultiples[0])
                {
                    if (entry.Divers == "")
                        entry.Divers = entry.Domain + ":" + entry.Address;
                    if(moduleName != entry.Divers)
                    {
                        if(moduleName != "")
                        {
                            response = response.TrimEnd(',');
                            response += " ],[ ";
                        }
                        response += "[\""+entry.Divers + "\"] ],[ ";
                        moduleName = entry.Divers;
                    }
                    response += "[" + DateToJavascriptLocal(entry.TimeStart).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + entry.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                }
                response = response.TrimEnd(',');
                response += " ]";
                response += "]";
                request.ResponseData = response;
                break;
            case "Parameter.StatsDouble":
                string parameter1 = "";
                string parameter2 = "";
                int paramSeparator = 0;
                paramSeparator = migCommand.GetOption(0).LastIndexOf(":");
                if (paramSeparator > 0)
                {
                    parameter1 = migCommand.GetOption(0).Substring(0, paramSeparator);
                    parameter2 = migCommand.GetOption(0).Substring(paramSeparator + 1);
                }
                domainSeparator = migCommand.GetOption(1).LastIndexOf(":");
                if (domainSeparator > 0)
                {
                    domain = migCommand.GetOption(1).Substring(0, domainSeparator);
                    address = migCommand.GetOption(1).Substring(domainSeparator + 1);
                }
                //
                response = "[";
                //
                dateStart = JavascriptToDate(long.Parse(migCommand.GetOption(2)));
                dateEnd = JavascriptToDate(long.Parse(migCommand.GetOption(3)));
                daysMultiples = new List<StatisticsEntry>[1];
                daysMultiples[0] = homegenie.Statistics.GetStatsDouble(domain, address, parameter1, parameter2, dateStart, dateEnd);
                response += "[ ";
                var paramName = "";
                foreach (var entry in daysMultiples[0])
                {
                    if("Sensor."+paramName != entry.Divers)
                    {
                        if(paramName != "")
                        {
                            response = response.TrimEnd(',');
                            response += " ],[ ";
                        }
                        paramName = entry.Divers.Substring(entry.Divers.LastIndexOf(".") + 1);
                        response += "[\""+ paramName + "\"] ],[ ";
                    }
                    response += "[" + DateToJavascriptLocal(entry.TimeStart).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + entry.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                }
                response = response.TrimEnd(',');
                response += " ]";
                response += "]";
                request.ResponseData = response;
                break;
            case "Parameter.StatDelete":
                response = "[";
                var dateText = migCommand.GetOption(0).Replace('.',',');
                dateStart = JavascriptToDateUtc(double.Parse(dateText));
                var responseDelete = homegenie.Statistics.DeleteStat(dateStart,migCommand.GetOption(1));
                response += "[Response," + responseDelete + "]";
                response += "]";
                request.ResponseData = response;
                break;
            }
        }

        private DateTime JavascriptToDate(long timestamp)
        {
            var baseDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            return (baseDate.AddMilliseconds(timestamp));
        }

        private DateTime JavascriptToDateUtc(double timestamp)
        {
            var baseDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Local);
            return (baseDate.AddMilliseconds(timestamp).ToUniversalTime());
        }

        private double DateToJavascript(DateTime date)
        {
            return ((date.Ticks - 621355968000000000L) / 10000D);
        }

        private double DateToJavascriptLocal(DateTime date)
        {
            return ((date.ToLocalTime().Ticks - 621355968000000000L) / 10000D);
        }
    }
}
