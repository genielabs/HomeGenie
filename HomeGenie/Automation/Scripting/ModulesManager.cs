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
using System.Globalization;

namespace HomeGenie.Automation.Scripting
{
    /// <summary>
    /// Modules Manager Helper class.\n
    /// Offers methods for filtering, selecting and operate on a group of modules.\n
    /// Class instance accessor: **Modules**
    /// </summary>
    [Serializable]
    public class ModulesManager
    {
        private string command = "Command.NotSelected";
        private string commandOptions = "0";
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
        private Func<ModulesManager,TsList<Module>> modulesListCallback = null;

        internal HomeGenieService homegenie;

        public ModulesManager(HomeGenieService hg)
        {
            homegenie = hg;
        }


        #region Selection/Filtering

        /// <summary>
        /// Select modules belonging to specified domains.
        /// </summary>
        /// <returns>ModulesManager</returns>
        /// <param name="domains">A string containing comma seperated domain names.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// // turn off all z-wave lights
        /// Modules
        ///     .InDomain("HomeAutomation.ZWave")
        ///     .OfDeviceType("Light,Dimmer")
        ///     .Off();
        /// </code>
        /// </example>
        public ModulesManager InDomain(string domains)
        {
            this.inDomain = domains;
            return this;
        }

        /// <summary>
        /// Select modules with specified address.
        /// </summary>
        /// <returns>ModulesManager</returns>
        /// <param name="addresses">A string containing comma seperated address values.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// // turn on X10 units A2 and B5
        /// Modules.WithAddress("A2,B5").On();
        /// </code>
        /// </example>
        public ModulesManager WithAddress(string addresses)
        {
            this.withAddress = addresses;
            return this;
        }

        /// <summary>
        /// Select modules matching specified names.
        /// </summary>
        /// <returns>ModulesManager</returns>
        /// <param name="moduleNames">A string containing comma seperated module names.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// // turn off ceiling light
        /// Modules.WithName("Ceiling Light").Off();
        /// </code>
        /// </example>
        public ModulesManager WithName(string moduleNames)
        {
            this.withName = moduleNames;
            return this;
        }

        /// <summary>
        /// Select modules of specified device types.
        /// </summary>
        /// <returns>ModulesManager</returns>
        /// <param name="deviceTypes">A string containing comma seperated type names.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// // turn on all lights and appliances
        /// Modules.OfDeviceType("Light,Dimmer,Switch").On();
        /// </code>
        /// </example>
        public ModulesManager OfDeviceType(string deviceTypes)
        {
            this.ofDeviceType = deviceTypes;
            return this;
        }

        /// <summary>
        /// Select modules included in specified groups.
        /// </summary>
        /// <returns>ModulesManager</returns>
        /// <param name="groups">A string containing comma seperated group names.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// Modules.InGroup("Living Room,Kitchen").Off();
        /// </code>
        /// </example>
        public ModulesManager InGroup(string groups)
        {
            this.inGroup = groups;
            return this;
        }

        /// <summary>
        /// Select all modules having specified parameters.
        /// </summary>
        /// <returns>ModulesManager</returns>
        /// <param name="parameters">A string containing comma seperated parameter names.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// // select all modules with Sensor.Temperature parameter and get the average temperature value
        /// var averagetemperature = Modules.WithParameter("Sensor.Temperature").Temperature;
        /// Program.Notify("Average Temperature", averagetemperature);
        /// </code>
        /// </example>
        public ModulesManager WithParameter(string parameters)
        {
            this.withParameter = parameters;
            return this;
        }

        /// <summary>
        /// Select all modules having specified features.
        /// </summary>
        /// <returns>ModulesManager</returns>
        /// <param name="feature">A string containing comma seperated feature names.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// // Turn on all Security System sirens
        /// Modules.WithFeature("HomeGenie.SecurityAlarm").On();
        /// </code>
        /// </example>
        public ModulesManager WithFeature(string features)
        {
            this.withFeature = features;
            return this;
        }

        /// <summary>
        /// Select all modules NOT having specified features.
        /// </summary>
        /// <returns>ModulesManager</returns>
        /// <param name="feature">A string containing comma seperated feature names.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// // Turn off all modules not having the "EnergySavingMode" feature
        /// Modules.WithoutFeature("EnergyManagement.EnergySavingMode").Off();
        /// </code>
        /// </example>
        public ModulesManager WithoutFeature(string features)
        {
            this.withoutFeature = features;
            return this;
        }

        #endregion


        #region Collections/Enumeration

        /// <summary>
        /// Iterate through each module in the current selection and pass it to the specified <callback>.
        /// To break the iteration, the callback must return *true*, otherwise *false*.
        /// </summary>
        /// <param name="callback">Callback function to call for each iteration.</param>
        /// <returns>ModulesManager</returns>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// var total_watts_load = 0D;
        /// Modules.WithParameter("Meter.Watts").Each( (module) => {
        ///             total_watts_load += module.Parameter("Meter.Watts").DecimalValue;
        ///             return false; // continue iterating
        /// });
        /// Program.Notify("Current power load", total_watts_load + " watts");
        /// </code>
        /// </example>
        public ModulesManager Each(Func<ModuleHelper, bool> callback)
        {
            foreach (var module in SelectedModules)
            {
                if (callback(new ModuleHelper(homegenie, module))) break;
            }
            return this;
        }

        /// <summary>
        /// Returns the module in the current selection.
        /// If the current selection contains more than one element, the first element will be returned.
        /// </summary>
        /// <returns>ModuleHelper</returns>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// var strobeAlarm = Modules.WithName("Strobe Alarm").Get();        
        /// </code>
        /// </example>
        public ModuleHelper Get()
        {
            return new ModuleHelper(homegenie, SelectedModules.Count > 0 ? SelectedModules.First() : null);
        }

        public ModuleHelper FromInstance(Module module)
        {
            return new ModuleHelper(homegenie, module);
        }

        /// <summary>
        /// Gets or sets the modules set on which this helper class will be working on.
        /// </summary>
        /// <value>The modules list callback.</value>
        public Func<ModulesManager,TsList<Module>> ModulesListCallback
        {
            get { return modulesListCallback; }
            set { modulesListCallback = value; }
        }

        /// <summary>
        /// Gets the complete modules list.
        /// </summary>
        /// <value>The modules.</value>
        public TsList<Module> Modules
        {
            get
            {
                if (modulesListCallback == null)
                    return homegenie.Modules;
                else
                    return modulesListCallback(this);
            }
        }

        /// <summary>
        /// Return the list of selected modules.
        /// </summary>
        /// <returns>List&lt;Module&gt;</returns>
        public virtual TsList<Module> SelectedModules
        {
            get
            {
                var modules = new TsList<Module>();
                // select modules in current command context
                for (int cm = 0; cm < Modules.Count; cm++)
                {
                    var module = Modules[cm];
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

        /// <summary>
        /// Return the list of control groups.
        /// </summary>
        /// <returns>List&lt;string&gt;</returns>
        public List<string> Groups
        {
            get
            {
                var groups = new List<string>();
                for (int cg = 0; cg < homegenie.Groups.Count; cg++)
                {
                    var group = homegenie.Groups[cg];
                    groups.Add(group.Name);
                }
                return groups;
            }
        }

        #endregion


        #region Commands

        /// <summary>
        /// Select an API command to be executed for selected modules. To perform the selected command, Execute or Set method must be invoked.
        /// </summary>
        /// <returns>ModulesManager</returns>
        /// <param name="command">API command to be performed.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// // turn on all modues of "Light" type
        /// Modules.OfDeviceType("Light").Command("Control.On").Execute();
        /// // set all dimmers to 50%
        /// Modules.OfDeviceType("Dimmer").Command("Control.Level").Set("50");
        /// </code>
        /// </example>
        public ModulesManager Command(string command)
        {
            this.command = command;
            return this;
        }

        /// <summary>
        /// Used before a command (*Set*, *Execute*, *On*, *Off*, *Toggle*, ...), it will put a pause after performing the command for each module in the current selection. 
        /// </summary>
        /// <returns>ModulesManager</returns>
        /// <param name="delaySeconds">Delay seconds.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// // Set the level of all dimmer type modules to 40%, 
        /// // putting a 100ms delay between each command
        /// Modules
        ///     .OfDeviceType("Dimmer")
        ///     .Command("Control.Level")
        ///     .IterationDelay(0.1)
        ///     .Set(40);
        /// </code>
        /// </example>
        public ModulesManager IterationDelay(double delaySeconds)
        {
            this.iterationDelay = delaySeconds;
            return this;
        }

        /// <summary>
        /// Execute current command on first selected module and return the response value.
        /// </summary>
        /// <param name="options">Options.</param>
        public object GetValue(string options = "")
        {
            this.commandOptions = options;
            object response = null;
            // execute this command context
            var selectedModules = SelectedModules;
            if (command != "" && selectedModules.Count > 0)
            {
                var module = selectedModules[0];
                response = InterfaceControl(
                    module,
                    new MigInterfaceCommand(module.Domain + "/" + module.Address + "/" + command + "/" + commandOptions)
                );
            }
            return response;
        }

        /// <summary>
        /// Execute current command for all selected modules.
        /// </summary>
        /// <returns>ModulesManager</returns>
        public ModulesManager Execute()
        {
            return Set();
        }

        /// <summary>
        /// Execute current command with specified options.
        /// </summary>
        /// <param name="options">A string containing options to be passed to the selected command.</param>
        /// <returns>ModulesManager</returns>
        public ModulesManager Execute(string options)
        {
            return Set(options);
        }

        /// <summary>
        /// Alias for Execute()
        /// </summary>
        /// <returns>ModulesManager</returns>
        public ModulesManager Set()
        {
            this.commandOptions = "0";
            return Set(this.commandOptions);
        }

        /// <summary>
        /// Alias for Execute(options)
        /// </summary>
        /// <param name="options">A string containing options to be passed to the selected command.</param>
        /// <returns>ModulesManager</returns>
        public ModulesManager Set(string options)
        {
            this.commandOptions = options;
            // execute this command context
            if (command != "")
            {
                foreach (var module in SelectedModules)
                {
                    InterfaceControl(
                        module,
                        new MigInterfaceCommand(module.Domain + "/" + module.Address + "/" + command + "/" + commandOptions)
                    );
                    DelayIteration();
                }
            }
            return this;
        }

        /// <summary>
        /// Turn on all selected modules.
        /// </summary>
        /// <returns>ModulesManager</returns>
        public ModulesManager On()
        {
            foreach (var module in SelectedModules)
            {
                InterfaceControl(
                    module,
                    new MigInterfaceCommand(module.Domain + "/" + module.Address + "/" + Commands.Control.ControlOn)
                );
                DelayIteration();
            }
            return this;
        }

        /// <summary>
        /// Turn off all selected modules.
        /// </summary>
        /// <returns>ModulesManager</returns>
        public ModulesManager Off()
        {
            foreach (var module in SelectedModules)
            {
                InterfaceControl(
                    module,
                    new MigInterfaceCommand(module.Domain + "/" + module.Address + "/" + Commands.Control.ControlOff)
                );
                DelayIteration();
            }
            return this;
        }

        /// <summary>
        /// Toggle all selected modules.
        /// </summary>
        /// <returns>ModulesManager</returns>
        public ModulesManager Toggle()
        {
            foreach (var module in SelectedModules)
            {
                var levelParameter = Utility.ModuleParameterGet(module, Properties.StatusLevel);
                if (levelParameter != null)
                {
                    if (levelParameter.Value == "0")
                    {
                        InterfaceControl(
                            module,
                            new MigInterfaceCommand(module.Domain + "/" + module.Address + "/" + Commands.Control.ControlOn)
                        );
                    }
                    else
                    {
                        InterfaceControl(
                            module,
                            new MigInterfaceCommand(module.Domain + "/" + module.Address + "/" + Commands.Control.ControlOff)
                        );
                    }
                    DelayIteration();
                }
            }
            return this;
        }

        #endregion


        #region Properties

        /// <summary>
        /// Gets or sets "Status.Level" parameter of selected modules. If more than one module is selected, when reading this property the average level value is returned.
        /// </summary>
        /// <value>The level (percentage value 0-100).</value>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// // Set the level of all modules with EnergySavingMode flag enabled to 40%
        /// Modules.WithFeature("EnergyManagement.EnergySavingMode").Level = 40;
        /// </code>
        /// </example>
        public double Level
        {
            get
            {
                return GetAverageParameterValue(Properties.StatusLevel);
            }
            set
            {
                this.command = Commands.Control.ControlLevel;
                this.Set(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Gets "on" status ("Status.Level" > 0).
        /// </summary>
        /// <value><c>true</c> if at least one module in the current selection is on; otherwise, <c>false</c>.</value>
        public bool IsOn
        {
            get
            {
                return IsGreaterThanZero(Properties.StatusLevel);
            }
        }

        /// <summary>
        /// Gets "off" status ("Status.Level" == 0).
        /// </summary>
        /// <value><c>true</c> if at least one module in the current selection is off; otherwise, <c>false</c>.</value>
        public bool IsOff
        {
            get
            {
                return IsZero(Properties.StatusLevel);
            }
        }

        /// <summary>
        /// Gets "alarm" status ("Sensor.Alarm" > 0).
        /// </summary>
        /// <value><c>true</c> if at least one module in the current is alarmed; otherwise, <c>false</c>.</value>
        public bool Alarmed
        {
            get
            {
                return IsGreaterThanZero(Properties.SensorAlarm) || IsGreaterThanZero(Properties.SensorTamper);
            }
        }

        /// <summary>
        /// Gets "motion detection" status ("Sensor.MotionDetect" > 0).
        /// </summary>
        /// <value><c>true</c> if at least one module in the current detected motion; otherwise, <c>false</c>.</value>
        public bool MotionDetected
        {
            get
            {
                return IsGreaterThanZero(Properties.SensorMotionDetect) || IsGreaterThanZero(Properties.StatusLevel);
            }
        }

        /// <summary>
        /// Gets temperature value ("Sensor.Temperature").
        /// </summary>
        /// <value>The temperature parameter of selected module (average value is returned when more than one module is selected).</value>
        public double Temperature
        {
            get
            {
                return GetAverageParameterValue(Properties.SensorTemperature);
            }
        }

        /// <summary>
        /// Gets luminance value ("Sensor.Luminance").
        /// </summary>
        /// <value>The luminance parameter of selected module (average value is returned when more than one module is selected).</value>
        public double Luminance
        {
            get
            {
                return GetAverageParameterValue(Properties.SensorLuminance);
            }
        }

        /// <summary>
        /// Gets humidity value ("Sensor.Humidity").
        /// </summary>
        /// <value>The humidity parameter of selected module (average value is returned when more than one module is selected).</value>
        public double Humidity
        {
            get
            {
                return GetAverageParameterValue(Properties.SensorHumidity);
            }
        }

        #endregion


        public ModulesManager Reset()
        {
            command = "Command.NotSelected";
            commandOptions = "0";
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

        private double GetAverageParameterValue(string parameter)
        {
            double averageValue = 0;
            int count = 0;
            foreach (var module in SelectedModules)
            {
                var p = Service.Utility.ModuleParameterGet(module, parameter);
                if (p != null)
                {
                    averageValue += p.DecimalValue;
                    count++;
                }
            }
            if (count > 0)
                averageValue = averageValue / SelectedModules.Count;
            return averageValue;
        }

        private bool IsGreaterThanZero(string parameter)
        {
            bool gz = false;
            foreach (var module in SelectedModules)
            {
                var p = Utility.ModuleParameterGet(module, parameter);
                if (p != null)
                {
                    // if at least one of the selected modules has 'parameter' greater than zero then return true
                    gz = (p.DecimalValue * 100D > 0D); 
                    if (gz) break;
                }
            }
            return gz;
        }

        private bool IsZero(string parameter)
        {
            bool ez = false;
            foreach (var module in SelectedModules)
            {
                var p = Utility.ModuleParameterGet(module, parameter);
                if (p != null)
                {
                    // if at least one of the selected modules has 'parameter' equal zero then return true
                    ez = (p.DecimalValue * 100D == 0D); 
                    if (ez) break;
                }
            }
            return ez;
        }

        private void DelayIteration()
        {
            if (this.iterationDelay > 0)
            {
                System.Threading.Thread.Sleep((int)(this.iterationDelay * 1000D));
            }
        }

        private object InterfaceControl(Module module, MigInterfaceCommand migCommand)
        {
            object response = null;
            migCommand.Domain = module.Domain;
            migCommand.Address = module.Address;
            try
            {
                response = homegenie.InterfaceControl(migCommand);
            }
            catch(Exception e)
            {
                // TODO: should report the error?
            }
            return response;
        }
    }
}
