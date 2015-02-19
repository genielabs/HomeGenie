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

namespace HomeGenie.Automation.Scripting
{
    /// <summary>
    /// Program Helper class.\n
    /// Class instance accessor: **Program**
    /// </summary>
    public class ProgramHelper
    {
        private HomeGenieService homegenie;
        private Module programModule;
        private int myProgramId = -1;
        private string myProgramDomain = Domains.HomeAutomation_HomeGenie_Automation;

        //private string parameter = "";
        //private string value = "";

        private bool initialized = false;
        // if setup has been done

        public ProgramHelper(HomeGenieService hg, int programId)
        {
            homegenie = hg;
            myProgramId = programId;
            //
            //var oldModule = homegenie.VirtualModules.Find(rm => rm.ParentId == myProgramId.ToString() && rm.Address == myProgramId.ToString());
            //if (oldModule == null)
            //{
            //    AddControlWidget(""); // no control widget --> not visible
            //}
            // reset features and other values
            Reset();
        }

        /// <summary>
        /// Execute a setup function once the program is enabled. It is meant to be used in the "Trigger Code Block" to configure program configuration fields, parameters and features.
        /// </summary>
        /// <param name="functionBlock">Function name or inline delegate.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        ///  Program.Setup(()=>
        ///  {              
        ///      Program.AddInputField(
        ///          "MaxLevel",
        ///          "40", 
        ///          "Keep level below the following value");
        ///      Program.AddFeature(
        ///          "Dimmer",
        ///          "EnergyManagement.EnergySavingMode", 
        ///          "Energy Saving Mode enabled light");
        ///  });
        /// </code>
        /// </example>
        public void Setup(Action functionBlock)
        {
            try
            {
                if (!this.initialized)
                {
                    //
                    if (programModule == null) RelocateProgramModule();
                    //
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
                    //
                    HomeGenieService.LogEvent(
                        myProgramDomain,
                        myProgramId.ToString(),
                        "Automation Program",
                        Properties.PROGRAM_STATUS,
                        "Setup"
                    );
                    functionBlock();
                    //
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
                    //
                    homegenie.modules_RefreshPrograms();
                    homegenie.modules_RefreshVirtualModules();
                }
            }
            catch (Exception e)
            {
                //TODO: report error
                throw (new Exception(e.StackTrace));
            }
        }

        /// <summary>
        /// Set the widget that will be used for displaying this program data in the UI Control page.
        /// </summary>
        /// <returns>ProgramHelper.</returns>
        /// <param name="widget">The widget path.</param>
        public ProgramHelper AddControlWidget(string widget)
        {
            var program = homegenie.ProgramEngine.Programs.Find(p => p.Address == myProgramId);
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
            Utility.ModuleParameterSet(module, Properties.WIDGET_DISPLAYMODULE, widget);
            //
            RelocateProgramModule();
            homegenie.modules_RefreshVirtualModules();
            homegenie.modules_Sort();
            //
            return this;
        }

        /// <summary>
        /// Adds a configuration input field for the program. The input field will be displayed in the program options dialog as a text input box.
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
        public ProgramHelper AddInputField(string field, string defaultValue, string description)
        {
            var parameter = this.Parameter("ConfigureOptions." + field);
            if (parameter.Value == "") parameter.Value = defaultValue;
            parameter.Description = description;
            return this;
        }

        /// <summary>
        /// Gets the value of a program input field.
        /// </summary>
        /// <returns>
        /// The input field.
        /// </returns>
        /// <param name='field'>
        /// Name of the input field to get.
        /// </param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// // ....
        /// var delay = Program.InputField("OffDelay").DecimalValue;
        /// Pause( delay ); 
        /// Modules.WithFeature("Timer.TurnOffDelay").Off();
        /// </code>
        /// </example>
        public ModuleParameter InputField(string field)
        {
            return this.Parameter("ConfigureOptions." + field);
        }


        /// <summary>
        /// Adds a "feature" field of type "checkbox" or "text" to the matching domain/type modules. This field will be showing in the module options popup and it will be bound to the given module parameter.
        /// This command should only appear inside a Program.Setup delegate.
        /// </summary>
        /// <returns>
        /// ProgramHelper.
        /// </returns>
        /// <param name='forDomains'>
        /// A string with comma separated list of domains of modules that will showing this input field. Use an empty string for all domains.
        /// </param>
        /// <param name='forModuleTypes'>
        /// A string with comma separated list of types of modules that will showing this input field.
        /// </param>
        /// <param name='parameterName'>
        /// Name of the module parameter associated to this input field.
        /// </param>
        /// <param name='description'>
        /// Description for this input field.
        /// </param>
        public ProgramHelper AddFeature(
            string forDomains,
            string forModuleTypes,
            string parameterName,
            string description,
            string type
        )
        {
            var program = homegenie.ProgramEngine.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            ProgramFeature feature = null;
            //
            try
            {
                feature = program.Features.Find(f => f.Property == parameterName);
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(
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
        /// Return the ProgramFeature object associated to the specified module parameter.
        /// </summary>
        /// <param name="propertyName">Parameter name.</param>
        public ProgramFeature Feature(string parameterName)
        {
            var program = homegenie.ProgramEngine.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            ProgramFeature feature = null;
            //
            try
            {
                feature = program.Features.Find(f => f.Property == parameterName);
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(
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






        //TODO: deprecate this?
        public ProgramHelper AddFeature(
            string forDomains,
            string forModuleTypes,
            string propertyName,
            string description
        )
        {
            return AddFeature(forDomains, forModuleTypes, propertyName, description, "checkbox");
        }
        //TODO: deprecate this?
        public ProgramHelper AddFeature(string forModuleTypes, string propertyName, string description) // default type = checkbox
        {
            return AddFeature("", forModuleTypes, propertyName, description, "checkbox");
        }
        //TODO: deprecate this?
        public ProgramHelper AddFeatureTextInput(
            string forDomain,
            string forModuleTypes,
            string propertyName,
            string description
        )
        {
            return AddFeature(forDomain, forModuleTypes, propertyName, description, "text");
        }
        //TODO: deprecate this?
        public ProgramHelper AddFeatureTextInput(string forModuleTypes, string propertyName, string description)
        {
            return AddFeature("", forModuleTypes, propertyName, description, "text");
        }





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
                virtualModule = homegenie.VirtualModules.Find(rm => rm.ParentId == myProgramId.ToString() && rm.Address == address);
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
                    Name = Properties.WIDGET_DISPLAYMODULE,
                    Value = widget
                });
                homegenie.VirtualModules.Add(virtualModule);
            }
            else
            {
                virtualModule.Domain = domain;
                virtualModule.DeviceType = (MIG.ModuleTypes)Enum.Parse(typeof(MIG.ModuleTypes), type);
                Utility.ModuleParameterSet(virtualModule, Properties.WIDGET_DISPLAYMODULE, widget);
            }
            // update real module device type and widget
            Module module = homegenie.Modules.Find(o => o.Domain == virtualModule.Domain && o.Address == virtualModule.Address);
            if (module != null)
            {
                module.DeviceType = virtualModule.DeviceType;
                Utility.ModuleParameterSet(module, Properties.WIDGET_DISPLAYMODULE, widget);
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
                oldModule = homegenie.VirtualModules.Find(rm => rm.ParentId == myProgramId.ToString() && rm.Address == address);
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
                    virtualModule = homegenie.VirtualModules.Find(rm => rm.ParentId == myProgramId.ToString() && rm.Address == x.ToString());
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
                        Name = Properties.WIDGET_DISPLAYMODULE,
                        Value = widget
                    });
                    homegenie.VirtualModules.Add(virtualModule);
                }
                else
                {
                    virtualModule.Domain = domain;
                    virtualModule.DeviceType = (MIG.ModuleTypes)Enum.Parse(typeof(MIG.ModuleTypes), type);
                    Utility.ModuleParameterSet(virtualModule, Properties.WIDGET_DISPLAYMODULE, widget);
                }
                // update real module device type and widget
                Module module = homegenie.Modules.Find(o => o.Domain == virtualModule.Domain && o.Address == virtualModule.Address);
                if (module != null)
                {
                    module.DeviceType = virtualModule.DeviceType;
                    Utility.ModuleParameterSet(module, Properties.WIDGET_DISPLAYMODULE, widget);
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
            homegenie.LogBroadcastEvent(
                Domains.HomeAutomation_HomeGenie_Automation,
                myProgramId.ToString(),
                "Automation Program",
                Properties.PROGRAM_NOTIFICATION,
                serializedMessage
            );
            return this;
        }

        /// <summary>
        /// Playbacks a synthesized voice message from speaker.
        /// </summary>
        /// <param name="sentence">Message to output.</param>
        /// <param name="locale">Language locale string (eg. "en-US", "it-IT", "en-GB", "nl-NL",...).</param>
        /// <param name="goAsync">If true, the command will be executed asyncronously.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// Program.Say("The garage door has been opened", "en-US");
        /// </code>
        /// </example>
        public void Say(string sentence, string locale, bool goAsync = false)
        {
            Utility.Say(sentence, locale, goAsync);
        }

        /// <summary>
        /// Playbacks a wave file.
        /// </summary>
        /// <param name="waveUrl">URL of the audio wave file to play.</param>
        public void Play(string waveUrl)
        {
            var webClient = new WebClient();
            byte[] audiodata = webClient.DownloadData(waveUrl);
            webClient.Dispose();

            string outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_tmp");
            if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);
            string file = Path.Combine(outputDirectory, "_wave_tmp." + Path.GetExtension(waveUrl));
            if (File.Exists(file)) File.Delete(file);

            var stream = File.OpenWrite(file);
            stream.Write(audiodata, 0, audiodata.Length);
            stream.Close();

            Utility.Play(file);

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
            try
            {
                var actionEvent = new MIG.InterfacePropertyChangedAction();
                actionEvent.Domain = programModule.Domain;
                actionEvent.Path = parameter;
                actionEvent.Value = value;
                actionEvent.SourceId = programModule.Address;
                actionEvent.SourceType = "Automation Program";
                homegenie.SignalModulePropertyChange(this, programModule, actionEvent);
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(
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
        /// <param name="module">Module object.</param>
        /// <param name="parameter">Parameter name.</param>
        /// <param name="value">The new parameter value to set.</param>
        /// <param name="description">Event description.</param>
        public ProgramHelper RaiseEvent(ModuleHelper module, string parameter, string value, string description)
        {
            try
            {
                var actionEvent = new MIG.InterfacePropertyChangedAction();
                actionEvent.Domain = module.Instance.Domain;
                actionEvent.Path = parameter;
                actionEvent.Value = value;
                actionEvent.SourceId = module.Instance.Address;
                actionEvent.SourceType = "Virtual Module";
                homegenie.SignalModulePropertyChange(this, module.Instance, actionEvent);
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(
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
        /// This command is usually put at the end of the "Code to Run". It is the equivalent of an infinite noop loop.
        /// </summary>
        public void GoBackground()
        {
            while (this.IsEnabled)
            {
                Thread.Sleep(5000);
            }
        }

        /// <summary>
        /// Executes a function asynchronously.
        /// </summary>
        /// <returns>
        /// The Thread object of this asynchronous task.
        /// </returns>
        /// <param name='functionBlock'>
        /// Function name or inline delegate.
        /// </param>
        public Thread RunAsyncTask(Utility.AsyncFunction functionBlock)
        {
            return Utility.RunAsyncTask(functionBlock);
        }

        /// <summary>
        /// Executes the specified Automation Program.
        /// </summary>
        /// <param name='programId'>
        /// Program name or ID.
        /// </param>
        public void Run(string programId)
        {
            Run(programId, "");
        }

        /// <summary>
        /// Executes the specified Automation Program.
        /// </summary>
        /// <param name='programId'>
        /// Program name or ID.
        /// </param>
        /// <param name='options'>
        /// Program options.
        /// </param>
        public void Run(string programId, string options)
        {
            var program = homegenie.ProgramEngine.Programs.Find(p => p.Address.ToString() == programId || p.Name == programId);
            if (program != null && program.Address != myProgramId && !program.IsRunning)
            {
                homegenie.ProgramEngine.Run(program, options);
            }
        }

        /// <summary>
        /// Returns a reference to the ProgramHelper of a program.
        /// </summary>
        /// <returns>ProgramHelper.</returns>
        /// <param name="programName">Program name.</param>
        public ProgramHelper WithName(string programName)
        {
            var program = homegenie.ProgramEngine.Programs.Find(p => p.Name.ToLower() == programName.ToLower());
            ProgramHelper programHelper = null;
            if (program != null)
            {
                programHelper = new ProgramHelper(homegenie, program.Address);
            }
            return programHelper;
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
            //
            if (programModule == null) RelocateProgramModule();
            //
            ModuleParameter parameter = null;
            try
            {
                parameter = Service.Utility.ModuleParameterGet(programModule, parameterName);
            }
            catch
            {
            }
            // create parameter if does not exists
            if (parameter == null)
            {
                parameter = Service.Utility.ModuleParameterSet(programModule, parameterName, "");
            }
            return parameter;
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
                var program = homegenie.ProgramEngine.Programs.Find(p => p.Address == myProgramId);
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
                var program = homegenie.ProgramEngine.Programs.Find(p => p.Address == myProgramId);
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



        // TODO: find a better place for this and deprecate it
        public double EnergyUseCounter
        {
            get
            {
                return homegenie.Statistics != null ? homegenie.Statistics.GetTotalCounter(Properties.METER_WATTS, 3600) : 0;
            }
        }


        public ProgramHelper Reset()
        {
            //this.parameter = "";
            //this.value = "";
            this.initialized = false;
            //
            AddControlWidget(""); // no control widget --> not visible
            //
            // remove all features 
            //
            var program = homegenie.ProgramEngine.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            if (program != null)
            {
                program.Features.Clear();
            }
            //
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
                HomeGenieService.LogEvent(
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
