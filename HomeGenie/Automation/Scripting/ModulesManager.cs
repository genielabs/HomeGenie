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
using System.Linq;
using System.Text;

using HomeGenie.Service;
using HomeGenie.Data;
using MIG;
using HomeGenie.Service.Constants;

namespace HomeGenie.Automation.Scripting
{
    public class ModulesManager
    {
//        private Dictionary<string, List<Module>> _cachedselections;

        private string command = "Command.NotSelected";
        private string commandvalue = "0";
        private string parameter = "Parameter.NotSelected";
        private string withname = "";
        private string oftype = "";
        private string ofdevicetype = "";
        private string ingroup = "";
        private string indomain = "";
        private string withaddress = "";
        private string withparameter = "";
        private string withfeature = "";
        private string withoutfeature = "";
        private double iterationdelay = 0;

        internal HomeGenieService _homegenie;

        public ModulesManager(HomeGenieService hg)
        {
            _homegenie = hg;
//            _cachedselections = new Dictionary<string, List<Module>>();
        }

        public virtual List<Module> SelectedModules
        {
            get
            {
                //string cachekey = command + "|" + commandvalue + "|" + parameter + "|" + withname + "|" + oftype + "|" + ofdevicetype + "|" + ingroup + "|" + indomain + "|" + withaddress + "|" + withparameter;
                //if (_cachedselections.ContainsKey(cachekey))
                //{
                //    return _cachedselections[cachekey];
                //}
                //
                List<Module> modules = new List<Module>();
                // select modules in current command context
                foreach (Module module in _homegenie.Modules.ToList<Module>())
                {
                    bool selected = true;
                    if (selected && this.indomain != null && this.indomain != "" && _getArgumentsList(this.indomain.ToLower()).Contains(module.Domain.ToLower()) == false)
                    {
                        selected = false;
                    }
                    if (selected && this.withaddress != null && this.withaddress != "" && _getArgumentsList(this.withaddress.ToLower()).Contains(module.Address.ToLower()) == false)
                    {
                        selected = false;
                    }
                    if (selected && this.withname != null && this.withname != "" && _getArgumentsList(this.withname.ToLower()).Contains(module.Name.ToLower()) == false)
                    {
                        selected = false;
                    }
                    if (selected && this.withparameter != null && this.withparameter != "")
                    {
                        if (module.Properties.Find( p => _getArgumentsList(this.withparameter).Contains(p.Name)) == null)
                        {
                            selected = false;
                        }
                    }
                    if (selected && this.withfeature != null && this.withfeature != "")
                    {
                        ModuleParameter mp = module.Properties.Find(p => _getArgumentsList(this.withfeature).Contains(p.Name));
                        if (mp == null || mp.Value != "On")
                        {
                            selected = false;
                        }
                    }
                    if (selected && this.withoutfeature != null && this.withoutfeature != "")
                    {
                        ModuleParameter mp = module.Properties.Find(p => _getArgumentsList(this.withoutfeature).Contains(p.Name));
                        if (mp != null && mp.Value == "On")
                        {
                            selected = false;
                        }
                    }
                    if (selected && this.ingroup != null && this.ingroup != "")
                    {
                        selected = false;
                        List<string> groups = _getArgumentsList(this.ingroup);
                        foreach (string grp in groups)
                        {
                            Group theGroup = _homegenie.Groups.Find(z => z.Name.ToLower() == grp.Trim().ToLower());
                            if (theGroup != null)
                            {
                                for (int m = 0; m < theGroup.Modules.Count; m++)
                                {
                                    if (module.Domain == theGroup.Modules[m].Domain && module.Address == theGroup.Modules[m].Address)
                                    {
                                        selected = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (selected && this.oftype != null && this.oftype != "")
                    {
                        selected = false;
                        List<string> types = _getArgumentsList(this.oftype);
                        foreach (string mtype in types)
                        {
                            if (module.Type.ToString().ToLower() == mtype.Trim().ToLower())
                            {
                                selected = true;
                                break;
                            }
                        }
                    }
                    if (selected && this.ofdevicetype != null && this.ofdevicetype != "")
                    {
                        selected = false;
                        List<string> devtypes = _getArgumentsList(this.ofdevicetype);
                        foreach(string dtype in devtypes)
                        {
                            if (module.DeviceType.ToString().ToLower() == dtype.Trim().ToLower())
                            {
                                selected = true;
                                break;
                            }
                        }
                    }
                    //
                    if (selected)
                    {
                        modules.Add(module);
                    }
                }
                //
                //if (!_cachedselections.ContainsKey(cachekey))
                //{
                //    _cachedselections.Add(cachekey, modules);
                //}
                //
                return modules;
            }
        }
		
		public List<string> Groups
		{
			get 
			{ 
				List<string> groups = new List<string>();
				foreach(Group g in _homegenie.Groups)
				{
					groups.Add(g.Name);
				}
				return groups; 
			}
		}
		
        public ModulesManager Each(Func<ModuleHelper, bool> callback)
        {
            foreach(Module m in SelectedModules)
            {
                if (callback(new ModuleHelper(_homegenie, m))) break;
            }
            return this;
        }

        public ModuleHelper Get()
        {
            return new ModuleHelper(_homegenie, SelectedModules.Count > 0 ? SelectedModules.First() : null);
        }

        public ModulesManager IterationDelay(double delayseconds)
        {
            this.iterationdelay = delayseconds;
            return this;
        }

        public ModulesManager WithParameter(string parameter)
        {
            this.withparameter = parameter;
            return this;
        }


        public ModulesManager WithFeature(string feature)
        {
            this.withfeature = feature;
            return this;
        }

        public ModulesManager WithoutFeature(string feature)
        {
            this.withoutfeature = feature;
            return this;
        }

        public ModuleHelper FromInstance(Module module)
        {
            return new ModuleHelper(_homegenie, module);
        }

        public ModulesManager Command(string command)
        {
            this.command = command;
            return this;
        }

        public ModulesManager InGroup(string group)
        {
            this.ingroup = group;
            return this;
        }

        public ModulesManager WithName(string modulename)
        {
            this.withname = modulename;
            return this;
        }

        public ModulesManager InDomain(string domain)
        {
            this.indomain = domain;
            return this;
        }

        public ModulesManager WithAddress(string moduleaddr)
        {
            this.withaddress = moduleaddr;
            return this;
        }

        public ModulesManager OfDeviceType(string devicetype)
        {
            this.ofdevicetype = devicetype;
            return this;
        }

        public ModulesManager OfType(string type)
        {
            this.oftype = type;
            return this;
        }

        public ModulesManager Execute()
        {
            return Set();
        }
        public ModulesManager Execute(string sparams)
        {
            return Set(sparams);
        }
        public ModulesManager Set()
        {
            this.commandvalue = "0";
            return Set(this.commandvalue);
        }
        public ModulesManager Set(string valueto)
        {
            this.commandvalue = valueto;
            // execute this command context
            if (command != "")
            {
                foreach (Module module in SelectedModules)
                {
                    _interfacecontrol(module, new MIGInterfaceCommand("/" + module.Domain + "/" + module.Address + "/" + command + "/" + commandvalue + "/"));
                    _delayiteration();
                }
            }
            return this;
        }

        ////////////////////////////////////////////////////////////
        public ModulesManager On()
        {
            foreach (Module module in SelectedModules)
            {
                _interfacecontrol(module, new MIGInterfaceCommand("/" + module.Domain + "/" + module.Address + "/Control.On/"));
                _delayiteration();
            }
            return this;
        }

        public ModulesManager Off()
        {
            foreach (Module module in SelectedModules)
            {
                _interfacecontrol(module, new MIGInterfaceCommand("/" + module.Domain + "/" + module.Address + "/Control.Off/"));
                _delayiteration();
            }
            return this;
        }

        public ModulesManager Toggle()
        {
            foreach (Module module in SelectedModules)
            {
                ModuleParameter level = Utility.ModuleParameterGet(module, Properties.STATUS_LEVEL);
                if (level != null)
                {
                    if (level.Value == "0")
                    {
                        _interfacecontrol(module, new MIGInterfaceCommand("/" + module.Domain + "/" + module.Address + "/Control.On/"));
                    }
                    else
                    {
                        _interfacecontrol(module, new MIGInterfaceCommand("/" + module.Domain + "/" + module.Address + "/Control.Off/"));
                    }
                }
                _delayiteration();
            }
            return this;
        }

        #region Properties
        public double Level
        {
            get
            {
                double avglevel = 0;
                if (SelectedModules.Count > 0)
                {
                    foreach (Module module in SelectedModules)
                    {
                        double level = Service.Utility.ModuleParameterGet(module, Properties.STATUS_LEVEL).DecimalValue;
                        level = (level * 100D);
                        avglevel += level;
                    }
                    avglevel = avglevel / SelectedModules.Count;
                }
                return avglevel;
            }
            set
            {
                this.command = Commands.Control.CONTROL_LEVEL;
                this.Set(value.ToString());
            }
        }


        public bool IsOn
        {
            get
            {
                bool ison = false;
                if (SelectedModules.Count > 0)
                {
                    foreach (Module module in SelectedModules)
                    {
                        double dvalue = Service.Utility.ModuleParameterGet(module, Properties.STATUS_LEVEL).DecimalValue;
                        ison = ison || (dvalue * 100D > 0D); // if at least one of the selected modules are on it returns true
                        if (ison) break;
                    }
                }
                return ison;
            }
        }

        public bool IsOff
        {
            get
            {
                return !this.IsOn;
            }
        }

        public bool Alarmed
        {
            get
            {
                bool alarmed = false;
                foreach (Module module in SelectedModules)
                {
                    double intvalue = Service.Utility.ModuleParameterGet(module, "Sensor.Alarm").DecimalValue;
                    alarmed = alarmed || (intvalue > 0); // if at least one of the selected modules are alarmed it returns true
                    if (alarmed) break;
                }
                return alarmed;
            }
        }

        public bool MotionDetected
        {
            get
            {
                bool alarmed = false;
                foreach (Module module in SelectedModules)
                {
                    double intvalue = Service.Utility.ModuleParameterGet(module, "Sensor.MotionDetect").DecimalValue;
                    alarmed = alarmed || (intvalue > 0); // if at least one of the selected modules detected motion it returns true
                    if (alarmed) break;
                }
                return alarmed;
            }
        }

        public double Temperature
        {
            get
            {
                return _getAverageParameterValue("Sensor.Temperature");
            }
        }

        public double Luminance
        {
            get
            {
                return _getAverageParameterValue("Sensor.Luminance");
            }
        }

        public double Humidity
        {
            get
            {
                return _getAverageParameterValue("Sensor.Humidity");
            }
        }

        public string X10RfData
        {
            get
            {
                return RfRemoteData;
            }
        }
        public string RfRemoteData
        {
            get
            {
                string rfdata = "";
                Module rfmod = _homegenie.Modules.Find(m => (m.Domain == Domains.HomeAutomation_X10 && m.Address == "RF"));
                if (rfmod != null)
                {
                    try
                    {
                        rfdata = Service.Utility.ModuleParameterGet(rfmod, "Receiver.RawData").Value;
                    }
                    catch { }
                }
                return rfdata;
            }
        }
        public string RfRemoteDataW800
        {
            get
            {
                string rfdata = "";
                Module rfmod = _homegenie.Modules.Find(m => (m.Domain == Domains.HomeAutomation_W800RF && m.Address == "RF"));
                if (rfmod != null)
                {
                    try
                    {
                        rfdata = Service.Utility.ModuleParameterGet(rfmod, "Receiver.RawData").Value;
                    }
                    catch { }
                }
                return rfdata;
            }
        }
        #endregion

        public ModulesManager Reset()
        {
            command = "Command.NotSelected";
            commandvalue = "0";
            parameter = "Parameter.NotSelected";
            withname = "";
            oftype = "";
            ofdevicetype = "";
            ingroup = "";
            indomain = "";
            withaddress = "";
            withparameter = "";
            withfeature = "";
            iterationdelay = 0;
            //
            return this;
        }

        private double _getAverageParameterValue(string parameter)
        {
            double avgvalue = 0;
            if (SelectedModules.Count > 0)
            {
                foreach (Module module in SelectedModules)
                {
                    double value = Service.Utility.ModuleParameterGet(module, parameter).DecimalValue;
                    avgvalue += value; ;
                }
                avgvalue = avgvalue / SelectedModules.Count;
            }
            return avgvalue;
        }

        internal static List<string> _getArgumentsList(string csargs)
        {
            List<string> retval = new List<string>();
            if (csargs.IndexOf('|') > 0)
            {
                retval = csargs.Split('|').ToList<string>();
            }
            else
            {
                retval = csargs.Split(',').ToList<string>();
            }
            return retval;
        }

        private void _delayiteration()
        {
            if (this.iterationdelay > 0)
            {
                System.Threading.Thread.Sleep((int)(this.iterationdelay * 1000D));
            }
        }

        private void _interfacecontrol(Module module, MIGInterfaceCommand cmd)
        {
            cmd.domain = module.Domain;
            cmd.nodeid = module.Address;
            //string options = "";
            //for (int o = 0; o < cmd.options.Length; o++ )
            //{
            //    options += cmd.options[o] + "/";
            //}
            _homegenie.InterfaceControl(cmd); //new Command("/" + module.Domain + "/" + module.Address + "/" + cmd.command + "/" + options));
            _homegenie.WaitOnPending(module.Domain);
        }
    }
}
