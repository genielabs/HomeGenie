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
        private HomeGenieService _hg;
        public Statistics(HomeGenieService hg)
        {
            _hg = hg;
        }

        public void ProcessRequest(MIGClientRequest request, MIGInterfaceCommand migcmd)
        {

            //SQLiteCommand dbcmd = _dbconnection.CreateCommand();
            switch (migcmd.command)
            {
                case "Global.CounterTotal":
                    migcmd.response = JsonHelper.GetSimpleResponse(_hg.Statistics.GetTotalCounter(migcmd.GetOption(0), 3600).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
                    break;

                case "Global.TimeRange":
                    StatisticsEntry totalrange = _hg.Statistics.GetStartDate();
                    migcmd.response = "[{ StartTime : '" + ((totalrange.TimeStart.Ticks - 621355968000000000) / 10000D).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "', EndTime : '" + ((totalrange.TimeEnd.Ticks - 621355968000000000) / 10000D).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "' }]";
                    break;

                case "Database.Reset":
                    _hg.Statistics.DatabaseReset();
                    break;

                case "Parameter.List":
                    var pdomain = "";
                    var paddress = "";
                    var pfilter = migcmd.GetOption(0).Split(':');
                    if (pfilter.Length == 2)
                    {
                        pdomain = pfilter[0];
                        paddress = pfilter[1];
                    }
                    migcmd.response = "[";
                    foreach (string sp in _hg.Statistics.GetParametersList(pdomain, paddress))
                    {
                        migcmd.response += "	'" + sp + "',\n";
                    }
                    migcmd.response = migcmd.response.TrimEnd(',', '\n');
                    migcmd.response += "\n]";
                    break;

                case "Parameter.Counter":
                    var cdomain = "";
                    var caddress = "";
                    var cfilter = migcmd.GetOption(1).Split(':');
                    if (cfilter.Length == 2)
                    {
                        cdomain = cfilter[0];
                        caddress = cfilter[1];
                    }
                    //
                    migcmd.response = "[";
                    migcmd.response += "[ ";
                    //
                    List<StatisticsEntry> hoursaverage = new List<StatisticsEntry>();
                    hoursaverage = _hg.Statistics.GetHourlyCounter(cdomain, caddress, migcmd.GetOption(0), 3600); ;
                    //
                    for (int h = 0; h < 24; h++)
                    {
                        StatisticsEntry firstentry = null;
                        if (hoursaverage != null && hoursaverage.Count > 0)
                        {
                            firstentry = hoursaverage.Find(se => se.TimeStart.ToLocalTime().Hour == h);
                        }
                        //
                        if (firstentry != null)
                        {
                            double sum = 0;
                            sum = (double)(hoursaverage.FindAll(se => se.TimeStart.ToLocalTime().Hour == h).Sum(se => se.Value));
                            // date is normalized to the current date, time info is preserved from original data entry
                            DateTime d = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + h.ToString("00") + ":00:00");
                            migcmd.response += "[" + ((d.Ticks - 621355968000000000) / 10000D).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + sum.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                        }
                        else
                        {
                            DateTime d = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + h.ToString("00") + ":00:00");
                            migcmd.response += "[" + ((d.Ticks - 621355968000000000) / 10000D).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ",0.000],";
                        }
                    }
                    migcmd.response = migcmd.response.TrimEnd(',');
                    migcmd.response += " ]";
                    migcmd.response += "]";
                    break;

                case "Parameter.StatsHour":
                    var domain = "";
                    var address = "";
                    var filter = migcmd.GetOption(1).Split(':');
                    if (filter.Length == 2)
                    {
                        domain = filter[0];
                        address = filter[1];
                    }
                    //
                    migcmd.response = "[";
                    //
                    List<StatisticsEntry>[] hoursaverages = new List<StatisticsEntry>[5];
                    hoursaverages[0] = _hg.Statistics.GetHourlyStats(domain, address, migcmd.GetOption(0), "Min");
                    hoursaverages[1] = _hg.Statistics.GetHourlyStats(domain, address, migcmd.GetOption(0), "Max");
                    hoursaverages[2] = _hg.Statistics.GetHourlyStats(domain, address, migcmd.GetOption(0), "Avg");
                    hoursaverages[3] = _hg.Statistics.GetHourlyStats24(domain, address, migcmd.GetOption(0), "Avg");
                    if (migcmd.GetOption(0) == Properties.METER_WATTS)
                    {
                        hoursaverages[4] = _hg.Statistics.GetTodayDetail(domain, address, migcmd.GetOption(0), "Sum");
                    }
                    else
                    {
                        hoursaverages[4] = _hg.Statistics.GetTodayDetail(domain, address, migcmd.GetOption(0), "Avg");
                    }
                    //
                    for (int x = 0; x < 4; x++)
                    {
                        migcmd.response += "[ ";
                        for (int h = 0; h < 24; h++)
                        {
                            StatisticsEntry firstentry = null;
                            if (hoursaverages[x] != null && hoursaverages[x].Count > 0)
                            {
                                if (migcmd.GetOption(0) == Properties.METER_WATTS)
                                {
                                    firstentry = hoursaverages[x].Find(se => se.TimeStart.ToLocalTime().Hour == h && se.Value > 0);
                                }
                                else
                                {
                                    firstentry = hoursaverages[x].Find(se => se.TimeStart.ToLocalTime().Hour == h);
                                }
                            }
                            //
                            if (firstentry != null)
                            {
                                double sum = 0;
                                switch (x)
                                {
                                    case 0:
                                        if (migcmd.GetOption(0) == Properties.METER_WATTS)
                                        {
                                            sum = (double)(hoursaverages[x].FindAll(se => se.TimeStart.ToLocalTime().Hour == h && se.Value > 0).Min(se => se.Value));
                                        }
                                        else
                                        {
                                            sum = (double)(hoursaverages[x].FindAll(se => se.TimeStart.ToLocalTime().Hour == h).Min(se => se.Value));
                                        }
                                        break;
                                    case 1:
                                        sum = (double)(hoursaverages[x].FindAll(se => se.TimeStart.ToLocalTime().Hour == h).Max(se => se.Value));
                                        break;
                                    case 2:
                                        if (migcmd.GetOption(0) == Properties.METER_WATTS)
                                        {
                                            sum = (double)(hoursaverages[x].FindAll(se => se.TimeStart.ToLocalTime().Hour == h && se.Value > 0).Average(se => se.Value));
                                        }
                                        else
                                        {
                                            sum = (double)(hoursaverages[x].FindAll(se => se.TimeStart.ToLocalTime().Hour == h).Average(se => se.Value));
                                        }
                                        break;
                                    case 3:
                                        if (migcmd.GetOption(0) == Properties.METER_WATTS)
                                        {
                                            sum = (double)(hoursaverages[x].FindAll(se => se.TimeStart.ToLocalTime().Hour == h && se.Value > 0).Average(se => se.Value));
                                        }
                                        else
                                        {
                                            sum = (double)(hoursaverages[x].FindAll(se => se.TimeStart.ToLocalTime().Hour == h).Average(se => se.Value));
                                        }
                                        break;
                                }
                                // date is normalized to the current date, time info is preserved from original data entry
                                DateTime d = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + h.ToString("00") + ":00:00");
                                migcmd.response += "[" + ((d.Ticks - 621355968000000000) / 10000D).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + sum.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                            }
                            else
                            {
                                DateTime d = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + h.ToString("00") + ":00:00");
                                migcmd.response += "[" + ((d.Ticks - 621355968000000000) / 10000D).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ",0.000],";
                            }
                        }
                        migcmd.response = migcmd.response.TrimEnd(',');
                        migcmd.response += " ],";

                    }
                    //
                    migcmd.response += "[ ";
                    foreach (StatisticsEntry se in hoursaverages[4])
                    {
                        DateTime d = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + se.TimeStart.ToLocalTime().ToString("HH:mm:ss.ffffff"));
                        migcmd.response += "[" + ((d.Ticks - 621355968000000000) / 10000D).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," + se.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "],";
                    }
                    migcmd.response = migcmd.response.TrimEnd(',');
                    migcmd.response += " ]";
                    //
                    migcmd.response += "]";
                    break;
            }




        }

    }
}
