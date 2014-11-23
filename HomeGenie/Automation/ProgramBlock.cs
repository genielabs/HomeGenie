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
using System.Xml.Serialization;

using HomeGenie.Service;
using HomeGenie.Automation.Scripting;
using HomeGenie.Service.Constants;

using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Jint;
using IronRuby;

namespace HomeGenie.Automation
{

    [Serializable()]
    public class ProgramBlock
    {
        private HomeGenieService homegenie = null;
        private bool isProgramEnabled = false;
        private string codeType = "";

        // event delegates
        public delegate void EnabledStateChangedEventHandler(object sender, bool isEnabled);

        public event EnabledStateChangedEventHandler EnabledStateChanged;

        // c# program fields
        private AppDomain programDomain = null;
        private Type assemblyType = null;
        private Object assembly = null;
        private MethodInfo methodRun = null;
        private MethodInfo methodReset = null;
        private MethodInfo methodEvaluateCondition = null;
        //private static object instanceObject = new object();
        private System.Reflection.Assembly appAssembly;

        // IronScript fields for Python, Ruby, Javascript
        internal object scriptEngine = null;
        private ScriptScope scriptScope = null;
        private ScriptingHost hgScriptingHost = null;

        // System events handlers
        internal Func<bool> SystemStarted = null;
        internal Func<bool> SystemStopping = null;
        internal Func<HomeGenie.Automation.Scripting.ModuleHelper, HomeGenie.Data.ModuleParameter, bool> ModuleChangedHandler = null;
        internal Func<HomeGenie.Automation.Scripting.ModuleHelper, HomeGenie.Data.ModuleParameter, bool> ModuleIsChangingHandler = null;
        internal List<string> registeredApiCalls = new List<string>();

        // Main program thread
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
        public List<ProgramFeature> Features = new List<ProgramFeature>();

        [NonSerialized]
        public bool LastConditionEvaluationResult;

        public string Domain = Domains.HomeAutomation_HomeGenie_Automation;
        public int Address = 0;
        public string Name;
        public string Description;
        public string Group;

        public string Type
        {
            get { return codeType; }
            set
            {
                codeType = value;
                scriptEngine = null;
                scriptScope = null;
                switch (codeType.ToLower())
                {
                case "python":
                    try { scriptEngine = Python.CreateEngine(); } catch { }
                    break;
                case "ruby":
                    try { scriptEngine = Ruby.CreateEngine(); } catch { }
                    break;
                case "javascript":
                    try { scriptEngine = new Jint.Engine(); } catch { }
                    break;
                }
                if (homegenie != null && scriptEngine != null)
                {
                    SetupScriptingScope();
                }
            }
        }

        public DateTime? ActivationTime;
        public DateTime? TriggerTime;

        public ProgramBlock()
        {
            // init stuff
            Type = "";
            ScriptCondition = "";
            ScriptSource = "";
            ScriptErrors = "";
            //
            AppAssembly = null;
            //
            Commands = new List<ProgramCommand>();
            Conditions = new List<ProgramCondition>();
            ConditionType = ConditionType.None;
            //
            isProgramEnabled = true;
            IsRunning = false;
        }

        public bool IsEnabled
        {
            get { return isProgramEnabled; }
            set
            {
                if (isProgramEnabled != value)
                {
                    isProgramEnabled = value;
                    if (isProgramEnabled) ActivationTime = DateTime.UtcNow;
                    if (EnabledStateChanged != null) EnabledStateChanged(this, value);
                }
            }
        }

        public void SetHost(HomeGenieService hg)
        {
            homegenie = hg;
            // force ScriptingHost assignment
            this.Type = codeType;
        }


        #region IronPython, IronRuby and Jint Javascript Scripts methods

        private void SetupScriptingScope()
        {
            hgScriptingHost = new ScriptingHost();
            hgScriptingHost.SetHost(homegenie, this.Address);
            if (scriptEngine.GetType() == typeof(ScriptEngine))
            {
                // IronPyton and IronRuby engines
                ScriptEngine currentEngine = (scriptEngine as ScriptEngine);
                dynamic scope = scriptScope = currentEngine.CreateScope();
                scope.hg = hgScriptingHost;
            }
            else if (scriptEngine.GetType() == typeof(Jint.Engine))
            {
                // Jint Javascript engine
                Jint.Engine javascriptEngine = (scriptEngine as Jint.Engine);
                javascriptEngine.SetValue("hg", hgScriptingHost);
            }
        }

        #endregion


        #region CSharp App methods

        internal System.Reflection.Assembly AppAssembly
        {
            get
            {
                return appAssembly;
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
                    catch
                    {
                    }
                    programDomain = null;
                    //
                    try
                    {
                        // Deleting assembly...
                        File.Delete(this.AssemblyFile);
                    }
                    catch
                    {
                    }
                }
                //
                IsRunning = false;
                //
                appAssembly = value;

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

        internal bool AssemblyLoad()
        {
            bool succeed = false;
            if (this.Type.ToLower() == "csharp")
            {
                try
                {
                    appAssembly = Assembly.Load(File.ReadAllBytes(this.AssemblyFile));
                    succeed = true;
                }
                catch (Exception e)
                {

                    this.ScriptErrors = e.Message + "\n" + e.StackTrace;
                }
            }
            return succeed;
        }

        internal void Reset()
        {
            // CSharp App
            if (appAssembly != null && methodReset != null)
            {
                methodReset.Invoke(assembly, null);
            }
            // Python, Ruby, Javascript
            else if (hgScriptingHost != null)
            {
                hgScriptingHost.Reset();
                hgScriptingHost = null;
            }
        }

        private bool CheckAppInstance()
        {
            bool success = false;
            if (programDomain != null)
            {
                success = true;
            }
            else
            {
                try
                {
                    // Creating app domain
                    programDomain = AppDomain.CurrentDomain;
                    //
                    assemblyType = appAssembly.GetType("HomeGenie.Automation.Scripting.ScriptingInstance");
                    assembly = Activator.CreateInstance(assemblyType);
                    //
                    MethodInfo miSetHost = assemblyType.GetMethod("SetHost");
                    miSetHost.Invoke(assembly, new object[2] { homegenie, this.Address });
                    //
                    methodRun = assemblyType.GetMethod("Run");
                    methodEvaluateCondition = assemblyType.GetMethod("EvaluateCondition");
                    methodReset = assemblyType.GetMethod("Reset");
                    //
                    success = true;
                }
                catch (Exception ex)
                {
                    HomeGenieService.LogEvent(
                        Domains.HomeAutomation_HomeGenie_Automation,
                        this.Address.ToString(),
                        ex.Message,
                        "Exception.StackTrace",
                        ex.StackTrace
                    );
                }
            }
            return success;
        }

        #endregion


        #region Common methods

        internal MethodRunResult Run(string options)
        {
            MethodRunResult result = null;
            switch (codeType.ToLower())
            {
            case "python":
                string pythonScript = this.ScriptSource;
                ScriptEngine pythonEngine = (scriptEngine as ScriptEngine);
                result = new MethodRunResult();
                try
                {
                    pythonEngine.Execute(pythonScript, scriptScope);
                }
                catch (Exception e)
                {
                    result.Exception = e;
                }
                break;
            case "ruby":
                string rubyScript = this.ScriptSource;
                ScriptEngine rubyEngine = (scriptEngine as ScriptEngine);
                result = new MethodRunResult();
                try
                {
                    rubyEngine.Execute(rubyScript, scriptScope);
                }
                catch (Exception e)
                {
                    result.Exception = e;
                }
                break;
            case "javascript":
                string jsScript = this.ScriptSource;
                Jint.Engine engine = (scriptEngine as Jint.Engine);
                    //engine.Options.AllowClr(false);
                result = new MethodRunResult();
                try
                {
                    engine.Execute(jsScript);
                }
                catch (Exception e)
                {
                    result.Exception = e;
                }
                break;
            case "csharp":
                if (appAssembly != null && CheckAppInstance())
                {
                    result = (MethodRunResult)methodRun.Invoke(assembly, new object[1] { options });
                }
                break;
            case "arduino":
                // TODO: upload compiled sketch to the board (make upload)
                result = new MethodRunResult();
                try
                {
                    ArduinoAppFactory.UploadSketch(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs", "arduino", this.Address.ToString()));
                }
                catch (Exception e)
                {
                    result.Exception = e;
                }
                break;
            }
            //
            return result;
        }

        internal MethodRunResult EvaluateCondition()
        {
            MethodRunResult result = null;
            switch (codeType.ToLower())
            {
            case "python":
                string pythonScript = this.ScriptCondition;
                ScriptEngine pythonEngine = (scriptEngine as ScriptEngine);
                result = new MethodRunResult();
                try
                {
                    pythonEngine.Execute(pythonScript, scriptScope);
                    result.ReturnValue = (scriptScope as dynamic).hg.executeCodeToRun;
                }
                catch (Exception e)
                {
                    result.Exception = e;
                }
                break;
            case "ruby":
                string rubyScript = this.ScriptCondition;
                ScriptEngine rubyEngine = (scriptEngine as ScriptEngine);
                result = new MethodRunResult();
                try
                {
                    rubyEngine.Execute(rubyScript, scriptScope);
                    result.ReturnValue = (scriptScope as dynamic).hg.executeCodeToRun;
                }
                catch (Exception e)
                {
                    result.Exception = e;
                }
                break;
            case "javascript":
                string jsScript = this.ScriptCondition;
                Jint.Engine engine = (scriptEngine as Jint.Engine);
                result = new MethodRunResult();
                try
                {
                    engine.Execute(jsScript);
                    result.ReturnValue = (engine.GetValue("hg").ToObject() as ScriptingHost).executeCodeToRun;
                }
                catch (Exception e)
                {
                    result.Exception = e;
                }
                break;
            case "csharp":
                if (appAssembly != null && CheckAppInstance())
                {
                    result = (MethodRunResult)methodEvaluateCondition.Invoke(assembly, null);
                }
                break;
            }
            //
            return result;
        }

        internal void Stop()
        {
            this.IsRunning = false;
            //this.Reset();
            //
            if (ProgramThread != null)
            {
                try
                {
                    ProgramThread.Abort();
                    ProgramThread.Join(100);
                }
                catch
                {
                }
                //
                ProgramThread = null;
            }
            //
            //TODO: complete cleanup and deallocation stuff here
            //
            ModuleIsChangingHandler = null;
            ModuleChangedHandler = null;
            SystemStarted = null;
            SystemStopping = null;
            //
            foreach (string apiCall in registeredApiCalls)
            {
                homegenie.UnRegisterDynamicApi(apiCall);
            }
            registeredApiCalls.Clear();
            //
            switch (codeType.ToLower())
            {
            case "python":
            case "ruby":
                (scriptEngine as ScriptEngine).Runtime.Shutdown();
                break;
            //case "javascript":
            //case "csharp":
            }
        }

        #endregion


    }


}

