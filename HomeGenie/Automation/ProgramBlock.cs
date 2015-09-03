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
using System.Diagnostics;

namespace HomeGenie.Automation
{

    [Serializable()]
    public class ProgramBlock
    {
        private HomeGenieService homegenie = null;
        private bool isProgramEnabled = false;
        private string codeType = "";
        internal ManualResetEvent RoutedEventAck = new ManualResetEvent(false);

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
        internal Func<bool> Stopping = null;
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
        public ConditionType ConditionType { get; set; }
        public List<ProgramCondition> Conditions { get; set; }
        public List<ProgramCommand> Commands { get; set; }

        // c# program public members
        public string ScriptCondition { get; set; }
        public string ScriptSource { get; set; }
        public string ScriptErrors { get; set; }

        // common public members
        public string Domain  { get; set; }
        public int Address  { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Group { get; set; }
        public List<ProgramFeature> Features  { get; set; }

        [XmlIgnore]
        public bool IsRunning { get; set; }
        [XmlIgnore]
        public bool LastConditionEvaluationResult { get; set; }

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
                case "wizard":
                    scriptEngine = new WizardEngine(homegenie);
                    break;
                }
                if (homegenie != null && scriptEngine != null)
                {
                    SetupScriptingScope();
                }
            }
        }

        public DateTime? ActivationTime { get; set; }
        public DateTime? TriggerTime { get; set; }

        public ProgramBlock()
        {
            // init stuff
            Domain = Domains.HomeAutomation_HomeGenie_Automation;
            Address = 0;
            Features = new List<ProgramFeature>();

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
            if (hgScriptingHost != null)
            {
                this.Reset();
                hgScriptingHost = null;
            }
            hgScriptingHost = new ScriptingHost();
            hgScriptingHost.SetHost(homegenie, this.Address);
            if (scriptEngine.GetType() == typeof(ScriptEngine))
            {
                // IronPyton and IronRuby engines
                var ironEngine = (scriptEngine as ScriptEngine);
                dynamic scope = scriptScope = ironEngine.CreateScope();
                scope.hg = hgScriptingHost;
            }
            else if (scriptEngine.GetType() == typeof(Jint.Engine))
            {
                // Jint Javascript engine
                var javascriptEngine = (scriptEngine as Jint.Engine);
                javascriptEngine.SetValue("hg", hgScriptingHost);
            }
            else if (scriptEngine.GetType() == typeof(WizardEngine))
            {
                var wizardEngine = (scriptEngine as WizardEngine);
                wizardEngine.SetScriptingHost(hgScriptingHost);
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
                try { Stop(); } catch {
                    // TODO: handle this...
                }
                if (programDomain != null)
                {
                    // Unloading program app domain...
                    try { AppDomain.Unload(programDomain); } catch { }
                    programDomain = null;
                }
                // clean up old assembly files
                try
                {
                    if (File.Exists(this.AssemblyFile))
                    {
                        File.Delete(this.AssemblyFile);
                    }
                    if (File.Exists(this.AssemblyFile + ".mdb"))
                    {
                        File.Delete(this.AssemblyFile + ".mdb");
                    }
                    if (File.Exists(this.AssemblyFile.Replace(".dll", ".mdb")))
                    {
                        File.Delete(this.AssemblyFile.Replace(".dll", ".mdb"));
                    }
                    if (File.Exists(this.AssemblyFile + ".pdb"))
                    {
                        File.Delete(this.AssemblyFile + ".pdb");
                    }
                    if (File.Exists(this.AssemblyFile.Replace(".dll", ".pdb")))
                    {
                        File.Delete(this.AssemblyFile.Replace(".dll", ".pdb"));
                    }
                }
                catch (Exception ee)
                {
                    HomeGenieService.LogError(ee);
                }
                // move/copy new assembly files
                // rename temp file to production file
                if (value != null)
                try
                {
                    string tmpfile = new Uri(value.CodeBase).LocalPath;
                    File.Move(tmpfile, this.AssemblyFile);
                    if (File.Exists(tmpfile + ".mdb"))
                    {
                        File.Move(tmpfile + ".mdb", this.AssemblyFile + ".mdb");
                    }
                    if (File.Exists(tmpfile.Replace(".dll", ".mdb")))
                    {
                        File.Move(tmpfile.Replace(".dll", ".mdb"), this.AssemblyFile.Replace(".dll", ".mdb"));
                    }
                    if (File.Exists(tmpfile + ".pdb"))
                    {
                        File.Move(tmpfile + ".pdb", this.AssemblyFile + ".pdb");
                    }
                    if (File.Exists(tmpfile.Replace(".dll", ".pdb")))
                    {
                        File.Move(tmpfile.Replace(".dll", ".pdb"), this.AssemblyFile.Replace(".dll", ".pdb"));
                    }
                }
                catch (Exception ee)
                {
                    HomeGenieService.LogError(ee);
                }
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
                    byte[] assemblyData = File.ReadAllBytes(this.AssemblyFile);
                    byte[] debugData = null;
                    if (File.Exists(this.AssemblyFile + ".mdb"))
                    {
                        debugData = File.ReadAllBytes(this.AssemblyFile + ".mdb");
                    }
                    else if (File.Exists(this.AssemblyFile + ".pdb"))
                    {
                        debugData = File.ReadAllBytes(this.AssemblyFile + ".pdb");
                    }
                    AppDomain.CurrentDomain.SetShadowCopyFiles();
                    if (debugData != null)
                    {
                        appAssembly = Assembly.Load(assemblyData, debugData);
                    }
                    else
                    {
                        appAssembly = Assembly.Load(assemblyData);
                    }
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
                //hgScriptingHost = null;
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
                    HomeGenieService.LogError(
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
            case "wizard":
                WizardEngine wizardEngine = (scriptEngine as WizardEngine);
                result = new MethodRunResult();
                try
                {
                    wizardEngine.ExecuteScript(this.Commands);
                }
                catch (Exception e)
                {
                    result.Exception = e;
                }
                break;
            case "arduino":
                result = new MethodRunResult();
                homegenie.RaiseEvent(
                    Domains.HomeAutomation_HomeGenie_Automation,
                    this.Address.ToString(),
                    "Arduino Sketch Upload",
                    "Arduino.UploadOutput",
                    "Upload started"
                );
                string[] outputResult = ArduinoAppFactory.UploadSketch(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "programs",
                    "arduino",
                    this.Address.ToString()
                )).Split('\n');
                //
                for (int x = 0; x < outputResult.Length; x++)
                {
                    if (!String.IsNullOrWhiteSpace(outputResult[x]))
                    {
                        homegenie.RaiseEvent(
                            Domains.HomeAutomation_HomeGenie_Automation,
                            this.Address.ToString(),
                            "Arduino Sketch",
                            "Arduino.UploadOutput",
                            outputResult[x]
                        );
                        Thread.Sleep(500);
                    }
                }
                //
                homegenie.RaiseEvent(
                    Domains.HomeAutomation_HomeGenie_Automation,
                    this.Address.ToString(),
                    "Arduino Sketch",
                    "Arduino.UploadOutput",
                    "Upload finished"
                );
                break;
            }
            //
            return result;
        }

        internal ProgramError GetFormattedError(Exception e, bool isTriggerBlock)
        {
            ProgramError error = new ProgramError() {
                CodeBlock = isTriggerBlock ? "TC" : "CR",
                Column = 0,
                Line = 0,
                ErrorNumber = "-1",
                ErrorMessage = e.Message
            };
            switch (codeType.ToLower())
            {
            case "csharp":
                var st = new StackTrace(e, true);
                error.Line = st.GetFrame(0).GetFileLineNumber();
                if (isTriggerBlock)
                {
                    int sourceLines = this.ScriptSource.Split('\n').Length;
                    error.Line -=  (CSharpAppFactory.CONDITION_CODE_OFFSET + CSharpAppFactory.PROGRAM_CODE_OFFSET + sourceLines);
                }
                else
                {
                    error.Line -=  CSharpAppFactory.PROGRAM_CODE_OFFSET;
                }
                break;
            case "python":
            case "ruby":
                string[] message = ((ScriptEngine)scriptEngine).GetService<ExceptionOperations>().FormatException(e).Split(',');
                if (message.Length > 2)
                {
                    int line = 0;
                    int.TryParse(message[1].Substring(5), out line);
                    error.Line = line;
                }
                break;
            case "javascript":
                break;
            }
            return error;
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
            case "wizard":
                WizardEngine wizardEngine = (scriptEngine as WizardEngine);
                result = new MethodRunResult();
                try
                {
                    result.ReturnValue = wizardEngine.EvaluateTrigger(this.Conditions);
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

        internal void Stop()
        {
            if (this.Stopping != null)
            {
                try { Stopping(); } catch { }
            }
            this.Reset();
            this.IsRunning = false;
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
            Stopping = null;
            //
            foreach (string apiCall in registeredApiCalls)
            {
                homegenie.ProgramEngine.UnRegisterDynamicApi(apiCall);
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

    public class ProgramError
    {
        public int Line = 0;
        public int Column = 0;
        public string ErrorMessage;
        public string ErrorNumber;
        public string CodeBlock;
    }

}

