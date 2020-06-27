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
#if !NETCOREAPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MIG;

using HomeGenie.Service.Constants;
using HomeGenie.Service.Logging;

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
                request.ResponseData = "{ \"StartTime\" : \"" + Utility.DateToJavascript(totalRange.TimeStart).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\", \"EndTime\" : \"" + Utility.DateToJavascript(totalRange.TimeEnd).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "\" }";
                break;

            case "Database.Reset":
                homegenie.Statistics.ResetDatabase();
                request.ResponseData = new ResponseStatus(Status.Ok);
                break;
            case "Configuration.Get":
                // Just one at the moment.
                request.ResponseData = "{ \"StatisticsUiRefreshSeconds\" : \"" + homegenie.SystemConfiguration.HomeGenie.Statistics.StatisticsUiRefreshSeconds + "\" }";
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
                dateStart = Utility.JavascriptToDate(long.Parse(migCommand.GetOption(2)));
                dateEnd = Utility.JavascriptToDate(long.Parse(migCommand.GetOption(3)));
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
                        response += "[" + Utility.DateToJavascript(date).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + sum.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                    }
                    else
                    {
                        var date = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + h.ToString("00") + ":00:00");
                        response += "[" + Utility.DateToJavascript(date).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ",0.000],";
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
                dateStart = Utility.JavascriptToDate(long.Parse(migCommand.GetOption(2)));
                dateEnd = Utility.JavascriptToDate(long.Parse(migCommand.GetOption(3)));
                var hoursAverages = new List<StatisticsEntry>[5];
                hoursAverages[0] = homegenie.Statistics.GetHourlyStats(domain, address, migCommand.GetOption(0), "Min", dateStart, dateEnd);
                hoursAverages[1] = homegenie.Statistics.GetHourlyStats(domain, address, migCommand.GetOption(0), "Max", dateStart, dateEnd);
                hoursAverages[2] = homegenie.Statistics.GetHourlyStats(domain, address, migCommand.GetOption(0), "Avg", dateStart, dateEnd);
                hoursAverages[3] = homegenie.Statistics.GetHourlyStatsToday(domain, address, migCommand.GetOption(0), "Avg");
                if (migCommand.GetOption(0).StartsWith(Properties.MeterAny))
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
                            if (migCommand.GetOption(0).StartsWith(Properties.MeterAny))
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
                                if (migCommand.GetOption(0).StartsWith(Properties.MeterAny))
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
                                if (migCommand.GetOption(0).StartsWith(Properties.MeterAny))
                                {
                                    sum = (double)(hoursAverages[x].FindAll(se => se.TimeStart.ToLocalTime().Hour == h && se.Value > 0).Average(se => se.Value));
                                }
                                else
                                {
                                    sum = (double)(hoursAverages[x].FindAll(se => se.TimeStart.ToLocalTime().Hour == h).Average(se => se.Value));
                                }
                                break;
                            case 3:
                                if (migCommand.GetOption(0).StartsWith(Properties.MeterAny))
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
                            response += "[" + Utility.DateToJavascript(date).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + sum.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                        }
                        else
                        {
                            var date = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + h.ToString("00") + ":00:00");
                            response += "[" + Utility.DateToJavascript(date).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ",0.000],";
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
                    response += "[" + Utility.DateToJavascript(date).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + entry.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
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
                dateStart = Utility.JavascriptToDate(long.Parse(migCommand.GetOption(2)));
                dateEnd = Utility.JavascriptToDate(long.Parse(migCommand.GetOption(3)));
                var daysAverages = new List<StatisticsEntry>[1];
                daysAverages[0] = homegenie.Statistics.GetHourlyStats(domain, address, migCommand.GetOption(0), "", dateStart, dateEnd);
                response += "[ ";
                foreach (var entry in daysAverages[0])
                {
                    response += "[" + Utility.DateToJavascriptLocal(entry.TimeStart).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + entry.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                }
                response = response.TrimEnd(',');
                response += " ]";
                //
                response += "]";
                request.ResponseData = response;
                break;
            case "Parameter.StatsMultiple":
                response = "[";
                //
                dateStart = Utility.JavascriptToDate(long.Parse(migCommand.GetOption(2)));
                dateEnd = Utility.JavascriptToDate(long.Parse(migCommand.GetOption(3)));
                var daysMultiples = new List<StatisticsEntry>[1];
                daysMultiples[0] = homegenie.Statistics.GetHourlyStats(domain, address, migCommand.GetOption(0), "All", dateStart, dateEnd);
                response += "[ ";
                var moduleName = "";
                foreach (var entry in daysMultiples[0])
                {
                    if (entry.CustomData == "")
                        entry.CustomData = entry.Domain + ":" + entry.Address;
                    if (moduleName != entry.CustomData)
                    {
                        if (moduleName != "")
                        {
                            response = response.TrimEnd(',');
                            response += " ],[ ";
                        }
                        response += "[\"" + entry.CustomData + "\"] ],[ ";
                        moduleName = entry.CustomData;
                    }
                    response += "[" + Utility.DateToJavascriptLocal(entry.TimeStart).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + entry.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                }
                response = response.TrimEnd(',');
                response += " ]";
                /*response += "[ ";
                var moduleName = "";
                foreach (var entry in daysMultiples[0])
                {
                    if (entry.CustomData == "")
                        entry.CustomData = entry.Domain + ":" + entry.Address;
                    if(moduleName != entry.CustomData)
                    {
                        if(moduleName != "")
                        {
                            response = response.TrimEnd(',');
                            response += " ] ],[ ";
                        }
                        response += "[ \""+entry.CustomData + "\" ],[ ";
                        moduleName = entry.CustomData;
                    }
                    response += "[" + DateToJavascriptLocal(entry.TimeStart).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + entry.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                }
                response = response.TrimEnd(',');
                response += " ]";
                if(moduleName != "")
                    response += " ]";*/
                //
                response += "]";
                request.ResponseData = response;
                break;
            case "Parameter.StatRemove":
                var dateText = migCommand.GetOption(0).Replace('.', ',');
                dateStart = Utility.JavascriptToDateUtc(double.Parse(dateText));
                homegenie.Statistics.DeleteData(dateStart, migCommand.GetOption(1));
                request.ResponseData = new ResponseText("OK");
                break;
            }
        }

    }
}
#endif
