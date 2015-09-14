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
using System.Threading;

using MIG;

using HomeGenie;
using HomeGenie.Data;
using HomeGenie.Service.Constants;

namespace HomeGenie.Service
{

    public class VirtualMeter
    {
        private HomeGenieService homegenie;
        private bool isRunning = true;
        private int reportFrequency = 30000;

        public VirtualMeter(HomeGenieService hg)
        {
            homegenie = hg;
            Start();
        }

        public void Start()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(virtualMeterTask));
        }

        public void Stop()
        {
            isRunning = false;
        }

        private void virtualMeterTask(object state)
        {
            while (isRunning)
            {
                for (int m = 0; m < homegenie.Modules.Count; m++)
                {
                    var module = homegenie.Modules[m];
                    ModuleParameter parameter = null;
                    parameter = module.Properties.Find(delegate(ModuleParameter mp) { return mp.Name == Properties.VIRTUALMETER_WATTS; });
                    if (parameter == null)
                    {
                        continue;
                    }
                    else
                    {
                        try
                        {
                            double watts = double.Parse(parameter.Value.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                            if (watts > 0)
                            {
                                parameter = module.Properties.Find(delegate(ModuleParameter mp) { return mp.Name == Properties.STATUS_LEVEL; });
                                double level = double.Parse(parameter.Value.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                                double fuzzyness = (new Random().Next(0, 50) - 25) / 100D;
                                //
                                homegenie.RaiseEvent(
                                    Domains.HomeGenie_System, 
                                    module.Domain,
                                    module.Address,
                                    module.Description,
                                    Properties.METER_WATTS,
                                    level == 0 ? "0.0" : ((watts * level) + fuzzyness).ToString(System.Globalization.CultureInfo.InvariantCulture)
                                );
                                //
                                Thread.Sleep(10);
                            }
                        }
                        catch { }
                    }
                }
                Thread.Sleep(reportFrequency);
            }
        }

    }
}

