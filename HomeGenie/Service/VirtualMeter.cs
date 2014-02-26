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
using MIG.Interfaces.HomeAutomation.Commons;

using HomeGenie;
using HomeGenie.Data;

namespace HomeGenie.Service
{

    public class VirtualMeter
    {
        Thread _meterthread;

        HomeGenieService _homegenie;

        private bool _vmrunning = true;

        public VirtualMeter(HomeGenieService hg)
        {
            _homegenie = hg;
            Start();
        }

        public void Start()
        {
            _meterthread = new Thread(new ThreadStart(_virtualmeterloop));
            _meterthread.Start();
        }

        public void Stop()
        {
            _vmrunning = false;
        }

        private void _virtualmeterloop()
        {
            while (_vmrunning)
            {
                try
                {
                    foreach (Module module in _homegenie.Modules)
                    {
                        ModuleParameter parameter = null;
                        parameter = module.Properties.Find(delegate(ModuleParameter mp) { return mp.Name == ModuleParameters.MODPAR_VIRTUALMETER_WATTS; });
                        if (parameter == null)
                        {
                            continue;
                        }
                        else
                        {
                            double vmwatts = 0;
                            //
                            try
                            {
                                vmwatts = double.Parse(parameter.Value.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                                if (vmwatts > 0)
                                {
                                    parameter = module.Properties.Find(delegate(ModuleParameter mp) { return mp.Name == ModuleParameters.MODPAR_STATUS_LEVEL; });
                                    double level = double.Parse(parameter.Value.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                                    //
                                    //_homegenie.migservice_InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = module.Domain, SourceId = module.Address, SourceType = module.Description, Path = Globals.MODPAR_STATUS_LEVEL, Value = level.ToString() });
                                    _homegenie.migservice_InterfacePropertyChanged(new InterfacePropertyChangedAction() { Domain = module.Domain, SourceId = module.Address, SourceType = module.Description, Path = ModuleParameters.MODPAR_METER_WATTS, Value = level == 0 ? "0" : (vmwatts * level).ToString(/* System.Globalization.CultureInfo.InvariantCulture */) });
                                    //
                                    Thread.Sleep(100);
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { } // TODO: this should use locking instead of try cactch
                //
                Thread.Sleep(30000);
            }
        }

    }
}

