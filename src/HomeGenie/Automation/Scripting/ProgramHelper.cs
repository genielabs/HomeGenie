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
using System.Diagnostics;
using System.Threading;
using System.Dynamic;
using System.Globalization;

using GLabs.Logging;
using MIG.Interfaces.HomeAutomation.Commons;
using Newtonsoft.Json;

using HomeGenie.Data;
using HomeGenie.Service;
using HomeGenie.Service.Constants;

namespace HomeGenie.Automation.Scripting
{
    /// <summary>
    /// Program Helper class.\n
    /// Class instance accessor: **Program**
    /// </summary>
    [Serializable]
    public class ProgramHelper : ProgramHelperBase
    {
        private Module programModule;
        private int myProgramId;
        private string myProgramDomain = Domains.HomeAutomation_HomeGenie_Automation;
        private object setupLock = new object();

        private Logger programLogger;

        // whether 'Setup' has been executed
        private bool initialized;

        public ProgramHelper(HomeGenieService hg, int programId) : base(hg)
        {
            myProgramId = programId;
        }

        /// <summary>
        /// Gets the logger object.
        /// </summary>
        /// <value>The logger object.</value>
        public Logger Log
        {
            get
            {
                if (programLogger == null)
                {
                    string categoryName = $"Program:{myProgramId}";
                    programLogger = LogManager.GetLogger(categoryName);
                }
                return programLogger;
            }
        }

        /// <summary>
        /// Execute a setup function when the program is enabled. It is meant to be used in the "Setup Code" to execute only once
        /// the instructions contained in the passed function. It is mainly used for setting program configuration fields, parameters and features.
        /// </summary>
        /// <param name="functionBlock">Function name or inline delegate.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        ///  Program.Setup(()=>
        ///  {
        ///      Program.AddOption(
        ///          "MaxLevel",
        ///          "40",
        ///          "Keep level below the following value",
        ///          "slider:10:80");
        ///      Program.AddFeature(
        ///          "Dimmer",
        ///          "EnergyManagement.EnergySavingMode",
        ///          "Energy Saving Mode enabled light",
        ///          "checkbox");
        ///  }, 0);
        /// </code>
        /// </example>
        public void Setup(Action functionBlock)
        {
            lock (setupLock)
            {
                if (!initialized)
                {
                    // mark config options to determine unused ones
                    if (Module != null)
                    {
                        for (var p = 0; p < Module.Properties.Count; p++)
                        {
                            var parameter = Module.Properties[p];
                            if (parameter.Name.StartsWith("ConfigureOptions."))
                            {
                                parameter.Description = null;
                            }
                        }
                    }

                    homegenie.RaiseEvent(
                        myProgramId,
                        myProgramDomain,
                        myProgramId.ToString(),
                        "Automation Program",
                        Properties.ProgramStatus,
                        "Setup"
                    );

                    functionBlock();

                    // remove deprecated config options
                    if (Module != null)
                    {
                        var parameterList =
                            Module.Properties.FindAll(mp => mp.Name.StartsWith("ConfigureOptions."));
                        foreach (var parameter in parameterList)
                        {
                            if (parameter.Name.StartsWith("ConfigureOptions.") && parameter.Description == null)
                            {
                                Module.Properties.Remove(parameter);
                            }
                        }
                    }

                    initialized = true;

                    homegenie.modules_RefreshPrograms();
                    homegenie.modules_RefreshVirtualModules();

                    homegenie.RaiseEvent(
                        myProgramId,
                        myProgramDomain,
                        myProgramId.ToString(),
                        "Automation Program",
                        Properties.ProgramStatus,
                        "Idle"
                    );
                }
            }
        }

        /// <summary>
        /// Run the program as soon as the "Setup Code" exits. This command is meant to be used in the "Setup Code".
        /// </summary>
        /// <param name="willRun">If set to <c>true</c> will run.</param>
        public void Run(bool willRun)
        {
            var program = GetProgramBlock();
            if (program != null)
            {
                program.WillRun = willRun;
            }
        }

        // this "dupe" is required to avoid issues with JavaScript engine method resolution as optional/default parameters are not well supported at the time
        public void Run()
        {
            Run(true);
        }

        /// <summary>
        /// Set the widget that will be used for displaying this program data in the UI Control page.
        /// </summary>
        /// <returns>ProgramHelper.</returns>
        /// <param name="widget">The widget path.</param>
        public ProgramHelper UseWidget(string widget)
        {
            var program = GetProgramBlock();
            var module = homegenie.VirtualModules.Find(rm =>
                rm.ParentId == myProgramId.ToString() && rm.Domain == myProgramDomain &&
                rm.Address == myProgramId.ToString());
            if (module == null)
            {
                module = new VirtualModule()
                {
                    ParentId = myProgramId.ToString(),
                    Domain = myProgramDomain,
                    Address = myProgramId.ToString(),
                    Name = (program != null ? program.Name : ""),
                    DeviceType = ModuleTypes.Program
                };
                homegenie.VirtualModules.Add(module);
            }

            module.Name = (program != null ? program.Name : "");
            module.Domain = myProgramDomain;

            var widgetModule = Utility.ModuleParameterGet(module, Properties.WidgetDisplayModule);
            if (widgetModule == null || widgetModule.Value != widget)
            {
                Utility.ModuleParameterSet(module, Properties.WidgetDisplayModule, widget);
                if (Module != null)
                {
                    Utility.ModuleParameterSet(Module, Properties.WidgetDisplayModule, widget);
                }
                homegenie.modules_RefreshVirtualModules();
                //homegenie.modules_Sort();
            }

            return this;
        }

        /// <summary>
        /// Register this program as implementation of the specified interface.
        /// </summary>
        /// <returns>
        /// ProgramHelper
        /// </returns>
        /// <param name="interfaceId">
        /// The string identifier of the implemented interface. (e.g. "@Statistics:Provider")
        /// </param>
        /// <param name="apiUrl">
        /// Mandatory URL of API to call to get implementation details/config
        /// </param>
        /// <param name="options">
        /// Optional custom object
        /// </param>
        public ProgramHelper Implements(string interfaceId, string apiUrl, object options = null) 
        {
            var pb = GetProgramBlock();
            if (pb != null)
            {
                if (!pb.ImplementedInterfaces.Exists(i => i.Identifier == interfaceId))
                {
                    pb.ImplementedInterfaces.Add(new ImplementedInterface()
                    {
                        Identifier = interfaceId,
                        ApiUrl = apiUrl,
                        Options = options
                    });
                }
            }
            return this;
        }

        /// <summary>
        /// Adds a configuration option for the program. The option field will be displayed in the program options dialog using the UI widget specified by the "type" field.
        /// </summary>
        /// <returns>
        /// ProgramHelper
        /// </returns>
        /// <param name='field'>
        /// Name of this input field
        /// </param>
        /// <param name='defaultValue'>
        /// Default value for this input field
        /// </param>
        /// <param name='description'>
        /// Description for this input field
        /// </param>
        /// <param name='type'>
        /// The type of this option (eg. "text", "password", "cron.text", ...). Each type can have different initialization options
        /// that are specified as a list of items separated by a ":" colon.
        /// </param>
        public ProgramHelper AddOption(string field, string defaultValue, string description, string type)
        {
            var parameter = Parameter("ConfigureOptions." + field);
            if (parameter.Value == "") parameter.Value = defaultValue;
            parameter.Description = description;
            parameter.FieldType = type;
            parameter.ParentId = myProgramId; // TODO ....
            return this;
        }

        /// <summary>
        /// Gets the value of a program option field.
        /// </summary>
        /// <returns>
        /// The option field.
        /// </returns>
        /// <param name='field'>
        /// Name of the option field to get.
        /// </param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// // ....
        /// var delay = Program.Option("OffDelay").DecimalValue;
        /// Pause( delay );
        /// Modules.WithFeature("Timer.TurnOffDelay").Off();
        /// </code>
        /// </example>
        public ModuleParameter Option(string field)
        {
            ModuleParameter parameter = null;
            try
            {
                parameter = Utility.ModuleParameterGet(Module, "ConfigureOptions." + field);
            }
            catch
            {
                // TODO: report exception
            }
            return parameter;
        }

        /// <summary>
        /// Adds a "feature" field to modules matching the specified domain/type.
        /// Feature fields are used by automation programs to create own handled module parameters.
        /// This command should only appear inside a Program.Setup delegate.
        /// </summary>
        /// <returns>
        /// ProgramHelper.
        /// </returns>
        /// <param name='forDomains'>
        /// Expression based on module domain names, used to select what modules will be showing this feature field. This can be either a valid Regular Expression enclosed by '/' and '/' delimiters or a simple comma (or '|') separated list of domain names (prepend '!' to a name to exclude it).
        /// </param>
        /// <param name='forModuleTypes'>
        /// Expression based on module types and parameters names, used to select what modules will be showing this feature field. This must be in the format `&lt;types_expr&gt;[:&lt;parameters_expr&gt;]`. &lt;types_expr&gt; and &lt;parameters_expr&gt; can be either a valid Regular Expression enclosed by '/' and '/' delimiters or a simple comma (or '|') separated list of names (prepend '!' to a name to exclude it).
        /// </param>
        /// <param name='parameterName'>
        /// Name of the module parameter bound to this feature field.
        /// </param>
        /// <param name='description'>
        /// Description for this input field.
        /// </param>
        /// <param name='type'>
        /// The type of this feature field (eg. "text", "password", "cron.text", ...). Each type can have different initialization options
        /// that are specified as ":" separated items. See html/ui/widgets folder for a list of possible types/options.
        /// </param>
        public ProgramHelper AddFeature(
            string forDomains,
            string forModuleTypes,
            string parameterName,
            string description,
            string type
        )
        {
            var program = GetProgramBlock();
            ProgramFeature feature = null;

            try
            {
                feature = program.Features.Find(f => f.Property == parameterName);
            }
            catch (Exception ex)
            {
                HomeGenieService.LogError(
                    myProgramDomain,
                    myProgramId.ToString(),
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }

            if (feature == null)
            {
                feature = new ProgramFeature();
                program.Features.Add(feature);
            }

            feature.FieldType = type;
            feature.Property = parameterName;
            feature.Description = description;
            feature.ForDomains = forDomains;
            feature.ForTypes = forModuleTypes;
            return this;
        }

        /// <summary>
        /// Return the feature field associated to the specified module parameter.
        /// </summary>
        /// <param name="parameterName">Parameter name.</param>
        public ProgramFeature Feature(string parameterName)
        {
            var program = GetProgramBlock();
            ProgramFeature feature = null;

            try
            {
                feature = program.Features.Find(f => f.Property == parameterName);
            }
            catch (Exception ex)
            {
                HomeGenieService.LogError(
                    myProgramDomain,
                    myProgramId.ToString(),
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }

            return feature;
        }

        /// <summary>
        /// Adds a new module to the system.
        /// </summary>
        /// <returns>ProgramHelper.</returns>
        /// <param name="domain">Domain.</param>
        /// <param name="address">Address.</param>
        /// <param name="type">Type (Generic, Program, Switch, Light, Dimmer, Sensor, Temperature, Siren, Fan, Thermostat, Shutter, Motor, DoorWindow, MediaTransmitter, MediaReceiver).</param>
        /// <param name="widget">Widget to display this modules with.</param>
        /// <param name="implementedFeatures">Allow only features explicitly declared in this list</param>
        public ProgramHelper AddModule(string domain, string address, string type, string widget = "", string[] implementedFeatures = null)
        {
            VirtualModule virtualModule = AddProgramModule(domain, address, type, widget, implementedFeatures);
            homegenie.modules_RefreshVirtualModules();
            homegenie.modules_Sort();
            // update real module device type and widget
            Module module =
                homegenie.Modules.Find(o => o.Domain == virtualModule.Domain && o.Address == virtualModule.Address);
            if (module != null)
            {
                if (module.DeviceType == ModuleTypes.Generic)
                {
                    module.DeviceType = virtualModule.DeviceType;
                }
                Utility.ModuleParameterSet(module, Properties.WidgetDisplayModule, widget);
            }
            return this;
        }

        /// <summary>
        /// Adds a new set of modules to the system.
        /// </summary>
        /// <returns>ProgramHelper.</returns>
        /// <param name="domain">Domain.</param>
        /// <param name="startAddress">Start address (numeric).</param>
        /// <param name="endAddress">End address (numeric).</param>
        /// <param name="type">Type (Generic, Program, Switch, Light, Dimmer, Sensor, Temperature, Siren, Fan, Thermostat, Shutter, Motor, DoorWindow, MediaTransmitter, MediaReceiver).</param>
        /// <param name="widget">Widget to display these modules with.</param>
        /// <param name="implementedFeatures">Allow only features explicitly declared in this list</param>
        public ProgramHelper AddModules(
            string domain,
            int startAddress,
            int endAddress,
            string type,
            string widget = "",
            string[] implementedFeatures = null)
        {
            var vmList = new List<VirtualModule>();
            for (int x = startAddress; x <= endAddress; x++)
            {
                VirtualModule virtualModule = AddProgramModule(domain, x.ToString(), type, widget, implementedFeatures);
                vmList.Add(virtualModule);
            }
            homegenie.modules_RefreshVirtualModules();
            homegenie.modules_Sort();
            foreach (var virtualModule in vmList)
            {
                // update real module device type and widget
                Module module = homegenie.Modules.Find(o => o.Domain == virtualModule.Domain && o.Address == virtualModule.Address);
                if (module != null)
                {
                    if (module.DeviceType == ModuleTypes.Generic)
                    {
                        module.DeviceType = virtualModule.DeviceType;
                    }
                    Utility.ModuleParameterSet(module, Properties.WidgetDisplayModule, widget);
                    homegenie.RaiseEvent(this, module.Domain, module.Address, "", Properties.WidgetDisplayModule, widget);
                }
            }
            return this;
        }

        /// <summary>
        /// Remove a module from the system.
        /// </summary>
        /// <returns>ProgramHelper.</returns>
        /// <param name="domain">Domain.</param>
        /// <param name="address">Address.</param>
        public ProgramHelper RemoveModule(string domain, string address)
        {
            VirtualModule oldModule = null;
            try
            {
                oldModule = homegenie.VirtualModules.Find(rm =>
                    rm.ParentId == myProgramId.ToString() && rm.Domain == domain && rm.Address == address);
            }
            catch
            {
            }

            if (oldModule != null)
            {
                homegenie.VirtualModules.Remove(oldModule);
                // remove from system modules all virtual ones generated by this program
                var module = homegenie.Modules.Find((m) =>
                {
                    var prop = Utility.ModuleParameterGet(m, Properties.VirtualModuleParentId);
                    if (prop != null && prop.Value == myProgramId.ToString(CultureInfo.InvariantCulture))
                    {
                        return m.Domain == oldModule.Domain && m.Address == oldModule.Address;
                    }
                    return false;
                });
                if (module != null)
                {
                    homegenie.Modules.Remove(module);
                }
                homegenie.RaiseEvent(this, myProgramDomain, myProgramId.ToString(), "", Properties.ProgramEvent, $"MODULE_REMOVED {oldModule.Domain}:{oldModule.Address}");
            }

            //
            homegenie.modules_RefreshVirtualModules();
            //homegenie.modules_Sort();
            return this;
        }
        
        [Obsolete("This method is deprecated, use AddModule(...) instead.")]
        public ProgramHelper AddVirtualModule(string domain, string address, string type, string widget)
        {
            AddModule(domain, address, type, widget);
            return this;
        }

        [Obsolete("This method is deprecated, use AddModules(...) instead.")]
        public ProgramHelper AddVirtualModules(
            string domain,
            string type,
            string widget,
            int startAddress,
            int endAddress)
        {
            AddModules(domain, startAddress, endAddress, type, widget);
            return this;
        }

        [Obsolete("This method is deprecated, use RemoveModule(...) instead.")]
        public ProgramHelper RemoveVirtualModule(string domain, string address)
        {
            RemoveModule(domain, address);
            return this;
        }

        /// <summary>
        /// Display UI notification message using the name of the program as default title for the notification.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// Program.Notify("Hello world!");
        /// </code>
        /// </example>
        public ProgramHelper Notify(string message)
        {
            var programBlock = GetProgramBlock();
            return Notify(programBlock.Name, message);
        }

        /// <summary>
        /// Display UI notification message using the name of the program as default title for the notification.
        /// </summary>
        /// <param name="message">Formatted message.</param>
        /// <param name="paramList">Format parameter list.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// Program.Notify("Hello world!");
        /// </code>
        /// </example>
        public ProgramHelper Notify(string message, params object[] paramList)
        {
            var programBlock = GetProgramBlock();
            return Notify(programBlock.Name, message, paramList);
        }

        /// <summary>
        /// Display UI notification message from current program.
        /// </summary>
        /// <param name="title">Title.</param>
        /// <param name="message">Message.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// Program.Notify("Test Program", "Hello world!");
        /// </code>
        /// </example>
        public ProgramHelper Notify(string title, string message)
        {
            dynamic notification = new ExpandoObject();
            notification.Title = title;
            notification.Message = message;
            string serializedMessage = JsonConvert.SerializeObject(notification);
            homegenie.RaiseEvent(
                myProgramId,
                Domains.HomeAutomation_HomeGenie_Automation,
                myProgramId.ToString(),
                "Automation Program",
                Properties.ProgramNotification,
                serializedMessage
            );
            return this;
        }

        /// <summary>
        /// Display UI notification message from current program.
        /// </summary>
        /// <param name="title">Title.</param>
        /// <param name="message">Formatted message.</param>
        /// <param name="paramList">Format parameter list.</param>
        public ProgramHelper Notify(string title, string message, params object[] paramList)
        {
            return Notify(title, String.Format(message, paramList));
        }

        /// <summary>
        /// Emits a new parameter value.
        /// </summary>
        /// <returns>ProgramHelper.</returns>
        /// <param name="parameter">Parameter name.</param>
        /// <param name="value">The new parameter value to set.</param>
        /// <param name="description">Event description. (optional)</param>
        public ProgramHelper Emit(string parameter, object value, string description = "")
        {
            try
            {
                var actionEvent = homegenie.MigService.GetEvent(
                    Module.Domain,
                    Module.Address,
                    description,
                    parameter,
                    value
                );
                homegenie.RaiseEvent(myProgramId, actionEvent);
                //homegenie.SignalModulePropertyChange(this, Module, actionEvent);
            }
            catch (Exception ex)
            {
                HomeGenieService.LogError(
                    Module.Domain,
                    Module.Address,
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }
            return this;
        }

        [Obsolete("This method is deprecated, use Emit(..) instead.")]
        public ProgramHelper RaiseEvent(string parameter, object value, string description)
        {
            Emit(parameter, value, description);
            return this;
        }

        /// <summary>
        /// This command is usually put at the end of the "Program Code". It is the equivalent of an infinite noop loop.
        /// </summary>
        public void GoBackground()
        {
            GetProgramBlock().IsEnabled = true;
            homegenie.RaiseEvent(
                myProgramId,
                myProgramDomain,
                myProgramId.ToString(),
                "Automation Program",
                Properties.ProgramStatus,
                "Background"
            );
            while (homegenie.ProgramManager.Enabled && this.IsEnabled)
            {
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Gets or sets a program parameter.
        /// </summary>
        /// <param name="parameterName">Parameter name.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// // sets an example program parameter
        /// Program.Parameter("MyProgram.ExampleParameter").Value = "Just testing...";
        ///
        /// // read "Sensor.Temperature" parameter of the program named "DHT-11"
        /// if (Program.WithName("DHT-11").Parameter("Sensor.Temperature").DecimalValue &lt; 20
        ///     && Modules.WithName("Heater").IsOff)
        /// {
        ///     Modules.WithName("Heater").On();
        /// }
        /// </code>
        /// </example>
        public ModuleParameter Parameter(string parameterName)
        {
            ModuleParameter parameter = null;
            try
            {
                parameter = Utility.ModuleParameterGet(Module, parameterName);
            }
            catch
            {
                // TODO: report exception
            }
            // create parameter if does not exists
            if (parameter == null)
            {
                parameter = Utility.ModuleParameterSet(Module, parameterName, "");
            }
            return parameter;
        }

        /// <summary>
        /// Gets or creates a persistent data Store for this program.
        /// </summary>
        /// <param name="storeName">Store name.</param>
        public StoreHelper Store(string storeName)
        {
            StoreHelper storage = null;
            if (Module != null)
            {
                storage = new StoreHelper(Module.Stores, storeName);
            }
            return storage;
        }

        /// <summary>
        /// Gets a value indicating whether the program is running.
        /// </summary>
        /// <value>
        /// <c>true</c> if this program is running; otherwise, <c>false</c>.
        /// </value>
        public bool IsRunning
        {
            get
            {
                return GetProgramBlock().IsRunning;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the program is enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if this program is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsEnabled
        {
            get
            {
                return GetProgramBlock().IsEnabled;
            }
        }

        /// <summary>
        /// Get a reference to the Module associated to the program.
        /// </summary>
        /// <value>
        /// Module object associated to the program.
        /// </value>
        public Module Module
        {
            get
            {

                if (programModule == null || programModule != homegenie.Modules.Find(rm =>
                        rm.Domain == myProgramDomain && rm.Address == myProgramId.ToString(CultureInfo.InvariantCulture)))
                {
                    // force automation modules regeneration
                    homegenie.modules_RefreshPrograms();
                    //
                    try
                    {
                        programModule = homegenie.Modules.Find(rm =>
                            rm.Domain == myProgramDomain && rm.Address == myProgramId.ToString(CultureInfo.InvariantCulture));
                    }
                    catch (Exception ex)
                    {
                        HomeGenieService.LogError(
                            myProgramDomain,
                            myProgramId.ToString(CultureInfo.InvariantCulture),
                            ex.Message,
                            "Exception.StackTrace",
                            ex.StackTrace
                        );
                    }
                }
                
                return programModule;
            }
        }

        /// <summary>
        /// Force update module database with current data.
        /// </summary>
        public bool UpdateModuleDatabase()
        {
            return homegenie.UpdateModulesDatabase();
        }

        public ProgramHelper Reset()
        {
            initialized = false;
            // no control widget --> not visible
            UseWidget("");
            // remove all features
            var program = GetProgramBlock();
            if (program != null)
            {
                program.WillRun = false;
                // clear implemented features
                program.Features.Clear();
                // clear implemented interfaces
                program.ImplementedInterfaces.Clear();
            }
            // set virtual modules generated by this program to inactive state
            homegenie.VirtualModules.ForEach((vm) =>
            {
                if ((Module == null || (vm.Domain != Module.Domain && vm.Address != Module.Address)) && vm.ParentId == myProgramId.ToString(CultureInfo.InvariantCulture))
                {
                    vm.IsActive = false;
                }
            });
            // remove from system modules all virtual ones generated by this program
            homegenie.Modules.RemoveAll((m) =>
            {
                var prop = Utility.ModuleParameterGet(m, Properties.VirtualModuleParentId);
                if (prop != null && prop.Value == myProgramId.ToString(CultureInfo.InvariantCulture))
                {
                    if (m.Domain == myProgramDomain && m.Address == myProgramId.ToString(CultureInfo.InvariantCulture))
                    {
                        return false;
                    }
                    // copy properties from instance module to virtual module (if found) before deleting instance module
                    var virtualModule = homegenie.VirtualModules.Find((vm) =>
                        vm.Domain == m.Domain && vm.Address == m.Address && vm.ParentId == prop.Value);
                    if (virtualModule != null)
                    {
                        virtualModule.Name = m.Name;
                        virtualModule.DeviceType = m.DeviceType;
                        virtualModule.Properties.Clear();
                        foreach (var p in m.Properties)
                        {
                            virtualModule.Properties.Add(p);
                        }
                    }
                    return true;
                }
                return false;
            });
            return this;
        }

        public ProgramHelperBase Restart()
        {
            var program = GetProgramBlock();
            if (program != null)
            {
                program.IsEnabled = false;
                try
                {
                    program.Engine.StopProgram();
                }
                catch
                {
                }
                program.IsEnabled = true;
                homegenie.UpdateProgramsDatabase();
            }
            return this;
        }


        private ProgramBlock GetProgramBlock()
        {
            return homegenie.ProgramManager.GetProgram(myProgramId);
        }

        private VirtualModule AddProgramModule(string domain, string address, string type, string widget, string[] implementedFeatures = null)
        {
            VirtualModule virtualModule = null;
            try
            {
                virtualModule = homegenie.VirtualModules.Find(rm => rm.ParentId == myProgramId.ToString() && rm.Domain == domain && rm.Address == address);
            }
            catch
            {
                Debugger.Break();
            }
            //
            if (virtualModule == null)
            {
                virtualModule = new VirtualModule() {
                    ParentId = myProgramId.ToString(),
                    Domain = domain,
                    Address = address,
                    DeviceType = (ModuleTypes)Enum.Parse(
                        typeof(ModuleTypes),
                        type
                    )
                };
                virtualModule.Properties.Add(new ModuleParameter() {
                    Name = Properties.WidgetDisplayModule,
                    Value = widget
                });
                homegenie.VirtualModules.Add(virtualModule);
            }
            else
            {
                virtualModule.IsActive = true;
                virtualModule.Domain = domain;
                if (virtualModule.DeviceType == ModuleTypes.Generic)
                    virtualModule.DeviceType = (ModuleTypes)Enum.Parse(typeof(ModuleTypes), type);
                Utility.ModuleParameterSet(virtualModule, Properties.WidgetDisplayModule, widget);
            }
            homegenie.RaiseEvent(this, myProgramDomain, myProgramId.ToString(), "", Properties.ProgramEvent, $"MODULE_ADDED {virtualModule.Domain}:{virtualModule.Address}");

            if (implementedFeatures != null)
            {
                virtualModule.ImplementFeatures = new List<string>(implementedFeatures);
            }
            return virtualModule;
        }

    }
}
