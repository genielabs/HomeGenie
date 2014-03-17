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

namespace HomeGenie.Automation
{
    public class ProgramEngine
    {
        public delegate void ConditionEvaluationCallback(ProgramBlock p, bool conditionsatisfied);

        private HomeGenie.Service.TsList<ProgramBlock> automationPrograms = new HomeGenie.Service.TsList<ProgramBlock>();

        private HomeGenieService homegenie = null;
        private SchedulerService scheduler = null;
        private CSharpAppFactory scriptingHost = null;

        private MacroRecorder macroRecorder = null;

        private object lockoObject = new object();

        private bool isEngineRunning = true;
        private bool isEngineEnabled = false;
        public static int USER_SPACE_PROGRAMS_START = 1000;

        public class EvaluateProgramConditionArgs
        {
            public ProgramBlock Program;
            public ConditionEvaluationCallback Callback;
        }

        public ProgramEngine(HomeGenieService hg)
        {
            homegenie = hg;
            scriptingHost = new CSharpAppFactory(homegenie);
            macroRecorder = new MacroRecorder(this);
            scheduler = new SchedulerService(this);
            scheduler.Start();
        }

        public void EvaluateProgramCondition(object evalArguments)
        {
            ProgramBlock program = (evalArguments as EvaluateProgramConditionArgs).Program;
            ConditionEvaluationCallback callback = (evalArguments as EvaluateProgramConditionArgs).Callback;
            program.IsEvaluatingConditionBlock = true;
            //
            bool isConditionSatisfied = false;
            //
            while (isEngineRunning)
            {
                if (program.IsRunning || !program.IsEnabled || !isEngineEnabled) { Thread.Sleep(500); continue; }
                //
                var stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
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
                            program.ScriptErrors = result.Exception.Message + "\n" + result.Exception.StackTrace;
                            program.IsEnabled = false;
                        }
                        else
                        {
                            isConditionSatisfied = (bool)result.ReturnValue;
                        }
                    }
                    else 
                    {
                        // it is a Wizard Script
                        isConditionSatisfied = (program.Conditions.Count > 0);
                        for (int c = 0; c < program.Conditions.Count; c++)
                        {
                            bool res = VerifyProgramCondition(program.Conditions[c]);
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
                    program.ScriptErrors = ex.Message + "\n" + ex.StackTrace;
                    program.IsEnabled = false;
                }
                //
                stopWatch.Stop();
                //
                callback(program, isConditionSatisfied);
                //
                int nextDelay = (int)(400 + (stopWatch.ElapsedMilliseconds > 400 ? stopWatch.ElapsedMilliseconds - 400 : 0));
                if (nextDelay > 500) nextDelay = 500;
                //
                Thread.Sleep(nextDelay);
            }
            program.IsEvaluatingConditionBlock = false;
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

        public System.CodeDom.Compiler.CompilerResults CompileScript(ProgramBlock program)
        {
            if (!Directory.Exists(Path.GetDirectoryName(program.AssemblyFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(program.AssemblyFile));
            }
            // DO NOT CHANGE THE FOLLOWING LINES OF CODE
            // it is a lil' trick for mono compatibility
            // since it was caching the assembly when using the same name
            string tmpfile = Guid.NewGuid().ToString() + ".dll";
            var result = scriptingHost.CompileScript(program.ScriptCondition, program.ScriptSource, tmpfile);
            // delete old assembly
            try
            {

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
                //pb.ScriptErrors = ex.Message + "\n" + ex.StackTrace;
                result.Errors.Add(new System.CodeDom.Compiler.CompilerError(program.Name, 0, 0, "0", ex.Message + "\n" + ex.StackTrace));
            }
            return result;
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
            lock (lockoObject)
            {
                program.IsRunning = true;
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
                            var result = program.Run(options);
                            if (result != null && result.Exception != null)
                            {
                                // runtime error occurred, script is being disabled
                                // so user can notice and fix it
                                program.ScriptErrors = result.Exception.Message + "\n" + result.Exception.StackTrace;
                                program.IsEnabled = false;
                            }
                            program.IsRunning = false;
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
                    });
                    //
                    program.ProgramThread.Start();
                }
                //
                Thread.Sleep(100);
            }

        }

        public void StopEngine()
        {
            isEngineRunning = false;
            scheduler.Stop();
            //lock (_programblocks)
            {
                foreach (ProgramBlock program in automationPrograms)
                {
                    program.Stop();
                }
            }
        }

        public TsList<ProgramBlock> Programs { get { lock (automationPrograms) return automationPrograms; } }
        
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
            lock (automationPrograms)
            {
                program.SetHost(homegenie);
                automationPrograms.Add(program);
            }
            //
            EvaluateProgramConditionArgs evalArgs = new EvaluateProgramConditionArgs()
            {
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

        public void ProgramRemove(ProgramBlock program)
        {
            program.Stop();
            program.IsEnabled = false;
            lock (automationPrograms)
            {
                automationPrograms.Remove(program);
            }
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
                                case "Program.Pause":
                                    Thread.Sleep((int)(double.Parse(program.Commands[x].CommandArguments, System.Globalization.CultureInfo.InvariantCulture) * 1000));
                                    break;
                                case "Program.Repeat":
                                    // TODO: implement check for contiguous repeat statements
                                    if (repeatCount <= 0)
                                    {
                                        repeatCount = (int)(double.Parse(program.Commands[x].CommandArguments, System.Globalization.CultureInfo.InvariantCulture));
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

        private bool VerifyProgramCondition(ProgramCondition c)
        {
            bool returnValue = false;
            string comparisonValue = c.ComparisonValue;
            //
            if (c.Domain == Domains.HomeAutomation_HomeGenie && c.Target == "Automation" && c.Property == "Scheduler.TimeEvent")
            {
                return homegenie.ProgramEngine.SchedulerService.IsScheduling(c.ComparisonValue);
            }
            //
            try
            {
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
                            parameter.Value = DateTime.Now.Day.ToString();
                            break;
                        case "Programs.DateMonth":
                            parameter.Value = DateTime.Now.Month.ToString();
                            break;
                        case "Programs.DateDayOfWeek":
                            parameter.Value = ((int)DateTime.Now.DayOfWeek).ToString();
                            break;
                        case "Programs.DateYear":
                            parameter.Value = DateTime.Now.Year.ToString();
                            break;
                        case "Programs.DateHour":
                            parameter.Value = DateTime.Now.Hour.ToString();
                            break;
                        case "Programs.DateMinute":
                            parameter.Value = DateTime.Now.Minute.ToString();
                            break;
                        case "Programs.Date":
                            parameter.Value = DateTime.Now.ToString("YY-MM-dd");
                            break;
                        case "Programs.Time":
                            parameter.Value = DateTime.Now.ToString("HH:mm:ss");
                            break;
                        case "Programs.DateTime":
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
                    if (DateTime.TryParse(parameter.Value, out dtval))
                    {
                        lvalue = dtval;
                        rvalue = DateTime.Parse(comparisonValue);
                    }
                    else if (double.TryParse(parameter.Value, out dval))
                    {
                        lvalue = dval;
                        rvalue = double.Parse(comparisonValue);
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
            }
            catch
            {
            }
            return returnValue;
        }

        private void ExecuteProgramCommand(ProgramCommand programCommand)
        {
            string command = programCommand.Domain + "/" + programCommand.Target + "/" + programCommand.CommandString + "/" + programCommand.CommandArguments;
            var interfaceCommand = new MIGInterfaceCommand(command);
            homegenie.InterfaceControl(interfaceCommand);
            homegenie.WaitOnPending(programCommand.Domain);
        }

    }
}