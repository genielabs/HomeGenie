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
    public class ProgramError
    {
        public int Line = 0;
        public int Column = 0;
        public string ErrorMessage;
        public string ErrorNumber;
        public string CodeBlock;
    }

    public class ProgramEngine
    {
        public delegate void ConditionEvaluationCallback(ProgramBlock p, bool conditionsatisfied);

        private HomeGenie.Service.TsList<ProgramBlock> automationPrograms = new HomeGenie.Service.TsList<ProgramBlock>();

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

            public override void ErrorReported(
                ScriptSource source,
                string message,
                Microsoft.Scripting.SourceSpan span,
                int errorCode,
                Microsoft.Scripting.Severity severity
            )
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
                try
                {
                    isConditionSatisfied = false;
                    //
                    if (program.Type.ToLower() != "wizard")
                    {
                        var result = program.EvaluateCondition();
                        if (result != null && result.Exception != null)
                        {
                            // runtime error occurred, script is being disabled
                            // so user can notice and fix it
                            List<ProgramError> error = new List<ProgramError>() { new ProgramError() {
                                    CodeBlock = "TC",
                                    Column = 0,
                                    Line = 0,
                                    ErrorNumber = "-1",
                                    ErrorMessage = result.Exception.Message
                                }
                            };
                            program.ScriptErrors = JsonConvert.SerializeObject(error);
                            program.IsEnabled = false;
                            RaiseProgramModuleEvent(
                                program,
                                Properties.RUNTIME_ERROR,
                                "TC: " + result.Exception.Message.Replace(
                                    '\n',
                                    ' '
                                )
                            );
                        }
                        else
                        {
                            isConditionSatisfied = (result != null ? (bool)result.ReturnValue : false);
                        }
                    }
                    else
                    {
                        // it is a Wizard Script
                        isConditionSatisfied = (program.Conditions.Count > 0);
                        for (int c = 0; c < program.Conditions.Count; c++)
                        {
                            // check for OR logic operator
                            if (program.Conditions[c].ComparisonOperator == ComparisonOperator.LogicOrJoint)
                            {
                                if (isConditionSatisfied)
                                {
                                    break;
                                }
                                else
                                {
                                    isConditionSatisfied = (c < program.Conditions.Count - 1);
                                    continue;
                                }
                            }
                            //
                            bool res = false;
                            try
                            {
                                res = VerifyProgramCondition(program.Conditions[c]);
                            } catch {
                                // TODO: report/handle exception
                            }
                            isConditionSatisfied = (isConditionSatisfied && res);
                        }
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
                    List<ProgramError> error = new List<ProgramError>() { new ProgramError() {
                            CodeBlock = "TC",
                            Column = 0,
                            Line = 0,
                            ErrorNumber = "-1",
                            ErrorMessage = ex.Message
                        }
                    };
                    program.ScriptErrors = JsonConvert.SerializeObject(error);
                    program.IsEnabled = false;
                    RaiseProgramModuleEvent(program, Properties.RUNTIME_ERROR, "TC: " + ex.Message.Replace('\n', ' '));
                }
                //
                callback(program, isConditionSatisfied);
                //
                Thread.Sleep(500);
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
            if (program.IsRunning) return;
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
            if (program.Type.ToLower() != "wizard")
            {
                if (program.Type.ToLower() == "csharp" && program.AppAssembly == null)
                {
                    program.IsRunning = false;
                }
                else
                {
                    program.TriggerTime = DateTime.UtcNow;
                    program.ProgramThread = new Thread(() =>
                    {
                        MethodRunResult result = null;
                        try
                        {
                            result = program.Run(options);
                        } catch (Exception ex) {
                            result = new MethodRunResult();
                            result.Exception = ex;
                        }
                        //
                        if (result != null && result.Exception != null)
                        {
                            // runtime error occurred, script is being disabled
                            // so user can notice and fix it
                            List<ProgramError> error = new List<ProgramError>() { new ProgramError() {
                                    CodeBlock = "CR",
                                    Column = 0,
                                    Line = 0,
                                    ErrorNumber = "-1",
                                    ErrorMessage = result.Exception.Message
                                }
                            };
                            program.ScriptErrors = JsonConvert.SerializeObject(error);
                            program.IsEnabled = false;
                            RaiseProgramModuleEvent(
                                program,
                                Properties.RUNTIME_ERROR,
                                "CR: " + result.Exception.Message.Replace(
                                    '\n',
                                    ' '
                                )
                            );
                        }
                        program.IsRunning = false;
                        program.ProgramThread = null;
                        RaiseProgramModuleEvent(program, Properties.PROGRAM_STATUS, "Idle");
                    });
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
                }
            }
            else
            {
                program.TriggerTime = DateTime.UtcNow;
                if (program.ConditionType == ConditionType.Once)
                {
                    program.IsEnabled = false;
                }
                //
                program.ProgramThread = new Thread(() =>
                {
                    try
                    {
                        ExecuteWizardScript(program);
                    }
                    catch (ThreadAbortException)
                    {
                        program.IsRunning = false;
                    }
                    finally
                    {
                        program.IsRunning = false;
                    }
                    RaiseProgramModuleEvent(program, Properties.PROGRAM_STATUS, "Idle");
                });
                //
                program.ProgramThread.Start();
            }
            //
            Thread.Sleep(100);
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
                if (pid <= program.Address) pid = program.Address + 1;
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
            try { File.Delete(Path.Combine(file, program.Address + ".dll")); } catch { }
            // remove arduino folder files 
            try { Directory.Delete(Path.Combine(file, "arduino", program.Address.ToString()), true); } catch { }
        }

        // TODO: find a better solution to this...
        public void ExecuteWizardScript(ProgramBlock program)
        {
            int repeatStartLine = 0;
            int repeatCount = 0;
            for (int x = 0; x < program.Commands.Count; x++)
            {
                if (program.Commands[x].Domain == Domains.HomeAutomation_HomeGenie)
                {
                    switch (program.Commands[x].Target)
                    {
                    case "Automation":
                            //
                        string cs = program.Commands[x].CommandString;
                        switch (cs)
                        {
                        case "Program.Run":
                            string programId = program.Commands[x].CommandArguments;
                            var programToRun = homegenie.ProgramEngine.Programs.Find(p => p.Address.ToString() == programId || p.Name == programId);
                            if (programToRun != null && programToRun.Address != program.Address && !programToRun.IsRunning)
                            {
                                Run(programToRun, "");
                            }
                            break;
                        case "Program.Pause":
                            Thread.Sleep((int)(double.Parse(
                                program.Commands[x].CommandArguments,
                                System.Globalization.CultureInfo.InvariantCulture
                            ) * 1000));
                            break;
                        case "Program.Repeat":
                                    // TODO: implement check for contiguous repeat statements
                            if (repeatCount <= 0)
                            {
                                repeatCount = (int)(double.Parse(
                                    program.Commands[x].CommandArguments,
                                    System.Globalization.CultureInfo.InvariantCulture
                                ));
                            }
                            if (--repeatCount == 0)
                            {
                                repeatStartLine = x + 1;
                            }
                            else
                            {
                                x = repeatStartLine - 1;
                            }
                            break;
                        default:
                            var programCommand = program.Commands[x];
                            string wrequest = programCommand.Domain + "/" + programCommand.Target + "/" + programCommand.CommandString + "/" + programCommand.CommandArguments;
                            homegenie.ExecuteAutomationRequest(new MIGInterfaceCommand(wrequest));
                            break;
                        }
                            //
                        break;
                    }
                }
                else
                {
                    ExecuteProgramCommand(program.Commands[x]);
                }
                //
                Thread.Sleep(10);
            }
        }

        internal void RaiseProgramModuleEvent(ProgramBlock program, string property, string value)
        {
            var programModule = homegenie.Modules.Find(m => m.Domain == Domains.HomeAutomation_HomeGenie_Automation && m.Address == program.Address.ToString());
            if (programModule != null)
            {
                var actionEvent = new MIG.InterfacePropertyChangedAction();
                actionEvent.Domain = programModule.Domain;
                actionEvent.Path = property;
                actionEvent.Value = value;
                actionEvent.SourceId = programModule.Address;
                actionEvent.SourceType = "Automation Program";
                Utility.ModuleParameterSet(programModule, property, value);
                homegenie.SignalModulePropertyChange(this, programModule, actionEvent);
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
                        int line = int.Parse(error[0].Split(' ')[0]);
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
                        int line = int.Parse(error[0].Split(' ')[0]);
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

            // dispose assembly and interrupt current task
            program.AppAssembly = null;
            program.IsEnabled = false;

            if (!Directory.Exists(Path.GetDirectoryName(program.AssemblyFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(program.AssemblyFile));
            }
            // DO NOT CHANGE THE FOLLOWING LINES OF CODE
            // it is a lil' trick for mono compatibility
            // since it was caching the assembly when using the same name
            string tmpfile = Guid.NewGuid().ToString() + ".dll";
            // delete old assembly
            System.CodeDom.Compiler.CompilerResults result = new System.CodeDom.Compiler.CompilerResults(null);
            try
            {
                result = CSharpAppFactory.CompileScript(program.ScriptCondition, program.ScriptSource, tmpfile);
                if (File.Exists(program.AssemblyFile))
                {
                    // delete old assebly
                    File.Delete(program.AssemblyFile);
                }
                // move newly compiled assembly to programs folder
                if (result.Errors.Count == 0)
                {
                    // submitting new assembly
                    File.Move(tmpfile, program.AssemblyFile);
                }
            }
            catch (Exception ex)
            {
                // report errors during post-compilation process
                result.Errors.Add(new System.CodeDom.Compiler.CompilerError(program.Name, 0, 0, "-1", ex.Message));
            }

            int startCodeLine = 19;
            int conditionCodeOffset = 7;
            //
            if (result.Errors.Count == 0)
            {
                program.AppAssembly = result.CompiledAssembly;
            }
            else
            {
                int sourceLines = program.ScriptSource.Split('\n').Length;
                foreach (System.CodeDom.Compiler.CompilerError error in result.Errors)
                {
                    //if (!ce.IsWarning)
                    {
                        int errorRow = (error.Line - startCodeLine);
                        string blockType = "CR";
                        if (errorRow >= sourceLines + conditionCodeOffset)
                        {
                            errorRow -= (sourceLines + conditionCodeOffset);
                            blockType = "TC";
                        }
                        errors.Add(new ProgramError() {
                            Line = errorRow,
                            Column = error.Column,
                            ErrorMessage = error.ErrorText,
                            ErrorNumber = error.ErrorNumber,
                            CodeBlock = blockType
                        });
                    }
                }
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

        private bool VerifyProgramCondition(ProgramCondition c)
        {
            bool returnValue = false;
            string comparisonValue = c.ComparisonValue;
            //
            if (c.Domain == Domains.HomeAutomation_HomeGenie && c.Target == "Automation" && (c.Property == "Scheduler.TimeEvent" || c.Property == "Scheduler.CronEvent"))
            {
                return homegenie.ProgramEngine.SchedulerService.IsScheduling(c.ComparisonValue);
            }
            //
            // if the comparison value starts with ":", then the value is read from another module property
            // eg: ":HomeAutomation.X10/B3/Level"
            if (comparisonValue.StartsWith(":"))
            {
                string[] propertyPath = comparisonValue.Substring(1).Split('/');
                comparisonValue = "";
                if (propertyPath.Length >= 3)
                {
                    string domain = propertyPath[0];
                    string address = propertyPath[1];
                    string propertyName = propertyPath[2];
                    var targetModule = homegenie.Modules.Find(m => m.Domain == domain && m.Address == address);
                    if (targetModule == null)
                    {
                        // abbreviated path, eg: ":X10/B3/Level"
                        targetModule = homegenie.Modules.Find(m => m.Domain.EndsWith("." + domain) && m.Address == address);
                    }
                    //
                    if (targetModule != null)
                    {
                        var mprop = Utility.ModuleParameterGet(targetModule, propertyName);
                        if (mprop != null)
                        {
                            comparisonValue = mprop.Value;
                        }
                    }
                }
            }
            //
            // the following Programs.* parameters are deprecated, just left for compatibility with HG < r340
            //
            ModuleParameter parameter = null;
            if (c.Domain == Domains.HomeAutomation_HomeGenie && c.Target == "Automation")
            {
                parameter = new ModuleParameter();
                parameter.Name = c.Property;
                switch (parameter.Name)
                {
                case "Programs.DateDay":
                case "Scheduler.DateDay":
                    parameter.Value = DateTime.Now.Day.ToString();
                    break;
                case "Programs.DateMonth":
                case "Scheduler.DateMonth":
                    parameter.Value = DateTime.Now.Month.ToString();
                    break;
                case "Programs.DateDayOfWeek":
                case "Scheduler.DateDayOfWeek":
                    parameter.Value = ((int)DateTime.Now.DayOfWeek).ToString();
                    break;
                case "Programs.DateYear":
                case "Scheduler.DateYear":
                    parameter.Value = DateTime.Now.Year.ToString();
                    break;
                case "Programs.DateHour":
                case "Scheduler.DateHour":
                    parameter.Value = DateTime.Now.Hour.ToString();
                    break;
                case "Programs.DateMinute":
                case "Scheduler.DateMinute":
                    parameter.Value = DateTime.Now.Minute.ToString();
                    break;
                case "Programs.Date":
                case "Scheduler.Date":
                    parameter.Value = DateTime.Now.ToString("YY-MM-dd");
                    break;
                case "Programs.Time":
                case "Scheduler.Time":
                    parameter.Value = DateTime.Now.ToString("HH:mm:ss");
                    break;
                case "Programs.DateTime":
                case "Scheduler.DateTime":
                    parameter.Value = DateTime.Now.ToString("YY-MM-dd HH:mm:ss");
                    break;
                }
            }
            else
            {
                Module module = homegenie.Modules.Find(m => m.Address == c.Target && m.Domain == c.Domain);
                parameter = module.Properties.Find(delegate(ModuleParameter mp)
                {
                    return mp.Name == c.Property;
                });
            }
            //
            if (parameter != null)
            {
                IComparable lvalue = parameter.Value;
                IComparable rvalue = comparisonValue;
                //
                double dval = 0;
                DateTime dtval = new DateTime();
                //
                if (double.TryParse(
                        parameter.Value.Replace(",", "."),
                        NumberStyles.Float | NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture,
                        out dval
                    ))
                {
                    lvalue = dval;
                    rvalue = double.Parse(
                        comparisonValue.Replace(",", "."),
                        NumberStyles.Float | NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture
                    );
                }
                else if (DateTime.TryParse(parameter.Value, out dtval))
                {
                    lvalue = dtval;
                    rvalue = DateTime.Parse(comparisonValue);
                }
                //
                int comparisonresult = lvalue.CompareTo(rvalue);
                if (c.ComparisonOperator == ComparisonOperator.LessThan && comparisonresult < 0)
                {
                    returnValue = true;
                }
                else if (c.ComparisonOperator == ComparisonOperator.Equals && comparisonresult == 0)
                {
                    returnValue = true;
                }
                else if (c.ComparisonOperator == ComparisonOperator.GreaterThan && comparisonresult > 0)
                {
                    returnValue = true;
                }
            }
            return returnValue;
        }

        private void ExecuteProgramCommand(ProgramCommand programCommand)
        {
            string command = programCommand.Domain + "/" + programCommand.Target + "/" + programCommand.CommandString + "/" + System.Uri.EscapeDataString(programCommand.CommandArguments);
            var interfaceCommand = new MIGInterfaceCommand(command);
            homegenie.InterfaceControl(interfaceCommand);
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