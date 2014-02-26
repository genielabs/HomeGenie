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
using System.Threading;
using System.Collections.Generic;

using System.IO;
using System.Reflection;
using Microsoft.CSharp;
using System.CodeDom.Compiler;

using HomeGenie.Service;
using HomeGenie.Automation.Scripting;
using HomeGenie.Service.Constants;

namespace HomeGenie.Automation
{
    [Serializable()]
    public class ProgramBlock
    {
        private bool _isenabled = false;

        public ProgramBlock()
        {
            // init stuff
            Type = "";
            ScriptCondition = "";
            ScriptSource = "";
            ScriptErrors = "";
            ScriptAssembly = null;
            Commands = new List<ProgramCommand>();
            Conditions = new List<ProgramCondition>();
            ConditionType = ConditionType.None;
            _isenabled = true;
            IsRunning = false;
            IsEvaluatingConditionBlock = false;
        }

        public string Domain = Domains.HomeAutomation_HomeGenie_Automation;
        public int Address = 0;

        public string Name;
        public string Description;
        public string Group;
        public string Type;
        public bool IsEnabled
        {
            get { return _isenabled; }
            set
            {
                if (value)
                {
                    ActivationTime = DateTime.UtcNow;
                }
                _isenabled = value;
            }
        }

        public DateTime? ActivationTime;
        public DateTime? TriggerTime;

        // these two are used when Type == "CSharp"
        public string ScriptCondition;
        public string ScriptSource;
        public string ScriptErrors;
        //
        private System.Reflection.Assembly _scriptassembly;
        internal System.Reflection.Assembly ScriptAssembly
        {
            get
            {
                return _scriptassembly;
            }
            set
            {
                ActivationTime = null;
                TriggerTime = null;
                try
                {
                    Stop();
                }
                catch (Exception)
                {
                    // TODO: handle this...
                }
                //
                if (_program_appdomain != null)
                {
                    // Unloading program app domain...
                    try
                    {
                        AppDomain.Unload(_program_appdomain);
                    }
                    catch { }
                    _program_appdomain = null;
                    //
                    try
                    {
                        // Deleting assembly...
                        File.Delete(this.AssemblyFile);
                    }
                    catch { }
                }
                //
                IsRunning = false;
                //
                _scriptassembly = value;

            }
        }

        // Type = "Wizard" program data
        // subj: Domain, TargetNode, Property (eg. Domains.HomeAutomation_ZWave, "4", Globals.MODPAR_METER_WATTS ) , Date, Time
        // cond: Equals, GreaterThan, LessThan 
        //  val: <value>
        public ConditionType ConditionType;
        public List<ProgramCondition> Conditions;
        public List<ProgramCommand> Commands;

        // common members 
        public bool IsRunning;
        public bool IsEvaluatingConditionBlock;
        public List<ProgramFeature> Features = new List<ProgramFeature>();

        [NonSerialized]
        public bool LastConditionEvaluationResult;
        //        [NonSerialized]
        internal Func<HomeGenie.Automation.Scripting.ModuleHelper, HomeGenie.Data.ModuleParameter, bool> ModuleChangedHandler = null;
        //        [NonSerialized]
        internal Func<HomeGenie.Automation.Scripting.ModuleHelper, HomeGenie.Data.ModuleParameter, bool> ModuleIsChangingHandler = null;
        //        [NonSerialized]
        internal List<string> _registeredapicalls = new List<string>();


        /////////////////////////////////////////////////////////////////////////////////


        internal Thread ProgramThread;
        private AppDomain _program_appdomain = null;
        private Type _program_assembly_type = null;
        private Object _program_assembly = null;
        private MethodInfo _program_method_run = null;
        private MethodInfo _program_method_reset = null;
        private MethodInfo _program_method_evaluatecondition = null;
        private static object _instobj = new object();
        private HomeGenieService _homegenie = null;
        //
        internal string AssemblyFile
        {
            get
            {
                string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs");
                file = Path.Combine(file, this.Address + ".dll");
                return file;
            }
        }
        internal bool AssemblyLoad(HomeGenieService homegenieref)
        {
            _homegenie = homegenieref;
            // TODO: deprecate all other "homegenieref" parameters in other funcs
            bool succeed = false;
            lock (_instobj)
                if (this.Type.ToLower() == "csharp")
                {
                    try
                    {
                        _scriptassembly = Assembly.Load(File.ReadAllBytes(this.AssemblyFile));
                        succeed = true;
                    }
                    catch (Exception e)
                    {

                        this.ScriptErrors = e.Message + "\n" + e.StackTrace;
                    }
                }
            return succeed;
        }
        internal MethodRunResult RunScript(HomeGenieService homegenieref, string options)
        {
            if (_scriptassembly == null) return null;
            //
            MethodRunResult res = null;
            //
            if (_checkinstance(homegenieref))
            {
                res = (MethodRunResult)_program_method_run.Invoke(_program_assembly, new object[1] { options });
            }
            //
            return res;
        }
        internal MethodRunResult EvaluateConditionStatement(HomeGenieService homegenieref)
        {
            if (_scriptassembly == null) return null;
            //
            MethodRunResult res = null;
            //
            if (_checkinstance(homegenieref))
            {
                res = (MethodRunResult)_program_method_evaluatecondition.Invoke(_program_assembly, null);
            }
            //
            return res;
        }
        internal void Stop()
        {
            //this.Reset();
            //
            this.IsRunning = false;
            this.IsEvaluatingConditionBlock = false;
            //
            if (ProgramThread != null)
            {
                try
                {
                    ProgramThread.Abort();
                }
                catch { }
                //
                ProgramThread = null;
            }
            //
            //TODO: complete cleanup and deallocation stuff here
            ModuleIsChangingHandler = null;
            ModuleChangedHandler = null;
            //
            foreach (string apicall in _registeredapicalls)
            {
                _homegenie.UnRegisterDynamicApi(apicall);
            }
            _registeredapicalls.Clear();
        }
        internal void Reset()
        {
            if (_scriptassembly != null && _program_method_reset != null)
            {
                _program_method_reset.Invoke(_program_assembly, null);
            }
        }

        private bool _checkinstance(HomeGenieService homegenieref)
        {
            lock (_instobj)
            {
                if (_program_appdomain == null)
                {
                    DateTime starttime = DateTime.Now;

                    bool success = false;

                    // Creating script app domain
                    _program_appdomain = AppDomain.CurrentDomain; //AppDomain.CreateDomain("HomeGenieScriptDomain-" + this.Address);


                    TimeSpan ts1 = new TimeSpan(DateTime.Now.Ticks - starttime.Ticks);


                    starttime = DateTime.Now;
                    _program_assembly_type = _scriptassembly.GetType("HomeGenie.Automation.Scripting.ScriptingInstance");
                    _program_assembly = Activator.CreateInstance(_program_assembly_type);


                    TimeSpan ts2 = new TimeSpan(DateTime.Now.Ticks - starttime.Ticks);


                    MethodInfo miSetHost = _program_assembly_type.GetMethod("SetHost");
                    //
                    try
                    {
                        miSetHost.Invoke(_program_assembly, new object[2] { homegenieref, this.Address });
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie_Automation, this.Address.ToString(), ex.Message, "Exception.StackTrace", ex.StackTrace);
                    }
                    //
                    _program_method_run = _program_assembly_type.GetMethod("Run");
                    _program_method_evaluatecondition = _program_assembly_type.GetMethod("EvaluateCondition");
                    _program_method_reset = _program_assembly_type.GetMethod("Reset");
                    //
                    return success;
                }
            }
            return true;
        }

    }
}

