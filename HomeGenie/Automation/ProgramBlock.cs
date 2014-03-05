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
        private HomeGenieService homegenie = null;
        private bool isProgramEnabled = false;

        // c# program fields
        private AppDomain programDomain = null;
        private Type assemblyType = null;
        private Object assembly = null;
        private MethodInfo methodRun = null;
        private MethodInfo methodReset = null;
        private MethodInfo methodEvaluateCondition = null;
        private static object instanceObject = new object();
        private System.Reflection.Assembly scriptAssembly;

        internal Func<HomeGenie.Automation.Scripting.ModuleHelper, HomeGenie.Data.ModuleParameter, bool> ModuleChangedHandler = null;
        internal Func<HomeGenie.Automation.Scripting.ModuleHelper, HomeGenie.Data.ModuleParameter, bool> ModuleIsChangingHandler = null;
        internal List<string> registeredApiCalls = new List<string>();
        internal Thread ProgramThread;

        // wizard script public members
        //
        // Type = "Wizard" program data
        // subj: Domain, TargetNode, Property (eg. Domains.HomeAutomation_ZWave, "4", Globals.MODPAR_METER_WATTS ) , Date, Time
        // cond: Equals, GreaterThan, LessThan 
        //  val: <value>
        public ConditionType ConditionType;
        public List<ProgramCondition> Conditions;
        public List<ProgramCommand> Commands;

        // c# program public members
        public string ScriptCondition;
        public string ScriptSource;
        public string ScriptErrors;

        // common public members 
        public bool IsRunning;
        public bool IsEvaluatingConditionBlock;
        public List<ProgramFeature> Features = new List<ProgramFeature>();

        [NonSerialized]
        public bool LastConditionEvaluationResult;

        public string Domain = Domains.HomeAutomation_HomeGenie_Automation;
        public int Address = 0;

        public string Name;
        public string Description;
        public string Group;
        public string Type;

        public DateTime? ActivationTime;
        public DateTime? TriggerTime;

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
            isProgramEnabled = true;
            IsRunning = false;
            IsEvaluatingConditionBlock = false;
        }

        public bool IsEnabled
        {
            get { return isProgramEnabled; }
            set
            {
                if (value)
                {
                    ActivationTime = DateTime.UtcNow;
                }
                isProgramEnabled = value;
            }
        }

        internal System.Reflection.Assembly ScriptAssembly
        {
            get
            {
                return scriptAssembly;
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
                if (programDomain != null)
                {
                    // Unloading program app domain...
                    try
                    {
                        AppDomain.Unload(programDomain);
                    }
                    catch { }
                    programDomain = null;
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
                scriptAssembly = value;

            }
        }

        
        
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
            homegenie = homegenieref;
            // TODO: deprecate all other "homegenieref" parameters in other funcs
            bool succeed = false;
            lock (instanceObject)
                if (this.Type.ToLower() == "csharp")
                {
                    try
                    {
                        scriptAssembly = Assembly.Load(File.ReadAllBytes(this.AssemblyFile));
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
            if (scriptAssembly == null) return null;
            //
            MethodRunResult result = null;
            //
            if (CheckInstance(homegenieref))
            {
                result = (MethodRunResult)methodRun.Invoke(assembly, new object[1] { options });
            }
            //
            return result;
        }
        internal MethodRunResult EvaluateConditionStatement(HomeGenieService homegenieref)
        {
            if (scriptAssembly == null) return null;
            //
            MethodRunResult result = null;
            //
            if (CheckInstance(homegenieref))
            {
                result = (MethodRunResult)methodEvaluateCondition.Invoke(assembly, null);
            }
            //
            return result;
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
            foreach (string apiCall in registeredApiCalls)
            {
                homegenie.UnRegisterDynamicApi(apiCall);
            }
            registeredApiCalls.Clear();
        }
        internal void Reset()
        {
            if (scriptAssembly != null && methodReset != null)
            {
                methodReset.Invoke(assembly, null);
            }
        }

        private bool CheckInstance(HomeGenieService homegenieref)
        {
            lock (instanceObject)
            {
                if (programDomain == null)
                {
                    bool success = false;

                    // Creating script app domain
                    programDomain = AppDomain.CurrentDomain; //AppDomain.CreateDomain("HomeGenieScriptDomain-" + this.Address);

                    assemblyType = scriptAssembly.GetType("HomeGenie.Automation.Scripting.ScriptingInstance");
                    assembly = Activator.CreateInstance(assemblyType);

                    MethodInfo miSetHost = assemblyType.GetMethod("SetHost");
                    //
                    try
                    {
                        miSetHost.Invoke(assembly, new object[2] { homegenieref, this.Address });
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie_Automation, this.Address.ToString(), ex.Message, "Exception.StackTrace", ex.StackTrace);
                    }
                    //
                    methodRun = assemblyType.GetMethod("Run");
                    methodEvaluateCondition = assemblyType.GetMethod("EvaluateCondition");
                    methodReset = assemblyType.GetMethod("Reset");
                    //
                    return success;
                }
            }
            return true;
        }

    }
}

