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
 *     Project Homepage: https://homegenie.it
 */

using System;
using System.Collections.Generic;
using System.Linq;

using HomeGenie.Service;
using HomeGenie.Data;
using MIG;
using HomeGenie.Service.Constants;
using System.Globalization;
using System.Threading.Tasks;

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
            inDomain = domains;
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
            withAddress = addresses;
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
            withName = moduleNames;
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
            ofDeviceType = deviceTypes;
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
            inGroup = groups;
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
            withParameter = parameters;
            return this;
        }

        /// <summary>
        /// Select all modules having specified features.
        /// </summary>
        /// <returns>ModulesManager</returns>
        /// <param name="features">A string containing comma separated feature names.</param>
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
            withFeature = features;
            return this;
        }

        /// <summary>
        /// Select all modules NOT having specified features.
        /// </summary>
        /// <returns>ModulesManager</returns>
        /// <param name="features">A string containing comma seperated feature names.</param>
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
            withoutFeature = features;
            return this;
        }

        #endregion


        #region Collections/Enumeration

        /// <summary>
        /// Iterate through each module in the current selection and pass it to the specified `callback`.
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
        /// Gets or sets the modules list on which this helper class will be working on.
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
                    if (selected && inDomain != null && inDomain != "" && GetArgumentsList(inDomain.ToLower()).Contains(module.Domain.ToLower()) == false)
                    {
                        selected = false;
                    }
                    if (selected && !string.IsNullOrEmpty(withAddress) && GetArgumentsList(withAddress.ToLower()).Contains(module.Address.ToLower()) == false)
                    {
                        selected = false;
                    }
                    if (selected && !string.IsNullOrEmpty(withName) && GetArgumentsList(withName.ToLower()).Contains(module.Name.ToLower()) == false)
                    {
                        selected = false;
                    }
                    if (selected && !string.IsNullOrEmpty(withParameter))
                    {
                        if (module.Properties.Find(p => GetArgumentsList(withParameter).Contains(p.Name)) == null)
                        {
                            selected = false;
                        }
                    }
                    if (selected && !string.IsNullOrEmpty(withFeature))
                    {
                        var parameter = module.Properties.Find(p => GetArgumentsList(withFeature).Contains(p.Name));
                        if (parameter == null || (parameter.Value != "On" && parameter.DecimalValue == 0))
                        {
                            selected = false;
                        }
                    }
                    if (selected && !string.IsNullOrEmpty(withoutFeature))
                    {
                        var parameter = module.Properties.Find(p => GetArgumentsList(withoutFeature).Contains(p.Name));
                        if (parameter != null && parameter.Value == "On")
                        {
                            selected = false;
                        }
                    }
                    if (selected && !string.IsNullOrEmpty(inGroup))
                    {
                        selected = false;
                        var groups = GetArgumentsList(inGroup);
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
                    if (selected && !string.IsNullOrEmpty(ofDeviceType))
                    {
                        selected = false;
                        var deviceTypes = GetArgumentsList(ofDeviceType);
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
        /// Select an API command to be executed for selected modules. To perform the selected command, `Submit` method must be called.
        /// </summary>
        /// <returns>ModulesManager</returns>
        /// <param name="cmd">API command to be performed.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// // turn on all modues of "Light" type
        /// Modules.OfDeviceType("Light").Command("Control.On").Execute();
        /// // set all dimmers to 50%
        /// Modules.OfDeviceType("Dimmer").Command("Control.Level").Submit("50");
        /// </code>
        /// </example>
        public ModulesManager Command(string cmd)
        {
            command = cmd;
            return this;
        }

        /// <summary>
        /// Used before a command (*Submit*, *On*, *Off*, *Toggle*, ...), it will put a pause after performing the command for each module in the current selection. 
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
        ///     .Submit("40");
        /// </code>
        /// </example>
        public ModulesManager IterationDelay(double delaySeconds)
        {
            iterationDelay = delaySeconds;
            return this;
        }

        /// <summary>
        /// Execute current command on first selected module and return the response value.
        /// </summary>
        /// <param name="options">Options.</param>
        public object GetValue(string options = "")
        {
            commandOptions = options;
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

        [Obsolete("This method is deprecated, use Submit() instead.")]
        public ModulesManager Execute()
        {
            return Set();
        }

        [Obsolete("This method is deprecated, use Submit(string options) instead.")]
        public ModulesManager Execute(string options)
        {
            return Set(options);
        }

        [Obsolete("This method is deprecated, use Submit() instead.")]
        public ModulesManager Set()
        {
            commandOptions = "0";
            return Set(commandOptions);
        }

        [Obsolete("This method is deprecated, use Submit(string options) instead.")]
        public ModulesManager Set(string options)
        {
            commandOptions = options;
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
        /// Submits the command previously specified with `Command` method.
        /// </summary>
        /// <param name="callback">Optional callback that will be called, for each module in the selection, with the result of the issued command.</param>
        /// <returns>ModulesManager</returns>
        public ModulesManager Submit(Action<Module, object> callback)
        {
            commandOptions = "0";
            return Submit(commandOptions, callback);
        }
        // the following redundant method definition is for Jint compatibility (JavaScript engine)
        public ModulesManager Submit()
        {
            return Submit(null);
        }

        /// <summary>
        /// Submits the command previously specified with `Command` method, passing to it the options given by the `options` parameter.
        /// </summary>
        /// <param name="options">A string containing a slash separated list of options to be passed to the selected command.</param>
        /// <param name="callback">Optional callback that will be called, for each module in the selection, with the result of the issued command.</param>
        /// <returns>ModulesManager</returns>
        public ModulesManager Submit(string options, Action<Module, object> callback = null)
        {
            commandOptions = options;
            // execute this command context
            if (command != "")
            {
                foreach (var module in SelectedModules)
                {
                    var response = InterfaceControl(
                        module,
                        new MigInterfaceCommand(module.Domain + "/" + module.Address + "/" + command + "/" + commandOptions)
                    );
                    if (callback != null)
                    {
                        Task.Run(() => callback(module, response));
                    }
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
                    new MigInterfaceCommand(module.Domain + "/" + module.Address + "/" + Service.API.Automation.Control.On)
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
                    new MigInterfaceCommand(module.Domain + "/" + module.Address + "/" + Service.API.Automation.Control.Off)
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
                    InterfaceControl(
                        module,
                        new MigInterfaceCommand(module.Domain + "/" + module.Address + "/" + Service.API.Automation.Control.Toggle)
                    );
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
                command = Service.API.Automation.Control.Level;
                Submit((value * 100).ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Gets or sets "Status.ColorHsb" parameter of selected modules. If more than one module is selected, when reading this property the first module color is returned.
        /// </summary>
        /// <value>The HSB color string (eg. "0.3130718,0.986,0.65").</value>
        /// <remarks />
        public string ColorHsb
        {
            get
            {
                // TODO: find a way to return average color value if more than one module is selected
                var module = Get().Instance;
                if (module != null)
                {
                    var parameter = Utility.ModuleParameterGet(module, Properties.StatusColorHsb);
                    return parameter != null ? parameter.Value : "";
                }
                return "";
            }
            set
            {
                command = Service.API.Automation.Control.ColorHsb;
                Submit(value);
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


        #region Utility methods

        /// <summary>
        /// Creates a copy of the actual modules selection.
        /// </summary>
        /// <returns>ModulesManager</returns>
        public ModulesManager Copy()
        {
            var modulesManager = new ModulesManager(homegenie)
            {
                command = command,
                commandOptions = commandOptions,
                withName = withName,
                ofDeviceType = ofDeviceType,
                inGroup = inGroup,
                inDomain = inDomain,
                withAddress = withAddress,
                withParameter = withParameter,
                withFeature = withFeature,
                withoutFeature = withoutFeature,
                iterationDelay = iterationDelay,
                modulesListCallback = modulesListCallback
            };
            return modulesManager;
        }

        /// <summary>
        /// Resets all selection filters.
        /// </summary>
        /// <returns>ModulesManager</returns>
        public ModulesManager Reset()
        {
            command = "Command.NotSelected";
            commandOptions = "0";
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
        
        #endregion


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
                var p = Utility.ModuleParameterGet(module, parameter);
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
            if (iterationDelay > 0)
            {
                System.Threading.Thread.Sleep((int)(iterationDelay * 1000D));
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
