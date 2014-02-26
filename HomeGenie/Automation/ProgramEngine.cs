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

        private HomeGenie.Service.TsList<ProgramBlock> _programblocks = new HomeGenie.Service.TsList<ProgramBlock>();

        //TODO: deprecate Automation States
        private Dictionary<string, bool> _automationstates = new Dictionary<string, bool>() { 
                { "Security.Armed", false},
                { "Security.Away", false},
                { "Security.Home", false}
        };

        private HomeGenieService _homegenie = null;
        private SchedulerService _schedulersvc = null;
        private ScriptingHost _scriptinghost = null;

        private MacroRecorder _macrorecorder = null;

        private object _lock = new object();

        private bool _enginerunning = true;
        private bool _engineenabled = false;
        public static int USER_SPACE_PROGRAMS_START = 1000;

        public ProgramEngine(HomeGenieService homegenie)
        {
            _homegenie = homegenie;
            _scriptinghost = new ScriptingHost(_homegenie);
            _macrorecorder = new MacroRecorder(this);
            _schedulersvc = new SchedulerService(this);
            _schedulersvc.Start();
        }

        public void EvaluateProgramConditionAsync(ProgramBlock p, ConditionEvaluationCallback callback)
        {
            p.IsEvaluatingConditionBlock = true;
            Thread evaluatorthread = new Thread(new ThreadStart(delegate()
            {
                bool conditionsatisfied = false;
                //
                while (_enginerunning)
                {
                    if (p.IsRunning || !p.IsEnabled || !_engineenabled) { Thread.Sleep(500); continue; }
                    //
                    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    try
                    {
                        conditionsatisfied = false;
                        //
                        if (p.Type.ToLower() == "csharp")
                        {
                            MethodRunResult res = p.EvaluateConditionStatement(_homegenie);
                            if (res != null && res.Exception != null)
                            {
                                // runtime error occurred, script is being disabled
                                // so user can notice and fix it
                                p.ScriptErrors = res.Exception.Message + "\n" + res.Exception.StackTrace;
                                p.IsEnabled = false;
                            }
                            else
                            {
                                conditionsatisfied = (bool)res.ReturnValue;
                            }
                        }
                        else
                        {
                            // it is a Wizard Script
                            conditionsatisfied = (p.Conditions.Count > 0);
                            for (int c = 0; c < p.Conditions.Count; c++)
                            {
                                bool res = _mcp_verifyCondition(p.Conditions[c]);
                                conditionsatisfied = (conditionsatisfied && res);
                            }
                        }
                        //
                        bool lasteval = p.LastConditionEvaluationResult;
                        p.LastConditionEvaluationResult = conditionsatisfied;
                        //
                        if (p.ConditionType == ConditionType.OnSwitchTrue)
                        {
                            conditionsatisfied = (conditionsatisfied == true && conditionsatisfied != lasteval);
                        }
                        else if (p.ConditionType == ConditionType.OnSwitchFalse)
                        {
                            conditionsatisfied = (conditionsatisfied == false && conditionsatisfied != lasteval);
                        }
                        else if (p.ConditionType == ConditionType.OnTrue || p.ConditionType == ConditionType.Once)
                        {
                            // noop
                        }
                        else if (p.ConditionType == ConditionType.OnFalse)
                        {
                            conditionsatisfied = !conditionsatisfied;
                        }
                    }
                    catch (Exception ex)
                    {
                        // a runtime error occured
                        p.ScriptErrors = ex.Message + "\n" + ex.StackTrace;
                        p.IsEnabled = false;
                    }
                    //
                    sw.Stop();
                    //
                    callback(p, conditionsatisfied);
                    //
                    int delaynext = (int)(400 + (sw.ElapsedMilliseconds > 400 ? sw.ElapsedMilliseconds - 400 : 0));
                    if (delaynext > 500) delaynext = 500;
                    //
                    Thread.Sleep(delaynext);
                }
                p.IsEvaluatingConditionBlock = false;
            }));
            //evaluatorthread.Priority = ThreadPriority.AboveNormal;
            evaluatorthread.Start();
        }

        public bool Enabled
        {
            get { return _engineenabled; }
            set { _engineenabled = value; }
        }

        public AutomationStatesManager AutomationStates
        {
            get { return new AutomationStatesManager(_automationstates); }
        }

        public HomeGenieService HomeGenie
        {
            get { return _homegenie; }
        }

        public MacroRecorder MacroRecorder
        {
            get { return _macrorecorder; }
        }

        public SchedulerService SchedulerService
        {
            get { return _schedulersvc; }
        }

        public System.CodeDom.Compiler.CompilerResults CompileScript(ProgramBlock pb)
        {
            if (!Directory.Exists(Path.GetDirectoryName(pb.AssemblyFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(pb.AssemblyFile));
            }
            // DO NOT CHANGE THE FOLLOWING LINES OF CODE
            // it is a lil' trick for mono compatibility
            // since it was caching the assembly when using the same name
            string tmpfile = Guid.NewGuid().ToString() + ".dll";
            System.CodeDom.Compiler.CompilerResults cres = _scriptinghost.CompileScript(pb.ScriptCondition, pb.ScriptSource, tmpfile);
            // delete old assembly
            try
            {

                if (File.Exists(pb.AssemblyFile))
                {
                    // delete old assebly
                    File.Delete(pb.AssemblyFile);
                }
                // move newly compiled assembly to programs folder
                if (cres.Errors.Count == 0)
                {
                    // submitting new assembly
                    File.Move(tmpfile, pb.AssemblyFile);
                }
            }
            catch (Exception ex)
            {
                // report errors during post-compilation process
                //pb.ScriptErrors = ex.Message + "\n" + ex.StackTrace;
                cres.Errors.Add(new System.CodeDom.Compiler.CompilerError(pb.Name, 0, 0, "0", ex.Message + "\n" + ex.StackTrace));
            }
            return cres;
        }

        public void Run(ProgramBlock pb, string options)
        {
            if (pb.IsRunning)
                return;
            //
            if (pb.ProgramThread != null)
            {
                pb.Stop();
                pb.IsRunning = false;
            }
            //
            lock (_lock)
            {
                pb.IsRunning = true;
                //
                if (pb.Type.ToLower() == "csharp")
                {
                    if (pb.ScriptAssembly != null)
                    {
                        pb.TriggerTime = DateTime.UtcNow;
                        pb.ProgramThread = new Thread(new ThreadStart(delegate()
                        {
                            MethodRunResult res = pb.RunScript(_homegenie, options);
                            if (res != null && res.Exception != null)
                            {
                                // runtime error occurred, script is being disabled
                                // so user can notice and fix it
                                pb.ScriptErrors = res.Exception.Message + "\n" + res.Exception.StackTrace;
                                pb.IsEnabled = false;
                            }
                            pb.IsRunning = false;
                        }));
                        pb.ProgramThread.Priority = ThreadPriority.BelowNormal;
                        try
                        {
                            pb.ProgramThread.Start();
                        }
                        catch
                        {
                            pb.Stop();
                            pb.IsRunning = false;
                        }
                    }
                    else
                    {
                        pb.IsRunning = false;
                    }
                }
                else
                {
                    pb.TriggerTime = DateTime.UtcNow;
                    if (pb.ConditionType == ConditionType.Once)
                    {
                        pb.IsEnabled = false;
                    }
                    //
                    pb.ProgramThread = new Thread(new ThreadStart(delegate()
                    {
                        try
                        {
                            ExecuteWizardScript(pb);
                        }
                        catch (ThreadAbortException)
                        {
                            pb.IsRunning = false;
                        }
                        finally
                        {
                            pb.IsRunning = false;
                        }
                    }));
                    pb.ProgramThread.Priority = ThreadPriority.Lowest;
                    pb.ProgramThread.Start();
                }
                //
                Thread.Sleep(100);
            }

        }

        public void StopEngine()
        {
            _enginerunning = false;
            _schedulersvc.Stop();
            //lock (_programblocks)
            {
                foreach (ProgramBlock pb in _programblocks)
                {
                    pb.Stop();
                }
            }
        }

        public TsList<ProgramBlock> Programs { get { lock (_programblocks) return _programblocks; } }


        public int GeneratePid()
        {
            int pid = USER_SPACE_PROGRAMS_START;
            foreach (ProgramBlock pb in _programblocks)
            {
                if (pid <= pb.Address) pid = pb.Address + 1;
            }
            return pid;
        }


        public void ProgramAdd(ProgramBlock pb)
        {
            lock (_programblocks)
            {
                _programblocks.Add(pb);
            }
            EvaluateProgramConditionAsync(pb, (ProgramBlock p, bool conditionsatisfied) =>
            {
                if (conditionsatisfied && p.IsEnabled)
                {
                    Run(p, null); // that goes async too
                }
            });
        }
        public void ProgramRemove(ProgramBlock pb)
        {
            pb.Stop();
            pb.IsEnabled = false;
            lock (_programblocks)
            {
                _programblocks.Remove(pb);
            }
        }

        // TODO: find a better solution to this...
        public void ExecuteWizardScript(ProgramBlock pb)
        {
            int repeatstartline = 0;
            int repeatcount = 0;
            for (int x = 0; x < pb.Commands.Count; x++)
            {
                if (pb.Commands[x].Domain == Domains.HomeAutomation_HomeGenie)
                {
                    switch (pb.Commands[x].Target)
                    {
                        case "Automation":
                            //
                            string cs = pb.Commands[x].CommandString;
                            switch (cs)
                            {
                                case "Program.Pause":
                                    Thread.Sleep((int)(double.Parse(pb.Commands[x].CommandArguments, System.Globalization.CultureInfo.InvariantCulture) * 1000));
                                    break;
                                case "Program.Repeat":
                                    // TODO: implement check for contiguous repeat statements
                                    if (repeatcount <= 0)
                                    {
                                        repeatcount = (int)(double.Parse(pb.Commands[x].CommandArguments, System.Globalization.CultureInfo.InvariantCulture));
                                    }
                                    if (--repeatcount == 0)
                                    {
                                        repeatstartline = x + 1;
                                    }
                                    else
                                    {
                                        x = repeatstartline - 1;
                                    }
                                    break;
                                default:
                                    ProgramCommand c = pb.Commands[x];
                                    string wrequest = c.Domain + "/" + c.Target + "/" + c.CommandString + "/" + c.CommandArguments;
                                    _homegenie.ExecuteAutomationRequest(new MIGInterfaceCommand(wrequest));
                                    break;
                            }
                            //
                            break;
                    }
                }
                else
                {
                    _mcp_executeCommand(pb.Commands[x]);
                }
                //
                Thread.Sleep(10);
            }
        }

        private bool _mcp_verifyCondition(ProgramCondition c)
        {
            bool retval = false;
            string comparisonvalue = c.ComparisonValue;
            //
            if (c.Domain == Domains.HomeAutomation_HomeGenie && c.Target == "Automation" && c.Property == "Scheduler.TimeEvent")
            {
                return _homegenie.ProgramEngine.SchedulerService.IsScheduling(c.ComparisonValue);
            }
            //
            try
            {
                //
                // if the comparison value starts with ":", then the value is read from another module property
                // eg: ":HomeAutomation.X10/B3/Level"
                if (comparisonvalue.StartsWith(":"))
                {
                    string[] proppath = comparisonvalue.Substring(1).Split('/');
                    comparisonvalue = "";
                    if (proppath.Length >= 3)
                    {
                        string domain = proppath[0];
                        string address = proppath[1];
                        string pname = proppath[2];
                        var mtarget = _homegenie.Modules.Find(m => m.Domain == domain && m.Address == address);
                        if (mtarget == null)
                        {
                            // abbreviated path, eg: ":X10/B3/Level"
                            mtarget = _homegenie.Modules.Find(m => m.Domain.EndsWith("." + domain) && m.Address == address);
                        }
                        //
                        if (mtarget != null)
                        {
                            var mprop = Utility.ModuleParameterGet(mtarget, pname);
                            if (mprop != null)
                            {
                                comparisonvalue = mprop.Value;
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
                    Module mod = _homegenie.Modules.Find(m => m.Address == c.Target && m.Domain == c.Domain);
                    parameter = mod.Properties.Find(delegate(ModuleParameter mp)
                    {
                        return mp.Name == c.Property;
                    });
                }
                //
                if (parameter != null)
                {
                    IComparable lvalue = parameter.Value;
                    IComparable rvalue = comparisonvalue;
                    //
                    double dval = 0;
                    DateTime dtval = new DateTime();
                    //
                    if (DateTime.TryParse(parameter.Value, out dtval))
                    {
                        lvalue = dtval;
                        rvalue = DateTime.Parse(comparisonvalue);
                    }
                    else if (double.TryParse(parameter.Value, out dval))
                    {
                        lvalue = dval;
                        rvalue = double.Parse(comparisonvalue);
                    }
                    //
                    int comparisonresult = lvalue.CompareTo(rvalue);
                    if (c.ComparisonOperator == ComparisonOperator.LessThan && comparisonresult < 0)
                    {
                        retval = true;
                    }
                    else if (c.ComparisonOperator == ComparisonOperator.Equals && comparisonresult == 0)
                    {
                        retval = true;
                    }
                    else if (c.ComparisonOperator == ComparisonOperator.GreaterThan && comparisonresult > 0)
                    {
                        retval = true;
                    }
                }
            }
            catch
            {
            }
            return retval;
        }

        private void _mcp_executeCommand(ProgramCommand c)
        {
            string wrequest = c.Domain + "/" + c.Target + "/" + c.CommandString + "/" + c.CommandArguments;
            MIGInterfaceCommand cmd = new MIGInterfaceCommand(wrequest);
            _homegenie.InterfaceControl(cmd);
            _homegenie.WaitOnPending(c.Domain);
        }

    }
}