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
using MIG;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace HomeGenie.Service.Handlers
{
    public class Logging
    {
        private HomeGenieService homegenie;

        public Logging(HomeGenieService hg)
        {
            homegenie = hg;
        }

        public void ProcessRequest(MIGClientRequest request, MIGInterfaceCommand migCommand)
        {
            var logData = new List<LogEntry>();
            try
            {
                switch (migCommand.Command)
                {
                    case "Recent.From":
                        logData = homegenie.RecentEventsLog.ToList().FindAll(le => le != null && le.Domain.StartsWith("MIG.") == false && (le.UnixTimestamp >= double.Parse(migCommand.GetOption(0))));
                        migCommand.Response = JsonConvert.SerializeObject(logData); //, Formatting.Indented);
                        break;

                    case "Recent.Last":
                        logData = homegenie.RecentEventsLog.ToList().FindAll(le => le != null
                        && le.Domain.StartsWith("MIG.") == false
                        && le.Timestamp > DateTime.UtcNow.AddMilliseconds(-int.Parse(migCommand.GetOption(0))));
                        migCommand.Response = JsonConvert.SerializeObject(logData); //, Formatting.Indented);
                        break;

                    case "RealTime.EventStream":
                        HttpListenerContext context = (HttpListenerContext)request.Context;
                        context.Response.KeepAlive = true;
                        context.Response.ContentType = "text/event-stream";
                        context.Response.ContentEncoding = Encoding.UTF8;
                        DateTime lastEvent = DateTime.UtcNow.AddSeconds(-3);
                        int looped = 0;
                        while (looped < 10)
                        {
                            logData = homegenie.RecentEventsLog.ToList().FindAll(le => le != null
                            && le.Domain.StartsWith("MIG.") == false
                            && le.Timestamp > lastEvent);
                            if (logData.Count > 0)
                            {
                                lastEvent = DateTime.UtcNow;
                                foreach (LogEntry entry in logData)
                                {
                                    byte[] data = System.Text.Encoding.UTF8.GetBytes("data: " + JsonConvert.SerializeObject(entry) + "\n\n");
                                    context.Response.OutputStream.Write(data, 0, data.Length);
                                    context.Response.OutputStream.Flush();
                                }
                            }
                            System.Threading.Thread.Sleep(1000);
                            looped++;
                        }
                        break;
                }
            }
            catch
            {
            }
        }
    }
}
