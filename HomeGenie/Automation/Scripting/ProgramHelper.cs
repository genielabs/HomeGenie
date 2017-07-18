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

using HomeGenie.Data;
using HomeGenie.Service;
using System.Threading;
using System.Net;
using System.IO;
using HomeGenie.Service.Constants;
using System.Dynamic;
using Newtonsoft.Json;
using MIG;

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
        private int myProgramId = -1;
        private string myProgramDomain = Domains.HomeAutomation_HomeGenie_Automation;
        private object setupLock = new object();

        //private string parameter = "";
        //private string value = "";

        private bool initialized = false;
        // if setup has been done

        public ProgramHelper(HomeGenieService hg, int programId) : base(hg)
        {
            myProgramId = programId;
            //
            //var oldModule = homegenie.VirtualModules.Find(rm => rm.ParentId == myProgramId.ToString() && rm.Address == myProgramId.ToString());
            //if (oldModule == null)
            //{
            //    AddControlWidget(""); // no control widget --> not visible
            //}
            // reset features and other values
            //Reset();
        }

        /// <summary>
        /// Execute a setup function when the program is enabled. It is meant to be used in the "Startup Code" to execute only once
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
            lock(setupLock)
            {
                if (!this.initialized)
                {
                    if (programModule == null) RelocateProgramModule();
                    // mark config options to determine unused ones
                    if (programModule != null)
                    {
                        foreach (var parameter in programModule.Properties)
                        {
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
                    if (programModule != null)
                    {
                        var parameterList = programModule.Properties.FindAll(mp => mp.Name.StartsWith("ConfigureOptions."));
                        foreach (var parameter in parameterList)
                        {
                            if (parameter.Name.StartsWith("ConfigureOptions.") && parameter.Description == null)
                            {
                                programModule.Properties.Remove(parameter);
                            }
                        }
                    }
                    this.initialized = true;

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

        public void Run()
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            if (program != null)
            {
                program.WillRun = true;
            }
        }

        /// <summary>
        /// Run the program as soon as the "Startup Code" exits. This command is meant to be used in the "Startup Code".
        /// </summary>
        /// <param name="willRun">If set to <c>true</c> will run.</param>
        public void Run(bool willRun = true)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            if (program != null)
            {
                program.WillRun = willRun;
            }
        }

        /// <summary>
        /// Set the widget that will be used for displaying this program data in the UI Control page.
        /// </summary>
        /// <returns>ProgramHelper.</returns>
        /// <param name="widget">The widget path.</param>
        public ProgramHelper UseWidget(string widget)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address == myProgramId);
            var module = homegenie.VirtualModules.Find(rm => rm.ParentId == myProgramId.ToString() && rm.Domain == myProgramDomain && rm.Address == myProgramId.ToString());
            //
            if (module == null)
            {
                module = new VirtualModule() {
                    ParentId = myProgramId.ToString(),
                    Domain = myProgramDomain,
                    Address = myProgramId.ToString(),
                    Name = (program != null ? program.Name : ""),
                    DeviceType = MIG.ModuleTypes.Program
                };
                homegenie.VirtualModules.Add(module);
            }
            //
            module.Name = (program != null ? program.Name : "");
            module.Domain = myProgramDomain;
            Utility.ModuleParameterSet(module, Properties.WidgetDisplayModule, widget);
            //
            RelocateProgramModule();
            homegenie.modules_RefreshVirtualModules();
            homegenie.modules_Sort();
            //
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
        /// that are specified as ":" separated items. See html/ui/widgets folder for a list of possible types/options.
        /// </param>
        public ProgramHelper AddOption(string field, string defaultValue, string description, string type)
        {
            var parameter = this.Parameter("ConfigureOptions." + field);
            if (parameter.Value == "") parameter.Value = defaultValue;
            parameter.Description = description;
            parameter.FieldType = type;
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
            return this.Parameter("ConfigureOptions." + field);
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
        /// A string with comma separated list of module domains that will showing this input field. Use an empty string for all domains.
        /// </param>
        /// <param name='forModuleTypes'>
        /// A string with comma separated list of module types that will be showing this input field.
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
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            ProgramFeature feature = null;
            //
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
            //
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
        /// <param name="propertyName">Parameter name.</param>
        public ProgramFeature Feature(string parameterName)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            ProgramFeature feature = null;
            //
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
            //
            return feature;
        }

        #region DEPRECATED

        [Obsolete("use 'Program.UseWidget(<widget>)' instead")]
        public ProgramHelper AddControlWidget(string widget)
        {
            this.UseWidget(widget);
            return this;
        }

        [Obsolete("use 'Program.AddOption(<field>, <defaultValue>, <description>, <type>)' instead")]
        public ProgramHelper AddInputField(string field, string defaultValue, string description)
        {
            this.AddOption(field, defaultValue, description, "text");
            return this;
        }

        [Obsolete("use 'Program.Option' instead")]
        public ModuleParameter InputField(string field)
        {
            return this.Parameter("ConfigureOptions." + field);
        }

        [Obsolete("use 'AddFeature(<forDomains>, <forTypes>, <forPropertyName>, <description>, \"checkbox\")' instead")]
        public ProgramHelper AddFeature(
            string forDomains,
            string forModuleTypes,
            string propertyName,
            string description
        )
        {
            return AddFeature(forDomains, forModuleTypes, propertyName, description, "checkbox");
        }
        [Obsolete("use 'AddFeature(<forDomains>, <forTypes>, <forPropertyName>, <description>, <type>)' instead")]
        public ProgramHelper AddFeature(string forModuleTypes, string propertyName, string description) // default type = checkbox
        {
            return AddFeature("", forModuleTypes, propertyName, description, "checkbox");
        }
        [Obsolete("use 'AddFeature(<forDomains>, <forTypes>, <forPropertyName>, <description>, \"text\")' instead")]
        public ProgramHelper AddFeatureTextInput(
            string forDomain,
            string forModuleTypes,
            string propertyName,
            string description
        )
        {
            return AddFeature(forDomain, forModuleTypes, propertyName, description, "text");
        }
        [Obsolete("use 'AddFeature(\"\", <forTypes>, <forPropertyName>, <description>, \"text\")' instead")]
        public ProgramHelper AddFeatureTextInput(string forModuleTypes, string propertyName, string description)
        {
            return AddFeature("", forModuleTypes, propertyName, description, "text");
        }

        #endregion DEPRECATED

        /// <summary>
        /// Adds a new virtual module to the system.
        /// </summary>
        /// <returns>ProgramHelper.</returns>
        /// <param name="domain">Domain.</param>
        /// <param name="address">Address.</param>
        /// <param name="type">Type (Generic, Program, Switch, Light, Dimmer, Sensor, Temperature, Siren, Fan, Thermostat, Shutter, DoorWindow, MediaTransmitter, MediaReceiver).</param>
        /// <param name="widget">Empty string or the path of the widget to be associated to the virtual module.</param>
        public ProgramHelper AddVirtualModule(string domain, string address, string type, string widget)
        {
            VirtualModule virtualModule = null;
            try
            {
                virtualModule = homegenie.VirtualModules.Find(rm => rm.ParentId == myProgramId.ToString() && rm.Domain == domain && rm.Address == address);
            }
            catch
            {
            }
            //
            if (virtualModule == null)
            {
                virtualModule = new VirtualModule() {
                    ParentId = myProgramId.ToString(),
                    Domain = domain,
                    Address = address,
                    DeviceType = (MIG.ModuleTypes)Enum.Parse(
                        typeof(MIG.ModuleTypes),
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
                virtualModule.Domain = domain;
                if (virtualModule.DeviceType == MIG.ModuleTypes.Generic) 
                    virtualModule.DeviceType = (MIG.ModuleTypes)Enum.Parse(typeof(MIG.ModuleTypes), type);
                Utility.ModuleParameterSet(virtualModule, Properties.WidgetDisplayModule, widget);
            }
            // update real module device type and widget
            Module module = homegenie.Modules.Find(o => o.Domain == virtualModule.Domain && o.Address == virtualModule.Address);
            if (module != null)
            {
                if (module.DeviceType == MIG.ModuleTypes.Generic) 
                    module.DeviceType = virtualModule.DeviceType;
                Utility.ModuleParameterSet(module, Properties.WidgetDisplayModule, widget);
            }
            //
            homegenie.modules_RefreshVirtualModules();
            homegenie.modules_Sort();
            return this;
        }

        /// <summary>
        /// Remove a virtual module from the system.
        /// </summary>
        /// <returns>ProgramHelper.</returns>
        /// <param name="domain">Domain.</param>
        /// <param name="address">Address.</param>
        public ProgramHelper RemoveVirtualModule(string domain, string address)
        {
            VirtualModule oldModule = null;
            try
            {
                oldModule = homegenie.VirtualModules.Find(rm => rm.ParentId == myProgramId.ToString() && rm.Domain == domain && rm.Address == address);
            }
            catch
            {
            }
            if (oldModule != null)
            {
                homegenie.VirtualModules.Remove(oldModule);
            }
            //
            homegenie.modules_RefreshVirtualModules();
            homegenie.modules_Sort();
            return this;
        }

        /// <summary>
        /// Adds a new set of virtual modules to the system.
        /// </summary>
        /// <returns>ProgramHelper.</returns>
        /// <param name="domain">Domain.</param>
        /// <param name="type">Type.</param>
        /// <param name="widget">Empty string or the path of the widget to be associated to the virtual module.</param>
        /// <param name="startAddress">Start address (numeric).</param>
        /// <param name="endAddress">End address (numeric).</param>
        public ProgramHelper AddVirtualModules(
            string domain,
            string type,
            string widget,
            int startAddress,
            int endAddress
        )
        {
            for (int x = startAddress; x <= endAddress; x++)
            {

                VirtualModule virtualModule = null;
                try
                {
                    virtualModule = homegenie.VirtualModules.Find(rm => rm.ParentId == myProgramId.ToString() && rm.Domain == domain && rm.Address == x.ToString());
                }
                catch
                {
                }
                //
                if (virtualModule == null)
                {
                    virtualModule = new VirtualModule() {
                        ParentId = myProgramId.ToString(),
                        Domain = domain,
                        Address = x.ToString(),
                        DeviceType = (MIG.ModuleTypes)Enum.Parse(
                            typeof(MIG.ModuleTypes),
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
                    virtualModule.Domain = domain;
                    if (virtualModule.DeviceType == MIG.ModuleTypes.Generic) 
                        virtualModule.DeviceType = (MIG.ModuleTypes)Enum.Parse(typeof(MIG.ModuleTypes), type);
                    Utility.ModuleParameterSet(virtualModule, Properties.WidgetDisplayModule, widget);
                }
                // update real module device type and widget
                Module module = homegenie.Modules.Find(o => o.Domain == virtualModule.Domain && o.Address == virtualModule.Address);
                if (module != null)
                {
                    if (module.DeviceType == MIG.ModuleTypes.Generic) 
                        module.DeviceType = virtualModule.DeviceType;
                    Utility.ModuleParameterSet(module, Properties.WidgetDisplayModule, widget);
                }

            }
            homegenie.modules_RefreshVirtualModules();
            homegenie.modules_Sort();
            return this;
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
            return this.Notify(title, String.Format(message, paramList));
        }

        /// <summary>
        /// Raise a parameter event and set the parameter with the specified value. 
        /// </summary>
        /// <returns>ProgramHelper.</returns>
        /// <param name="parameter">Parameter name.</param>
        /// <param name="value">The new parameter value to set.</param>
        /// <param name="description">Event description.</param>
        public ProgramHelper RaiseEvent(string parameter, string value, string description)
        {
            if (programModule == null) RelocateProgramModule();
            try
            {
                var actionEvent = homegenie.MigService.GetEvent(
                    programModule.Domain,
                    programModule.Address,
                    description,
                    parameter,
                    value
                );
                homegenie.RaiseEvent(myProgramId, actionEvent);
                //homegenie.SignalModulePropertyChange(this, programModule, actionEvent);
            }
            catch (Exception ex)
            {
                HomeGenieService.LogError(
                    programModule.Domain,
                    programModule.Address,
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }
            return this;
        }

        /// <summary>
        /// Raise a module parameter event and set the parameter with the specified value. 
        /// </summary>
        /// <returns>ProgramHelper.</returns>
        /// <param name="module">The module source of this event.</param>
        /// <param name="parameter">Parameter name.</param>
        /// <param name="value">The new parameter value to set.</param>
        /// <param name="description">Event description.</param>
        public ProgramHelper RaiseEvent(ModuleHelper sourceModule, string parameter, string value, string description)
        {
            // TODO: deprecate this method, use ModuleHelper.RaiseEvent instead
            try
            {
                var actionEvent = homegenie.MigService.GetEvent(
                    sourceModule.Instance.Domain,
                    sourceModule.Instance.Address,
                    description,
                    parameter,
                    value
                );
                homegenie.RaiseEvent(myProgramId, actionEvent);
            }
            catch (Exception ex)
            {
                HomeGenieService.LogError(
                    programModule.Domain,
                    programModule.Address,
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }
            return this;
        }

        /// <summary>
        /// This command is usually put at the end of the "Program Code". It is the equivalent of an infinite noop loop.
        /// </summary>
        public void GoBackground()
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address == myProgramId);
            program.IsEnabled = true;
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
        /// if (Program.WithName("DHT-11").Parameter("Sensor.Temperature").DecimalValue < 20 
        ///     && Modules.WithName("Heater").IsOff)
        /// {
        ///     Modules.WithName("Heater").On();
        /// }
        /// </code>
        /// </example>
        public ModuleParameter Parameter(string parameterName)
        {
            if (programModule == null) RelocateProgramModule();
            ModuleParameter parameter = null;
            try
            {
                parameter = Service.Utility.ModuleParameterGet(programModule, parameterName);
            }
            catch
            {
                // TODO: report exception
            }
            // create parameter if does not exists
            if (parameter == null)
            {
                parameter = Service.Utility.ModuleParameterSet(programModule, parameterName, "");
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
            if (programModule == null) RelocateProgramModule();
            if (this.programModule != null)
            {
                storage = new StoreHelper(this.programModule.Stores, storeName);
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
                var program = homegenie.ProgramManager.Programs.Find(p => p.Address == myProgramId);
                return program.IsRunning;
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
                var program = homegenie.ProgramManager.Programs.Find(p => p.Address == myProgramId);
                return program.IsEnabled;
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
                RelocateProgramModule();
                return programModule;
            }
        }

        public ProgramHelper Reset()
        {
            this.initialized = false;
            // no control widget --> not visible
            this.UseWidget(""); 
            // remove all features 
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            if (program != null)
            {
                program.WillRun = false;
                program.Features.Clear();
            }
            return this;
        }


        private void RelocateProgramModule()
        {
            // force automation modules regeneration
            homegenie.modules_RefreshPrograms();
            //
            try
            {
                programModule = homegenie.Modules.Find(rm => rm.Domain == myProgramDomain && rm.Address == myProgramId.ToString());
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
        }

    }
}
