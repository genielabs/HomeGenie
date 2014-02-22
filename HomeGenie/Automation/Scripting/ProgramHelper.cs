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
        private HomeGenieService _homegenie;
        private Module _programmodule;
        private int _myprogramid = -1;
        private string _myprogramdomain = Domains.HomeAutomation_HomeGenie_Automation;

        private string parameter = "";
        private string value = "";

        private bool initialized = false; // if setup has been done

        public ProgramHelper(HomeGenieService hg, int programid)
        {
            _homegenie = hg;
            _myprogramid = programid;
            //
            VirtualModule oldmodule = _homegenie.VirtualModules.Find(rm => rm.ParentId == _myprogramid.ToString() && rm.Address == _myprogramid.ToString());
            if (oldmodule == null)
            {
                AddControlWidget(""); // no control widget --> not visible
            }
        }
		
		/// <summary>
		/// Run an Automation Program
		/// </summary>
		/// <param name='programid'>
		/// Name or ID of program to run
		/// </param>
        public void Run(string programid)
        { 
            Run(programid, "");
        }
		
        public void Run(string programid, string optionstring)
        {
            //TODO: improve locking for single instance run only
            ProgramBlock pb = _homegenie.ProgramEngine.Programs.Find(p => p.Address.ToString() == programid || p.Name == programid);
            pb.IsRunning = true;
            if (pb.Type.ToLower() == "csharp")
            {
                pb.RunScript(_homegenie, optionstring);
            }
            else
            {
                _homegenie.ProgramEngine.ExecuteWizardScript(pb);
            }
            pb.IsRunning = false;
        }

		/// <summary>
		/// Run a given function in the background
		/// </summary>
		/// <returns>
		/// The Thread for this background task
		/// </returns>
		/// <param name='fnblock'>
		/// Function name or inline delegate
		/// </param>
        public Thread RunAsyncTask(Utility.AsyncFunction fnblock)
        {
            return Utility.RunAsyncTask(fnblock);
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
                ProgramBlock pb = _homegenie.ProgramEngine.Programs.Find(p => p.Address == _myprogramid);
                return pb.IsRunning;
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
                ProgramBlock pb = _homegenie.ProgramEngine.Programs.Find(p => p.Address == _myprogramid);
                return pb.IsEnabled;
            }
        }
		
        public void GoBackground()
        {
            while (this.IsEnabled)
            {
                System.Threading.Thread.Sleep(5000);
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
		/// <param name='defaultvalue'>
		/// Default value for this input field
		/// </param>
		/// <param name='description'>
		/// Description for this input field
		/// </param>
        public ProgramHelper AddInputField(string field, string defaultvalue, string description)
        {
            var par = this.Parameter("ConfigureOptions." + field);
            if (par.Value == "") par.Value = defaultvalue;
            par.Description = description;
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
                _relocateprogrammodule();
                return _programmodule;
            }
        }

        public ProgramHelper AddFeature(string fordomains, string formoduletypes, string propname, string description, string type) // default type = checkbox
        {
            ProgramBlock pb = _homegenie.ProgramEngine.Programs.Find(p => p.Address.ToString() == _myprogramid.ToString());
            ProgramFeature pf = null;
            //
            try { pf = pb.Features.Find(f => f.Property == propname); }
            catch { }
            //
            if (pf == null)
            {
                pf = new ProgramFeature();
                pb.Features.Add(pf);
            }
            pf.FieldType = type;
            pf.Property = propname;
            pf.Description = description;
            pf.ForDomains = fordomains;
            pf.ForTypes = formoduletypes;
            return this;
        }

        public ProgramFeature Feature(string propname)
        {
            ProgramBlock pb = _homegenie.ProgramEngine.Programs.Find(p => p.Address.ToString() == _myprogramid.ToString());
            ProgramFeature pf = null;
            //
            try { pf = pb.Features.Find(f => f.Property == propname); } catch { }
            //
            return pf;
        }
		
		/// <summary>
		/// Adds a checkbox option to modules' option popup. 
		/// </summary>
		/// <returns>
		/// ProgramHelper
		/// </returns>
		/// <param name='fordomains'>
		/// A string with comma separated list of the module's domains that will show this checkbox
		/// </param>
		/// <param name='formoduletypes'>
		/// A string with comma separated list of the module's types that will show this checkbox
		/// </param>
		/// <param name='propname'>
		/// Name for this checkbox option
		/// </param>
		/// <param name='description'>
		/// Description for this checkbox option
		/// </param>
        public ProgramHelper AddFeature(string fordomains, string formoduletypes, string propname, string description)
        {
            return AddFeature(fordomains, formoduletypes, propname, description, "checkbox");
        }

        public ProgramHelper AddFeature(string formoduletypes, string propname, string description) // default type = checkbox
        {
            return AddFeature("", formoduletypes, propname, description, "checkbox");
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
		/// <param name='formoduletypes'>
		/// A string with comma separated list of the module's types that will show this input field
		/// </param>
		/// <param name='propname'>
		/// Name for this input field
		/// </param>
		/// <param name='description'>
		/// Description for this input field
		/// </param>
        public ProgramHelper AddFeatureTextInput(string fordomain, string formoduletypes, string propname, string description)
        {
            return AddFeature(fordomain, formoduletypes, propname, description, "text");
        }

        public ProgramHelper AddFeatureTextInput(string formoduletypes, string propname, string description) 
        {
            return AddFeature("", formoduletypes, propname, description, "text");
        }

        public ProgramHelper AddVirtualModule(string domain, string address, string type, string widget)
        {
            VirtualModule oldmodule = null;
            try { oldmodule = _homegenie.VirtualModules.Find(rm => rm.ParentId == _myprogramid.ToString() && rm.Address == address); }
            catch { }
            //
            if (oldmodule == null)
            {
                VirtualModule m = new VirtualModule() { ParentId = _myprogramid.ToString(), Domain = domain, Address = address, DeviceType = (Module.DeviceTypes)Enum.Parse(typeof(Module.DeviceTypes), type) };
                m.Properties.Add(new ModuleParameter() { Name = Properties.WIDGET_DISPLAYMODULE, Value = widget });
                _homegenie.VirtualModules.Add(m);
            }
            else
            {
                oldmodule.Domain = domain;
                oldmodule.DeviceType = (Module.DeviceTypes)Enum.Parse(typeof(Module.DeviceTypes), type);
                Utility.ModuleParameterSet(oldmodule, Properties.WIDGET_DISPLAYMODULE, widget);
            }
            //
            _homegenie._modules_refresh_virtualmods();
            _homegenie._modules_sort();
            return this;
        }

        public ProgramHelper RemoveVirtualModule(string domain, string address)
        {
            VirtualModule oldmodule = null;
            try { oldmodule = _homegenie.VirtualModules.Find(rm => rm.ParentId == _myprogramid.ToString() && rm.Address == address); }
            catch { }
            if (oldmodule != null)
            {
                _homegenie.VirtualModules.Remove(oldmodule);
            }
            //
            _homegenie._modules_refresh_virtualmods();
            _homegenie._modules_sort();
            return this;
        }

        public ProgramHelper AddVirtualModules(string domain, string type, string widget, int startaddress, int endaddress)
        {
            for (int x = startaddress; x <= endaddress; x++)
            {

                VirtualModule oldmodule = null;
                try { oldmodule = _homegenie.VirtualModules.Find(rm => rm.ParentId == _myprogramid.ToString() && rm.Address == x.ToString()); } catch { }
                //
                if (oldmodule == null)
                {
                    VirtualModule m = new VirtualModule() { ParentId = _myprogramid.ToString(), Domain = domain, Address = x.ToString(), DeviceType = (Module.DeviceTypes)Enum.Parse(typeof(Module.DeviceTypes), type) };
                    m.Properties.Add(new ModuleParameter() { Name = Properties.WIDGET_DISPLAYMODULE, Value = widget });
                    _homegenie.VirtualModules.Add(m);
                }
                else
                {
                    oldmodule.Domain = domain;
                    oldmodule.DeviceType = (Module.DeviceTypes)Enum.Parse(typeof(Module.DeviceTypes), type);
                    Utility.ModuleParameterSet(oldmodule, Properties.WIDGET_DISPLAYMODULE, widget);
                }

            }
            _homegenie._modules_refresh_virtualmods();
            _homegenie._modules_sort();
            return this;
        }

        public ProgramHelper AddControlWidget(string widget)
        {
            ProgramBlock pb = _homegenie.ProgramEngine.Programs.Find(p => p.Address == _myprogramid);
            VirtualModule m = _homegenie.VirtualModules.Find(rm => rm.ParentId == _myprogramid.ToString() && rm.Domain == _myprogramdomain && rm.Address == _myprogramid.ToString());
            //
            if (m == null)
            {
                m = new VirtualModule() { ParentId = _myprogramid.ToString(), Visible = (widget != ""), Domain = _myprogramdomain, Address = _myprogramid.ToString(), Name = pb.Name, DeviceType = Module.DeviceTypes.Program };
                _homegenie.VirtualModules.Add(m);
            }
            //
            m.Name = pb.Name;
            m.Domain = _myprogramdomain;
            m.Visible = (widget != "");
            Utility.ModuleParameterSet(m, Properties.WIDGET_DISPLAYMODULE, widget);
            //
            _relocateprogrammodule();
            //
            return this;
        }

        private void _relocateprogrammodule()
        {
            // force automation modules regeneration
            _homegenie._modules_refresh_programs();
            //
            try
            {
                _programmodule = _homegenie.Modules.Find(rm => rm.Domain == _myprogramdomain && rm.Address == _myprogramid.ToString());
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(_myprogramdomain, _myprogramid.ToString(), ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
        }

        public void Setup(Action fn)
        {
            try
            {
                if (!this.initialized)
                {
                    //
                    if (_programmodule == null) _relocateprogrammodule();
                    //
                    // mark config options to determine unused ones
                    if (_programmodule != null)
                    {
                        foreach (ModuleParameter p in _programmodule.Properties)
                        {
                            if (p.Name.StartsWith("ConfigureOptions."))
                            {
                                p.Description = null;
                            }
                        }
                    }
                    //
                    fn();
                    //
                    // remove deprecated config options
                    if (_programmodule != null)
                    {
                        List<ModuleParameter> plist = _programmodule.Properties.FindAll(mp => mp.Name.StartsWith("ConfigureOptions."));
                        foreach (ModuleParameter p in plist)
                        {
                            if (p.Name.StartsWith("ConfigureOptions.") && p.Description == null)
                            {
                                _programmodule.Properties.Remove(p);
                            }
                        }
                    }
                    this.initialized = true;
                    //
                    _homegenie._modules_refresh_programs();
                    _homegenie._modules_refresh_virtualmods();
                }
            }
            catch (Exception e)
            { 
                //TODO: report error
                throw ( new Exception(e.StackTrace) );
            }
        }

        public ProgramHelper WithName(string programname)
        {
            ProgramBlock pb = _homegenie.ProgramEngine.Programs.Find(p => p.Name.ToLower() == programname.ToLower());
            ProgramHelper ph = null;
            if (pb != null)
            {
                ph = new ProgramHelper(_homegenie, pb.Address);
            }
            return ph;
        }

        public ModuleParameter Parameter(string parameter)
        {
            //
            if (_programmodule == null) _relocateprogrammodule();
            //
            ModuleParameter value = null;
            try
            {
                value = Service.Utility.ModuleParameterGet(_programmodule, parameter);
            }
            catch { }
            // create parameter if does not exists
            if (value == null)
            {
                value = Service.Utility.ModuleParameterSet(_programmodule, parameter, "");
            }
            return value;
        }


        public void Say(string sentence, string locale, bool goasync = false)
        {
            Utility.Say(sentence, locale, goasync);
        }

        public void Play(string waveurl)
        {
            WebClient wc = new WebClient();
            byte[] audiodata = wc.DownloadData(waveurl);
            wc.Dispose();

            string outdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_tmp");
            if (!Directory.Exists(outdir)) Directory.CreateDirectory(outdir);
            string file = Path.Combine(outdir, "_wave_tmp." + Path.GetExtension(waveurl));
            if (File.Exists(file)) File.Delete(file);

            FileStream fs = File.OpenWrite(file);
            fs.Write(audiodata, 0, audiodata.Length);
            fs.Close();

            Utility.Play(file);

        }


        public ProgramHelper Notify(string title, string message)
        {
            _homegenie.LogBroadcastEvent("HomeGenie.Automation", _myprogramid.ToString(), "Automation Program", title, message);
            return this;
        }

        public ProgramHelper RaiseEvent(string parameter, string value, string description)
        {
            MIG.InterfacePropertyChangedAction mact = new MIG.InterfacePropertyChangedAction();
            mact.Domain = _programmodule.Domain;
            mact.Path = parameter;
            mact.Value = value;
            mact.SourceId = _programmodule.Address;
            mact.SourceType = "Automation Program";
            try
            {
                _homegenie.SignalModulePropertyChange(this, _programmodule, mact);
            }
            catch (Exception ex) 
            {
                HomeGenieService.LogEvent(_programmodule.Domain, _programmodule.Address, ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
            return this;
        }

        public ProgramHelper RaiseEvent(ModuleHelper module, string parameter, string value, string description)
        {
            MIG.InterfacePropertyChangedAction mact = new MIG.InterfacePropertyChangedAction();
            mact.Domain = module.Instance.Domain;
            mact.Path = parameter;
            mact.Value = value;
            mact.SourceId = module.Instance.Address;
            mact.SourceType = "Virtual Module";
            try
            {
                _homegenie.SignalModulePropertyChange(this, module.Instance, mact);
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(_programmodule.Domain, _programmodule.Address, ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
            return this;
        }

        // TODO: find a better place for this
        public double EnergyUseCounter
        {
            get { return _homegenie.Statistics != null ? _homegenie.Statistics.GetTotalCounter(Properties.METER_WATTS, 3600) : 0; }
        }

/*

        public ProgramHelper Set(string value)
        {
            this.value = value;
            //if (_programmodule != null)
            {
                ModuleParameter parameter = Utility.ModuleParameterGet(_programmodule, this.parameter);
                if (parameter == null)
                {
                    _programmodule.Properties.Add(new ModuleParameter() { Name = this.parameter, Value = value });
                }
                else
                {
                    parameter.Value = value;
                }
            }
            return this;
        }

        public ProgramHelper Set(string value, string description)
        {
            this.value = value;
            //if (_programmodule != null)
            {
                ModuleParameter parameter = Utility.ModuleParameterGet(_programmodule, this.parameter);
                if (parameter == null)
                {
                    _programmodule.Properties.Add(new ModuleParameter() { Name = this.parameter, Value = value, Description = description });
                }
                else
                {
                    if (initialized || parameter.Value == "")
                    {
                        parameter.Value = value;
                    }
                    parameter.Description = description;
                }
            }
            return this;
        }
*/

        // that isn't of any use here.. .anyway... =)
        public ProgramHelper Reset()
        {
            this.parameter = "";
            this.value = "";
//            this.initialized = false;
            //
            if (_programmodule == null) _relocateprogrammodule();
            //
            // remove all features 
            //
            ProgramBlock pb = _homegenie.ProgramEngine.Programs.Find(p => p.Address.ToString() == _myprogramid.ToString());
            pb.Features.Clear();
            //
            initialized = false;
            //
            AddControlWidget(""); // no control widget --> not visible
            //
            return this;
        }

    }
}
