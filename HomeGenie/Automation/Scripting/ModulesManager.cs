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
        private string command = "Command.NotSelected";
        private string commandValue = "0";
        //private string parameter = "Parameter.NotSelected";
        private string withName = "";
        private string ofDeviceType = "";
        private string inGroup = "";
        private string inDomain = "";
        private string withAddress = "";
        private string withParameter = "";
        private string withFeature = "";
        private string withoutFeature = "";
        private double iterationDelay = 0;

        internal HomeGenieService homegenie;

        public ModulesManager(HomeGenieService hg)
        {
            homegenie = hg;
        }

        public virtual List<Module> SelectedModules
        {
            get
            {
                var modules = new List<Module>();
                // select modules in current command context
                foreach (var module in homegenie.Modules.ToList<Module>())
                {
                    bool selected = true;
                    if (selected && this.inDomain != null && this.inDomain != "" && GetArgumentsList(this.inDomain.ToLower()).Contains(module.Domain.ToLower()) == false)
                    {
                        selected = false;
                    }
                    if (selected && this.withAddress != null && this.withAddress != "" && GetArgumentsList(this.withAddress.ToLower()).Contains(module.Address.ToLower()) == false)
                    {
                        selected = false;
                    }
                    if (selected && this.withName != null && this.withName != "" && GetArgumentsList(this.withName.ToLower()).Contains(module.Name.ToLower()) == false)
                    {
                        selected = false;
                    }
                    if (selected && this.withParameter != null && this.withParameter != "")
                    {
                        if (module.Properties.Find(p => GetArgumentsList(this.withParameter).Contains(p.Name)) == null)
                        {
                            selected = false;
                        }
                    }
                    if (selected && this.withFeature != null && this.withFeature != "")
                    {
                        var parameter = module.Properties.Find(p => GetArgumentsList(this.withFeature).Contains(p.Name));
                        if (parameter == null || parameter.Value != "On")
                        {
                            selected = false;
                        }
                    }
                    if (selected && this.withoutFeature != null && this.withoutFeature != "")
                    {
                        var parameter = module.Properties.Find(p => GetArgumentsList(this.withoutFeature).Contains(p.Name));
                        if (parameter != null && parameter.Value == "On")
                        {
                            selected = false;
                        }
                    }
                    if (selected && this.inGroup != null && this.inGroup != "")
                    {
                        selected = false;
                        var groups = GetArgumentsList(this.inGroup);
                        foreach (string group in groups)
                        {
                            var theGroup = homegenie.Groups.Find(z => z.Name.ToLower() == group.Trim().ToLower());
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
                    if (selected && this.ofDeviceType != null && this.ofDeviceType != "")
                    {
                        selected = false;
                        var deviceTypes = GetArgumentsList(this.ofDeviceType);
                        foreach (string dtype in deviceTypes)
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

                return modules;
            }
        }

        public List<string> Groups
        {
            get
            {
                var groups = new List<string>();
                foreach (var group in homegenie.Groups)
                {
                    groups.Add(group.Name);
                }
                return groups;
            }
        }

        public ModulesManager Each(Func<ModuleHelper, bool> callback)
        {
            foreach (var module in SelectedModules)
            {
                if (callback(new ModuleHelper(homegenie, module))) break;
            }
            return this;
        }

        public ModuleHelper Get()
        {
            return new ModuleHelper(homegenie, SelectedModules.Count > 0 ? SelectedModules.First() : null);
        }

        public ModulesManager IterationDelay(double delaySeconds)
        {
            this.iterationDelay = delaySeconds;
            return this;
        }

        public ModulesManager WithParameter(string parameter)
        {
            this.withParameter = parameter;
            return this;
        }


        public ModulesManager WithFeature(string feature)
        {
            this.withFeature = feature;
            return this;
        }

        public ModulesManager WithoutFeature(string feature)
        {
            this.withoutFeature = feature;
            return this;
        }

        public ModuleHelper FromInstance(Module module)
        {
            return new ModuleHelper(homegenie, module);
        }

        public ModulesManager Command(string command)
        {
            this.command = command;
            return this;
        }

        public ModulesManager InGroup(string group)
        {
            this.inGroup = group;
            return this;
        }

        public ModulesManager WithName(string modulename)
        {
            this.withName = modulename;
            return this;
        }

        public ModulesManager InDomain(string domain)
        {
            this.inDomain = domain;
            return this;
        }

        public ModulesManager WithAddress(string moduleaddr)
        {
            this.withAddress = moduleaddr;
            return this;
        }

        public ModulesManager OfDeviceType(string devicetype)
        {
            this.ofDeviceType = devicetype;
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
            this.commandValue = "0";
            return Set(this.commandValue);
        }

        public ModulesManager Set(string valueToSet)
        {
            this.commandValue = valueToSet;
            // execute this command context
            if (command != "")
            {
                foreach (var module in SelectedModules)
                {
                    InterfaceControl(
                        module,
                        new MIGInterfaceCommand("/" + module.Domain + "/" + module.Address + "/" + command + "/" + commandValue + "/")
                    );
                    DelayIteration();
                }
            }
            return this;
        }

        ////////////////////////////////////////////////////////////
        public ModulesManager On()
        {
            foreach (var module in SelectedModules)
            {
                InterfaceControl(
                    module,
                    new MIGInterfaceCommand("/" + module.Domain + "/" + module.Address + "/Control.On/")
                );
                DelayIteration();
            }
            return this;
        }

        public ModulesManager Off()
        {
            foreach (var module in SelectedModules)
            {
                InterfaceControl(
                    module,
                    new MIGInterfaceCommand("/" + module.Domain + "/" + module.Address + "/Control.Off/")
                );
                DelayIteration();
            }
            return this;
        }

        public ModulesManager Toggle()
        {
            foreach (var module in SelectedModules)
            {
                var levelParameter = Utility.ModuleParameterGet(module, Properties.STATUS_LEVEL);
                if (levelParameter != null)
                {
                    if (levelParameter.Value == "0")
                    {
                        InterfaceControl(
                            module,
                            new MIGInterfaceCommand("/" + module.Domain + "/" + module.Address + "/Control.On/")
                        );
                    }
                    else
                    {
                        InterfaceControl(
                            module,
                            new MIGInterfaceCommand("/" + module.Domain + "/" + module.Address + "/Control.Off/")
                        );
                    }
                }
                DelayIteration();
            }
            return this;
        }

        #region Properties

        public double Level
        {
            get
            {
                double averageLevel = 0;
                if (SelectedModules.Count > 0)
                {
                    foreach (var module in SelectedModules)
                    {
                        var levelParameter = Utility.ModuleParameterGet(module, Properties.STATUS_LEVEL);
                        if (levelParameter != null)
                        {
                            double level = levelParameter.DecimalValue;
                            level = (level * 100D);
                            averageLevel += level;
                        }
                    }
                    averageLevel = averageLevel / SelectedModules.Count;
                }
                return averageLevel;
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
                bool isOn = false;
                if (SelectedModules.Count > 0)
                {
                    foreach (var module in SelectedModules)
                    {
                        var levelParameter = Utility.ModuleParameterGet(module, Properties.STATUS_LEVEL);
                        if (levelParameter != null)
                        {
                            double dvalue = levelParameter.DecimalValue;
                            isOn = isOn || (dvalue * 100D > 0D); // if at least one of the selected modules are on it returns true
                            if (isOn) break;
                        }

                    }
                }
                return isOn;
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
                foreach (var module in SelectedModules)
                {
                    var alarmParameter = Utility.ModuleParameterGet(module, "Sensor.Alarm");
                    if (alarmParameter != null)
                    {
                        double intvalue = alarmParameter.DecimalValue;
                        alarmed = alarmed || (intvalue > 0); // if at least one of the selected modules are alarmed it returns true
                        if (alarmed) break;
                    }
                }
                return alarmed;
            }
        }

        public bool MotionDetected
        {
            get
            {
                bool alarmed = false;
                foreach (var module in SelectedModules)
                {
                    var motionParameter = Service.Utility.ModuleParameterGet(module, "Sensor.MotionDetect");
                    if (motionParameter != null)
                    {
                        double intvalue = motionParameter.DecimalValue;
                        alarmed = alarmed || (intvalue > 0); // if at least one of the selected modules detected motion it returns true
                        if (alarmed) break;
                    }
                }
                return alarmed;
            }
        }

        public double Temperature
        {
            get
            {
                return GetAverageParameterValue("Sensor.Temperature");
            }
        }

        public double Luminance
        {
            get
            {
                return GetAverageParameterValue("Sensor.Luminance");
            }
        }

        public double Humidity
        {
            get
            {
                return GetAverageParameterValue("Sensor.Humidity");
            }
        }

        // TODO: deprecate this
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
                string rfData = "";
                var rfModule = homegenie.Modules.Find(m => (m.Domain == Domains.HomeAutomation_X10 && m.Address == "RF"));
                if (rfModule != null)
                {
                    var rawdataParameter = Service.Utility.ModuleParameterGet(rfModule, "Receiver.RawData");
                    if (rawdataParameter != null)
                    {
                        rfData = rawdataParameter.Value;
                    }
                }
                return rfData;
            }
        }

        // TODO: deprecate this
        public string RfRemoteDataW800
        {
            get
            {
                string rfData = "";
                var rfModule = homegenie.Modules.Find(m => (m.Domain == Domains.HomeAutomation_W800RF && m.Address == "RF"));
                if (rfModule != null)
                {
                    var rawdataParameter = Service.Utility.ModuleParameterGet(rfModule, "Receiver.RawData");
                    if (rawdataParameter != null)
                    {
                        rfData = rawdataParameter.Value;
                    }
                }
                return rfData;
            }
        }

        #endregion

        public ModulesManager Reset()
        {
            command = "Command.NotSelected";
            commandValue = "0";
            //parameter = "Parameter.NotSelected";
            withName = "";
            ofDeviceType = "";
            inGroup = "";
            inDomain = "";
            withAddress = "";
            withParameter = "";
            withFeature = "";
            iterationDelay = 0;
            //
            return this;
        }

        private double GetAverageParameterValue(string parameter)
        {
            double averageValue = 0;
            if (SelectedModules.Count > 0)
            {
                foreach (var module in SelectedModules)
                {
                    double value = Service.Utility.ModuleParameterGet(module, parameter).DecimalValue;
                    averageValue += value;
                    ;
                }
                averageValue = averageValue / SelectedModules.Count;
            }
            return averageValue;
        }

        internal static List<string> GetArgumentsList(string csArgumentList)
        {
            var returnValue = new List<string>();
            if (csArgumentList.IndexOf('|') > 0)
            {
                returnValue = csArgumentList.Split('|').ToList<string>();
            }
            else
            {
                returnValue = csArgumentList.Split(',').ToList<string>();
            }
            return returnValue;
        }

        private void DelayIteration()
        {
            if (this.iterationDelay > 0)
            {
                System.Threading.Thread.Sleep((int)(this.iterationDelay * 1000D));
            }
        }

        private void InterfaceControl(Module module, MIGInterfaceCommand migCommand)
        {
            migCommand.Domain = module.Domain;
            migCommand.NodeId = module.Address;
            homegenie.InterfaceControl(migCommand);
            homegenie.WaitOnPending(module.Domain);
        }
    }
}
