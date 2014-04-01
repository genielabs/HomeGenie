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

namespace HomeGenie.Automation.Scripting
{

    public class ProgramHelper
    {
        private HomeGenieService homegenie;
        private Module programModule;
        private int myProgramId = -1;
        private string myProgramDomain = Domains.HomeAutomation_HomeGenie_Automation;

        private string parameter = "";
        private string value = "";

        private bool initialized = false; // if setup has been done

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
        /// Run an Automation Program
        /// </summary>
        /// <param name='programId'>
        /// Name or ID of program to run
        /// </param>
        public void Run(string programId)
        {
            Run(programId, "");
        }

        public void Run(string programId, string options)
        {
            //TODO: improve locking for single instance run only
            var program = homegenie.ProgramEngine.Programs.Find(p => p.Address.ToString() == programId || p.Name == programId);
            program.IsRunning = true;
            if (program.Type.ToLower() == "csharp")
            {
                program.Run(options);
            }
            else if (program.Type.ToLower() == "wizard")
            {
                homegenie.ProgramEngine.ExecuteWizardScript(program);
            }
            else
            {
                // Run IronScript
            }
            program.IsRunning = false;
        }

        /// <summary>
        /// Run a given function in the background
        /// </summary>
        /// <returns>
        /// The Thread for this background task
        /// </returns>
        /// <param name='functionBlock'>
        /// Function name or inline delegate
        /// </param>
        public Thread RunAsyncTask(Utility.AsyncFunction functionBlock)
        {
            return Utility.RunAsyncTask(functionBlock);
        }

        /// <summary>
        /// Gets a value indicating whether the current program is running.
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
        /// Gets a value indicating whether this program is enabled.
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

        public void GoBackground()
        {
            while (this.IsEnabled)
            {
                Thread.Sleep(5000);
            }
        }

        /// <summary>
        /// Adds an input field to the program options dialog.
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
        /// Get a program input field.
        /// </summary>
        /// <returns>
        /// The input field.
        /// </returns>
        /// <param name='field'>
        /// Name of the input field to get.
        /// </param>
        public ModuleParameter InputField(string field)
        {
            return this.Parameter("ConfigureOptions." + field);
        }

        /// <summary>
        /// Get a reference to the Module associated to this program
        /// </summary>
        /// <value>
        /// Program module.
        /// </value>
        public Module Module
        {
            get
            {
                RelocateProgramModule();
                return programModule;
            }
        }

        public ProgramHelper AddFeature(string forDomains, string forModuleTypes, string propertyName, string description, string type) // default type = checkbox
        {
            var program = homegenie.ProgramEngine.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            ProgramFeature feature = null;
            //
            try { feature = program.Features.Find(f => f.Property == propertyName); }
            catch { }
            //
            if (feature == null)
            {
                feature = new ProgramFeature();
                program.Features.Add(feature);
            }
            feature.FieldType = type;
            feature.Property = propertyName;
            feature.Description = description;
            feature.ForDomains = forDomains;
            feature.ForTypes = forModuleTypes;
            return this;
        }

        public ProgramFeature Feature(string propertyName)
        {
            var program = homegenie.ProgramEngine.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            ProgramFeature feature = null;
            //
            try { feature = program.Features.Find(f => f.Property == propertyName); }
            catch { }
            //
            return feature;
        }

        /// <summary>
        /// Adds a checkbox option to modules' option popup. 
        /// </summary>
        /// <returns>
        /// ProgramHelper
        /// </returns>
        /// <param name='forDomains'>
        /// A string with comma separated list of the module's domains that will show this checkbox
        /// </param>
        /// <param name='forModuleTypes'>
        /// A string with comma separated list of the module's types that will show this checkbox
        /// </param>
        /// <param name='propertyName'>
        /// Name for this checkbox option
        /// </param>
        /// <param name='description'>
        /// Description for this checkbox option
        /// </param>
        public ProgramHelper AddFeature(string forDomains, string forModuleTypes, string propertyName, string description)
        {
            return AddFeature(forDomains, forModuleTypes, propertyName, description, "checkbox");
        }

        public ProgramHelper AddFeature(string forModuleTypes, string propertyName, string description) // default type = checkbox
        {
            return AddFeature("", forModuleTypes, propertyName, description, "checkbox");
        }


        /// <summary>
        /// Adds an input field to modules' option popup. 
        /// </summary>
        /// <returns>
        /// ProgramHelper
        /// </returns>
        /// <param name='fordomains'>
        /// A string with comma separated list of the module's domains that will show this input field
        /// </param>
        /// <param name='forModuleTypes'>
        /// A string with comma separated list of the module's types that will show this input field
        /// </param>
        /// <param name='propertyName'>
        /// Name for this input field
        /// </param>
        /// <param name='description'>
        /// Description for this input field
        /// </param>
        public ProgramHelper AddFeatureTextInput(string forDomain, string forModuleTypes, string propertyName, string description)
        {
            return AddFeature(forDomain, forModuleTypes, propertyName, description, "text");
        }

        public ProgramHelper AddFeatureTextInput(string forModuleTypes, string propertyName, string description)
        {
            return AddFeature("", forModuleTypes, propertyName, description, "text");
        }

        public ProgramHelper AddVirtualModule(string domain, string address, string type, string widget)
        {
            VirtualModule oldModule = null;
            try { oldModule = homegenie.VirtualModules.Find(rm => rm.ParentId == myProgramId.ToString() && rm.Address == address); }
            catch { }
            //
            if (oldModule == null)
            {
                var module = new VirtualModule() { ParentId = myProgramId.ToString(), Domain = domain, Address = address, DeviceType = (Module.DeviceTypes)Enum.Parse(typeof(Module.DeviceTypes), type) };
                module.Properties.Add(new ModuleParameter() { Name = Properties.WIDGET_DISPLAYMODULE, Value = widget });
                homegenie.VirtualModules.Add(module);
            }
            else
            {
                oldModule.Domain = domain;
                oldModule.DeviceType = (Module.DeviceTypes)Enum.Parse(typeof(Module.DeviceTypes), type);
                Utility.ModuleParameterSet(oldModule, Properties.WIDGET_DISPLAYMODULE, widget);
            }
            //
            homegenie.modules_RefreshVirtualModules();
            homegenie.modules_Sort();
            return this;
        }

        public ProgramHelper RemoveVirtualModule(string domain, string address)
        {
            VirtualModule oldModule = null;
            try { oldModule = homegenie.VirtualModules.Find(rm => rm.ParentId == myProgramId.ToString() && rm.Address == address); }
            catch { }
            if (oldModule != null)
            {
                homegenie.VirtualModules.Remove(oldModule);
            }
            //
            homegenie.modules_RefreshVirtualModules();
            homegenie.modules_Sort();
            return this;
        }

        public ProgramHelper AddVirtualModules(string domain, string type, string widget, int startAddress, int endAddress)
        {
            for (int x = startAddress; x <= endAddress; x++)
            {

                VirtualModule oldmodule = null;
                try { oldmodule = homegenie.VirtualModules.Find(rm => rm.ParentId == myProgramId.ToString() && rm.Address == x.ToString()); }
                catch { }
                //
                if (oldmodule == null)
                {
                    var module = new VirtualModule() { ParentId = myProgramId.ToString(), Domain = domain, Address = x.ToString(), DeviceType = (Module.DeviceTypes)Enum.Parse(typeof(Module.DeviceTypes), type) };
                    module.Properties.Add(new ModuleParameter() { Name = Properties.WIDGET_DISPLAYMODULE, Value = widget });
                    homegenie.VirtualModules.Add(module);
                }
                else
                {
                    oldmodule.Domain = domain;
                    oldmodule.DeviceType = (Module.DeviceTypes)Enum.Parse(typeof(Module.DeviceTypes), type);
                    Utility.ModuleParameterSet(oldmodule, Properties.WIDGET_DISPLAYMODULE, widget);
                }

            }
            homegenie.modules_RefreshVirtualModules();
            homegenie.modules_Sort();
            return this;
        }

        public ProgramHelper AddControlWidget(string widget)
        {
            var program = homegenie.ProgramEngine.Programs.Find(p => p.Address == myProgramId);
            var module = homegenie.VirtualModules.Find(rm => rm.ParentId == myProgramId.ToString() && rm.Domain == myProgramDomain && rm.Address == myProgramId.ToString());
            //
            if (module == null)
            {
                module = new VirtualModule() { ParentId = myProgramId.ToString(), Visible = (widget != ""), Domain = myProgramDomain, Address = myProgramId.ToString(), Name = (program != null ? program.Name : ""), DeviceType = Module.DeviceTypes.Program };
                homegenie.VirtualModules.Add(module);
            }
            //
            module.Name = (program != null ? program.Name : "");
            module.Domain = myProgramDomain;
            module.Visible = (widget != "");
            Utility.ModuleParameterSet(module, Properties.WIDGET_DISPLAYMODULE, widget);
            //
            RelocateProgramModule();
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
                HomeGenieService.LogEvent(myProgramDomain, myProgramId.ToString(), ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
        }

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
            catch { }
            // create parameter if does not exists
            if (parameter == null)
            {
                parameter = Service.Utility.ModuleParameterSet(programModule, parameterName, "");
            }
            return parameter;
        }


        public void Say(string sentence, string locale, bool goAsync = false)
        {
            Utility.Say(sentence, locale, goAsync);
        }

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


        public ProgramHelper Notify(string title, string message)
        {
            homegenie.LogBroadcastEvent(Domains.HomeAutomation_HomeGenie_Automation, myProgramId.ToString(), "Automation Program", title, message);
            return this;
        }

        public ProgramHelper RaiseEvent(string parameter, string value, string description)
        {
            var actionEvent = new MIG.InterfacePropertyChangedAction();
            actionEvent.Domain = programModule.Domain;
            actionEvent.Path = parameter;
            actionEvent.Value = value;
            actionEvent.SourceId = programModule.Address;
            actionEvent.SourceType = "Automation Program";
            try
            {
                homegenie.SignalModulePropertyChange(this, programModule, actionEvent);
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(programModule.Domain, programModule.Address, ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
            return this;
        }

        public ProgramHelper RaiseEvent(ModuleHelper module, string parameter, string value, string description)
        {
            var actionEvent = new MIG.InterfacePropertyChangedAction();
            actionEvent.Domain = module.Instance.Domain;
            actionEvent.Path = parameter;
            actionEvent.Value = value;
            actionEvent.SourceId = module.Instance.Address;
            actionEvent.SourceType = "Virtual Module";
            try
            {
                homegenie.SignalModulePropertyChange(this, module.Instance, actionEvent);
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(programModule.Domain, programModule.Address, ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
            return this;
        }

        // TODO: find a better place for this
        public double EnergyUseCounter
        {
            get { return homegenie.Statistics != null ? homegenie.Statistics.GetTotalCounter(Properties.METER_WATTS, 3600) : 0; }
        }

        // that isn't of any use here.. .anyway... =)
        public ProgramHelper Reset()
        {
            this.parameter = "";
            this.value = "";
            //
            if (programModule == null) RelocateProgramModule();
            //
            // remove all features 
            //
            var program = homegenie.ProgramEngine.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            if (program != null)
            {
                program.Features.Clear();
            }
            //
            initialized = false;
            //
            AddControlWidget(""); // no control widget --> not visible
            //
            return this;
        }

    }
}
