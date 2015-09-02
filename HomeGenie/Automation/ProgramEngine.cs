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
using System.Threading;
using System.IO;
using HomeGenie.Automation.Scripting;
using HomeGenie.Data;
using HomeGenie.Service;
using MIG;
using HomeGenie.Service.Constants;
using HomeGenie.Automation.Scheduler;
using Microsoft.Scripting.Hosting;
using Newtonsoft.Json;
using System.Globalization;
using System.Diagnostics;

namespace HomeGenie.Automation
{

    public class ProgramEngine
    {
        public delegate void ConditionEvaluationCallback(ProgramBlock p, bool conditionsatisfied);

        private TsList<ProgramBlock> automationPrograms = new TsList<ProgramBlock>();
        private HomeGenieService homegenie = null;
        private SchedulerService scheduler = null;
        private MacroRecorder macroRecorder = null;
        //private object lockObject = new object();
        private bool isEngineRunning = true;
        private bool isEngineEnabled = false;
        public static int USER_SPACE_PROGRAMS_START = 1000;

        public class ScriptEngineErrors : ErrorListener
        {
            private string blockType = "TC";
            public List<ProgramError> Errors = new List<ProgramError>();

            public ScriptEngineErrors(string type)
            {
                blockType = type;
            }

            public override void ErrorReported(ScriptSource source, string message, Microsoft.Scripting.SourceSpan span, int errorCode, Microsoft.Scripting.Severity severity)
            {
                Errors.Add(new ProgramError() {
                    Line = span.Start.Line,
                    Column = span.Start.Column,
                    ErrorMessage = message,
                    ErrorNumber = errorCode.ToString(),
                    CodeBlock = blockType
                });
            }
        }

        public class EvaluateProgramConditionArgs
        {
            public ProgramBlock Program;
            public ConditionEvaluationCallback Callback;
        }

        public ProgramEngine(HomeGenieService hg)
        {
            homegenie = hg;
            macroRecorder = new MacroRecorder(this);
            scheduler = new SchedulerService(this);
            scheduler.Start();
        }

        public void EvaluateProgramCondition(object evalArguments)
        {
            ProgramBlock program = (evalArguments as EvaluateProgramConditionArgs).Program;
            ConditionEvaluationCallback callback = (evalArguments as EvaluateProgramConditionArgs).Callback;
            //
            bool isConditionSatisfied = false;
            //
            while (isEngineRunning && program.IsEnabled)
            {
                if (program.IsRunning || !isEngineEnabled)
                {
                    Thread.Sleep(1000);
                    continue;
                }
                //
                program.RoutedEventAck.WaitOne(1000);
                //
                try
                {
                    isConditionSatisfied = false;
                    //
                    var result = program.EvaluateCondition();
                    if (result != null && result.Exception != null)
                    {
                        // runtime error occurred, script is being disabled
                        // so user can notice and fix it
                        List<ProgramError> error = new List<ProgramError>() { program.GetFormattedError(result.Exception, true) };
                        program.ScriptErrors = JsonConvert.SerializeObject(error);
                        program.IsEnabled = false;
                        RaiseProgramModuleEvent(program, Properties.RUNTIME_ERROR, "TC: " + result.Exception.Message.Replace('\n', ' ').Replace('\r', ' '));
                    }
                    else
                    {
                        isConditionSatisfied = (result != null ? (bool)result.ReturnValue : false);
                    }
                    //
                    bool lastResult = program.LastConditionEvaluationResult;
                    program.LastConditionEvaluationResult = isConditionSatisfied;
                    //
                    if (program.ConditionType == ConditionType.OnSwitchTrue)
                    {
                        isConditionSatisfied = (isConditionSatisfied == true && isConditionSatisfied != lastResult);
                    }
                    else if (program.ConditionType == ConditionType.OnSwitchFalse)
                    {
                        isConditionSatisfied = (isConditionSatisfied == false && isConditionSatisfied != lastResult);
                    }
                    else if (program.ConditionType == ConditionType.OnTrue || program.ConditionType == ConditionType.Once)
                    {
                        // noop
                    }
                    else if (program.ConditionType == ConditionType.OnFalse)
                    {
                        isConditionSatisfied = !isConditionSatisfied;
                    }
                }
                catch (Exception ex)
                {
                    // a runtime error occured
                    if (!ex.GetType().Equals(typeof(System.Reflection.TargetException)))
                    {
                        List<ProgramError> error = new List<ProgramError>() { program.GetFormattedError(ex, true) };
                        program.ScriptErrors = JsonConvert.SerializeObject(error);
                        program.IsEnabled = false;
                        RaiseProgramModuleEvent(program, Properties.RUNTIME_ERROR, "TC: " + ex.Message.Replace('\n', ' ').Replace('\r', ' '));
                    }
                }
                //
                callback(program, isConditionSatisfied);
                //
                program.RoutedEventAck.Reset();
            }
        }

        public bool Enabled
        {
            get { return isEngineEnabled; }
            set { isEngineEnabled = value; }
        }

        public HomeGenieService HomeGenie
        {
            get { return homegenie; }
        }

        public MacroRecorder MacroRecorder
        {
            get { return macroRecorder; }
        }

        public SchedulerService SchedulerService
        {
            get { return scheduler; }
        }

        public List<ProgramError> CompileScript(ProgramBlock program)
        {
            List<ProgramError> errors = new List<ProgramError>();
            switch (program.Type.ToLower())
            {
            case "csharp":
                errors = CompileCsharp(program);
                break;
            case "ruby":
            case "python":
                errors = CompileIronScript(program);
                break;
            case "javascript":
                errors = CompileJavascript(program);
                break;
            case "arduino":
                errors = CompileArduino(program);
                break;
            }
            return errors;
        }

        public void Run(ProgramBlock program, string options)
        {
            if (program.IsRunning)
                return;
            //
            if (program.ProgramThread != null)
            {
                program.Stop();
                program.IsRunning = false;
            }
            //
            program.IsRunning = true;
            RaiseProgramModuleEvent(program, Properties.PROGRAM_STATUS, "Running");
            //
            program.TriggerTime = DateTime.UtcNow;
            program.ProgramThread = new Thread(() =>
            {
                MethodRunResult result = null;
                try
                {
                    result = program.Run(options);
                }
                catch (Exception ex)
                {
                    result = new MethodRunResult();
                    result.Exception = ex;
                }
                //
                if (result != null && result.Exception != null && !result.Exception.GetType().Equals(typeof(System.Reflection.TargetException)))
                {
                    // runtime error occurred, script is being disabled
                    // so user can notice and fix it
                    List<ProgramError> error = new List<ProgramError>() { program.GetFormattedError(result.Exception, false) };
                    program.ScriptErrors = JsonConvert.SerializeObject(error);
                    program.IsEnabled = false;
                    RaiseProgramModuleEvent(program, Properties.RUNTIME_ERROR, "CR: " + result.Exception.Message.Replace('\n', ' ').Replace('\r', ' '));
                }
                program.IsRunning = false;
                program.ProgramThread = null;
                RaiseProgramModuleEvent(program, Properties.PROGRAM_STATUS, "Idle");
            });
            //
            if (program.ConditionType == ConditionType.Once)
            {
                program.IsEnabled = false;
            }
            //
            try
            {
                program.ProgramThread.Start();
            }
            catch
            {
                program.Stop();
                program.IsRunning = false;
                RaiseProgramModuleEvent(program, Properties.PROGRAM_STATUS, "Idle");
            }
            //
            //Thread.Sleep(100);
        }

        public void StopEngine()
        {
            isEngineRunning = false;
            scheduler.Stop();
            foreach (ProgramBlock program in automationPrograms)
            {
                program.Stop();
            }
        }

        public TsList<ProgramBlock> Programs { get { return automationPrograms; } }

        public int GeneratePid()
        {
            int pid = USER_SPACE_PROGRAMS_START;
            foreach (ProgramBlock program in automationPrograms)
            {
                if (pid <= program.Address)
                    pid = program.Address + 1;
            }
            return pid;
        }

        public void ProgramAdd(ProgramBlock program)
        {
            program.SetHost(homegenie);
            automationPrograms.Add(program);
            program.EnabledStateChanged += program_EnabledStateChanged;
            //
            // in case of c# script preload assembly from generated .dll
            if (program.Type.ToLower() == "csharp" && !program.AssemblyLoad())
            {
                program.ScriptErrors = "Program update is required.";
            }
            //
            // Initialize state
            RaiseProgramModuleEvent(program, Properties.PROGRAM_STATUS, "Idle");
            if (program.IsEnabled)
            {
                StartProgramEvaluator(program);
            }
        }

        public void ProgramRemove(ProgramBlock program)
        {
            program.IsEnabled = false;
            program.Stop();
            automationPrograms.Remove(program);
            // delete program files
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs");
            // remove csharp assembly
            try
            {
                File.Delete(Path.Combine(file, program.Address + ".dll"));
            }
            catch
            {
            }
            // remove arduino folder files 
            try
            {
                Directory.Delete(Path.Combine(file, "arduino", program.Address.ToString()), true);
            } catch { }
        }

        internal void RaiseProgramModuleEvent(ProgramBlock program, string property, string value)
        {
            var programModule = homegenie.Modules.Find(m => m.Domain == Domains.HomeAutomation_HomeGenie_Automation && m.Address == program.Address.ToString());
            if (programModule != null)
            {
                Utility.ModuleParameterSet(programModule, property, value);
                homegenie.RaiseEvent(programModule.Domain, programModule.Address, "Automation Program", property, value);
                //homegenie.MigService.RaiseEvent(actionEvent);
                //homegenie.SignalModulePropertyChange(this, programModule, actionEvent);
            }
        }

        private List<ProgramError> CompileIronScript(ProgramBlock program)
        {
            List<ProgramError> errors = new List<ProgramError>();

            ScriptSource source = (program.scriptEngine as ScriptEngine).CreateScriptSourceFromString(program.ScriptCondition);
            ScriptEngineErrors errorListener = new ScriptEngineErrors("TC");
            source.Compile(errorListener);
            errors.AddRange(errorListener.Errors);
            errorListener = new ScriptEngineErrors("CR");
            source = (program.scriptEngine as ScriptEngine).CreateScriptSourceFromString(program.ScriptSource);
            source.Compile(errorListener);
            errors.AddRange(errorListener.Errors);

            return errors;
        }

        private List<ProgramError> CompileJavascript(ProgramBlock program)
        {
            List<ProgramError> errors = new List<ProgramError>();

            Jint.Parser.JavaScriptParser jp = new Jint.Parser.JavaScriptParser(false);
            //Jint.Parser.ParserOptions po = new Jint.Parser.ParserOptions();
            try
            {
                jp.Parse(program.ScriptCondition);
            }
            catch (Exception e)
            {
                // TODO: parse error message
                if (e.Message.Contains(":"))
                {
                    string[] error = e.Message.Split(':');
                    string message = error[1];
                    if (message != "hg is not defined") // TODO: find a better solution for this
                    {
                        int line = int.Parse(error[0].Split(' ')[1]);
                        errors.Add(new ProgramError() {
                            Line = line,
                            ErrorMessage = message,
                            CodeBlock = "TC"
                        });
                    }
                }
            }
            //
            try
            {
                jp.Parse(program.ScriptSource);
            }
            catch (Exception e)
            {
                // TODO: parse error message
                if (e.Message.Contains(":"))
                {
                    string[] error = e.Message.Split(':');
                    string message = error[1];
                    if (message != "hg is not defined") // TODO: find a better solution for this
                    {
                        int line = int.Parse(error[0].Split(' ')[1]);
                        errors.Add(new ProgramError() {
                            Line = line,
                            ErrorMessage = message,
                            CodeBlock = "CR"
                        });
                    }
                }
            }

            return errors;
        }

        private List<ProgramError> CompileCsharp(ProgramBlock program)
        {
            List<ProgramError> errors = new List<ProgramError>();

            // check for output directory
            if (!Directory.Exists(Path.GetDirectoryName(program.AssemblyFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(program.AssemblyFile));
            }

            // dispose assembly and interrupt current task (if any)
            program.AppAssembly = null;
            program.IsEnabled = false;

            // DO NOT CHANGE THE FOLLOWING LINES OF CODE
            // it is a lil' trick for mono compatibility
            // since it will be caching the assembly when using the same name
            // and use the old one instead of the new one
            string tmpfile = Path.Combine("programs", Guid.NewGuid().ToString() + ".dll");
            System.CodeDom.Compiler.CompilerResults result = new System.CodeDom.Compiler.CompilerResults(null);
            try
            {
                result = CSharpAppFactory.CompileScript(program.ScriptCondition, program.ScriptSource, tmpfile);
            }
            catch (Exception ex)
            {
                // report errors during post-compilation process
                result.Errors.Add(new System.CodeDom.Compiler.CompilerError(program.Name, 0, 0, "-1", ex.Message));
            }

            if (result.Errors.Count > 0)
            {
                int sourceLines = program.ScriptSource.Split('\n').Length;
                foreach (System.CodeDom.Compiler.CompilerError error in result.Errors)
                {
                    int errorRow = (error.Line - CSharpAppFactory.PROGRAM_CODE_OFFSET);
                    string blockType = "CR";
                    if (errorRow >= sourceLines + CSharpAppFactory.CONDITION_CODE_OFFSET)
                    {
                        errorRow -= (sourceLines + CSharpAppFactory.CONDITION_CODE_OFFSET);
                        blockType = "TC";
                    }
                    if (!error.IsWarning)
                    {
                        errors.Add(new ProgramError() {
                            Line = errorRow,
                            Column = error.Column,
                            ErrorMessage = error.ErrorText,
                            ErrorNumber = error.ErrorNumber,
                            CodeBlock = blockType
                        });
                    }
                    else
                    {
                        var warning = String.Format("{0},{1},{2}: {3}", blockType, errorRow, error.Column, error.ErrorText);
                        RaiseProgramModuleEvent(program, Properties.COMPILER_WARNING, warning);
                    }
                }
            }
            if (errors.Count == 0)
            {
                program.AppAssembly = result.CompiledAssembly;
            }

            return errors;
        }

        private List<ProgramError> CompileArduino(ProgramBlock program)
        {
            List<ProgramError> errors = new List<ProgramError>();

            // Generate, compile and upload Arduino Sketch
            string sketchFileName = ArduinoAppFactory.GetSketchFile(program.Address.ToString());
            if (!Directory.Exists(Path.GetDirectoryName(sketchFileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(sketchFileName));
            }
            string sketchMakefile = Path.Combine(Path.GetDirectoryName(sketchFileName), "Makefile");

            try
            {
                // .ino source is stored in the ScriptSource property
                File.WriteAllText(sketchFileName, program.ScriptSource);
                // Makefile source is stored in the ScriptCondition property
                File.WriteAllText(sketchMakefile, program.ScriptCondition);
                errors = ArduinoAppFactory.CompileSketch(sketchFileName, sketchMakefile);
            }
            catch (Exception e)
            { 
                errors.Add(new ProgramError() {
                    Line = 0,
                    Column = 0,
                    ErrorMessage = "General failure: is 'arduino-mk' package installed?\n\n" + e.Message,
                    ErrorNumber = "500",
                    CodeBlock = "CR"
                });
            }

            return errors;
        }

        private void StartProgramEvaluator(ProgramBlock program)
        {
            EvaluateProgramConditionArgs evalArgs = new EvaluateProgramConditionArgs() {
                Program = program,
                Callback = (ProgramBlock p, bool conditionsatisfied) =>
                {
                    if (conditionsatisfied && p.IsEnabled)
                    {
                        Run(p, null); // that goes async too
                    }
                }
            };
            ThreadPool.QueueUserWorkItem(new WaitCallback(EvaluateProgramCondition), evalArgs);
        }

        private void program_EnabledStateChanged(object sender, bool isEnabled)
        {
            ProgramBlock program = (ProgramBlock)sender;
            if (isEnabled)
            {
                program.ScriptErrors = "";
                homegenie.modules_RefreshPrograms();
                homegenie.modules_RefreshVirtualModules();
                RaiseProgramModuleEvent(program, Properties.PROGRAM_STATUS, "Enabled");
                // TODO: CRITICAL
                // TODO: we should ensure to dispose previous Evaluator Thread before starting the new one
                StartProgramEvaluator(program);
            }
            else
            {
                RaiseProgramModuleEvent(program, Properties.PROGRAM_STATUS, "Disabled");
                homegenie.modules_RefreshPrograms();
                homegenie.modules_RefreshVirtualModules();
            }
            homegenie.modules_Sort();
        }
    }
}