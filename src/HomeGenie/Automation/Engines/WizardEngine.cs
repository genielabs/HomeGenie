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
using System.Globalization;
using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json;

using MIG;

using HomeGenie.Automation.Scripting;
using HomeGenie.Service.Constants;
using HomeGenie.Service;
using HomeGenie.Data;
using HomeGenie.Automation.Engines.WizardScript;
using Control = HomeGenie.Service.API.Automation.Control;

namespace HomeGenie.Automation.Engines
{
    public class WizardEngine : ProgramEngineBase, IProgramEngine
    {
        public class WizardScript
        {
            public List<ScriptCommand> Commands = new List<ScriptCommand>();
            public List<ScriptCondition> Conditions = new List<ScriptCondition>();
            public ConditionType ConditionType = ConditionType.None;
            public bool LastConditionEvaluationResult { get; set; }

            public WizardScript(ProgramBlock pb)
            {
                if (pb == null || String.IsNullOrEmpty(pb.ScriptSource)) return;
                try
                {
                    var s = JsonConvert.DeserializeObject<WizardScript>(pb.ScriptSource);
                    Commands = s.Commands;
                    Conditions = s.Conditions;
                    ConditionType = s.ConditionType;
                }
                catch (Exception e)
                {
                    // TODO: report initialization exception
                }
            }
        }

        public static void ConvertToVisualCode(HomeGenieService homegenie, ProgramBlock program)
        {
                const string visualCodeBlock = @"{{
                  ""blocks"": {{
                    ""languageVersion"": 0,
                    ""blocks"": [{0}{1}]
                  }}
                }}";
                const string setupBlock = @"{{
                  ""type"": ""setup_code"",
                  ""id"": ""{0}"",
                  ""inputs"": {{
                    ""SETUP"": {1}
                  }}
                }}";
                const string mainBlock = @"{{
                  ""type"": ""main_code"",
                  ""id"": ""{0}"",
                  ""inputs"": {{
                    ""MAIN"": {1}
                  }}
                }}";
                // TODO: convert old "wizard" type programs to "visual" (VPL)
                var script = new WizardScript(program);
                // Setup block
                // --> script.Conditions
                // --> script.ConditionType
                // Main block
                dynamic previousBlock = null;
                dynamic rootBlock = null;
                dynamic repeatBlock = null;
                string selectedModuleAddress = "";
                int id = 0;
                foreach (var command in script.Commands)
                {
                    dynamic commandBlock = new ExpandoObject();
                    dynamic block = new ExpandoObject();
                    commandBlock.block = block;
                    block.id = "C" + id++;
                    block.fields = new ExpandoObject();
                    if (command.Domain == Domains.HomeAutomation_HomeGenie && command.Target == "Automation")
                    {
                        switch (command.CommandString)
                        {
                            case "Program.Run":
                                block.type = "program_run";
                                string pid1 = command.CommandArguments;
                                var prg1 = homegenie.ProgramManager.Programs
                                    .Find(p => p.Address.ToString() == pid1 || p.Name == pid1);
                                block.fields.PROGRAM = prg1 != null ? prg1.Address.ToString(CultureInfo.InvariantCulture) : "";
                                break;
                            case "Program.WaitFor":
                                block.type = "program_wait_for";
                                string pid2 = command.CommandArguments;
                                var prg2 = homegenie.ProgramManager.Programs
                                    .Find(p => p.Address.ToString() == pid2 || p.Name == pid2);
                                block.fields.PROGRAM = prg2 != null ? prg2.Address.ToString(CultureInfo.InvariantCulture) : "";
                                break;
                            case "Program.Pause":
                                block.type = "program_pause";
                                double.TryParse(command.CommandArguments, out var seconds);
                                block.inputs = new ExpandoObject();
                                block.inputs.SECONDS = new ExpandoObject();
                                block.inputs.SECONDS.shadow = new ExpandoObject();
                                block.inputs.SECONDS.shadow.type = "math_number";
                                block.inputs.SECONDS.shadow.id = "I" + id++;
                                block.inputs.SECONDS.shadow.fields = new ExpandoObject();
                                block.inputs.SECONDS.shadow.fields.NUM = seconds;
                                break;
                            case "Program.Repeat":
                                block.type = "controls_repeat";
                                block.fields.TIMES = command.CommandArguments;
                                block.inputs = new ExpandoObject();
                                if (repeatBlock != null)
                                {
                                    block.inputs.DO = repeatBlock.block.next;
                                    previousBlock = repeatBlock;
                                }
                                else
                                {
                                    block.inputs.DO = rootBlock;
                                    previousBlock = null;
                                }
                                repeatBlock = commandBlock;
                                break;
                            case "Program.Play":
                                block.type = "program_play";
                                block.inputs = new ExpandoObject();
                                block.inputs.MEDIA_URL = new ExpandoObject();
                                block.inputs.MEDIA_URL.shadow = new ExpandoObject();
                                block.inputs.MEDIA_URL.shadow.type = "text";
                                block.inputs.MEDIA_URL.shadow.id = "I" + id++;
                                block.inputs.MEDIA_URL.shadow.fields = new ExpandoObject();
                                block.inputs.MEDIA_URL.shadow.fields.TEXT = command.CommandArguments;
                                break;
                            case "Program.Say":
                                block.type = "program_say";
                                block.inputs = new ExpandoObject();
                                block.inputs.SENTENCE = new ExpandoObject();
                                block.inputs.SENTENCE.shadow = new ExpandoObject();
                                block.inputs.SENTENCE.shadow.type = "text";
                                block.inputs.SENTENCE.shadow.id = "I" + id++;
                                block.inputs.SENTENCE.shadow.fields = new ExpandoObject();
                                block.inputs.SENTENCE.shadow.fields.TEXT = command.CommandArguments;
                                break;
                        }
                        if (previousBlock != null)
                        {
                            previousBlock.block.next = commandBlock;
                        }
                        else
                        {
                            rootBlock = commandBlock;
                        }
                    }
                    else
                    {
                        dynamic moduleSelectBlock = new ExpandoObject();
                        dynamic selectBlock = new ExpandoObject();
                        moduleSelectBlock.block = selectBlock;
                        selectBlock.type = "module_select";
                        selectBlock.id = "M" + id++;
                        selectBlock.fields = new ExpandoObject();
                        selectBlock.fields.MODULE = $"{command.Domain}:{command.Target}";
                        selectBlock.next = commandBlock;
                        switch (command.CommandString)
                        {
                            case Control.On:
                                block.type = "control_on";
                                break;
                            case Control.Off:
                                block.type = "control_off";
                                break;
                            case Control.Toggle:
                                block.type = "control_toggle";
                                break;
                            case Control.Level:
                                block.type = "control_level";
                                double.TryParse(command.CommandArguments, out var level);
                                block.fields.LEVEL = level;
                                break;
                            case Control.ColorHsb:
                                block.type = "control_color";
                                var hsb = command.CommandArguments.Split(',');
                                string htmlColor = "";
                                try
                                {
                                    htmlColor = Utility.HtmlColorFromHsb(
                                        double.Parse(hsb[0], CultureInfo.InvariantCulture),
                                        double.Parse(hsb[1], CultureInfo.InvariantCulture),
                                        double.Parse(hsb[2], CultureInfo.InvariantCulture)
                                    );
                                } catch { }
                                block.fields.COLOR = htmlColor;
                                double duration = 0.25;
                                if (hsb.Length > 3)
                                {
                                    double.TryParse(hsb[3], NumberStyles.Float, CultureInfo.InvariantCulture, out duration);
                                }
                                block.fields.DURATION = duration;
                                break;
                            // TODO: implement Thermostat commands
                            default:
                                block.type = "custom_command";
                                block.fields.COMMAND = command.CommandString;
                                block.fields.OPTIONS = command.CommandArguments;
                                break;
                        }
                        if (previousBlock != null)
                        {
                            previousBlock.block.next = selectedModuleAddress != selectBlock.fields.MODULE
                                ? moduleSelectBlock : commandBlock;
                        }
                        else
                        {
                            rootBlock = moduleSelectBlock;
                        }
                        selectedModuleAddress = selectBlock.fields.MODULE; 
                    }
                    previousBlock = commandBlock;
                }
                foreach (var condition in script.Conditions)
                {
                    // TODO: wizard script conditions currently not supported
                    /*
                     * - Scheduler.TimeEvent / Scheduler.CronEvent (same)
                     * - OR operator
                     * - Property value comparison against given value or other module's property
                     */
                }
                program.ScriptSetup = "";
                program.ScriptSource = "";
                program.Type = "visual";
                if (rootBlock != null)
                {
                    string jsonBlocks = String.Format(
                        visualCodeBlock,
                        "", // "String.Format(setupBlock, "B0", "\"\"") + ",",
                        String.Format(mainBlock, "B1", JsonConvert.SerializeObject(rootBlock))
                    );
                    program.Data = jsonBlocks;
                }
        }


        private ScriptingHost hgScriptingHost;
        private WizardScript script;

        public WizardEngine(ProgramBlock pb) : base(pb)
        {
            script = new WizardScript(pb);
        }

        public WizardScript Script
        {
            get { return script; }
        }

        public void Unload()
        {
            Reset();
            hgScriptingHost = null;
        }

        public bool Load()
        {
            if (HomeGenie == null)
                return false;

            script = new WizardScript(ProgramBlock);

            if (hgScriptingHost != null)
            {
                this.Reset();
                hgScriptingHost = null;
            }
            hgScriptingHost = new ScriptingHost();
            hgScriptingHost.SetHost(HomeGenie, ProgramBlock.Address);

            return true;
        }

        public override MethodRunResult Setup()
        {
            MethodRunResult result = null;
            result = new MethodRunResult();
            try
            {
                result.ReturnValue = EvaluateCondition(script.Conditions);
            }
            catch (Exception e)
            {
                result.Exception = e;
            }
            return result;
        }

        public override MethodRunResult Run(string options)
        {
            var result = new MethodRunResult();
            try
            {
                ExecuteScript(script.Commands);
            }
            catch (Exception e)
            {
                result.Exception = e;
            }
            return result;
        }

        public void Reset()
        {
            if (hgScriptingHost != null) hgScriptingHost.Reset();
        }

        public override ProgramError GetFormattedError(Exception e, bool isSetupBlock)
        {
            ProgramError error = new ProgramError()
            {
                CodeBlock = isSetupBlock ? CodeBlockEnum.TC : CodeBlockEnum.CR,
                Column = 0,
                Line = 0,
                ErrorNumber = "-1",
                ErrorMessage = e.Message
            };
            return error;
        }

        public override List<ProgramError> Compile()
        {
            var errors = new List<ProgramError>();
            if (!String.IsNullOrEmpty(ProgramBlock.ScriptSource))
            {
                try
                {
                    script = JsonConvert.DeserializeObject<WizardScript>(ProgramBlock.ScriptSource);
                }
                catch (Exception e)
                {
                    errors = new List<ProgramError>()
                    {
                        new ProgramError()
                        {
                            ErrorMessage = e.Message
                        }
                    };
                }
            }
            // initial condition evaluation status
            script.LastConditionEvaluationResult = false;
            return errors;
        }

        private bool EvaluateCondition(IReadOnlyList<ScriptCondition> conditions)
        {
            bool isConditionSatisfied = (conditions.Count > 0);
            for (int c = 0; c < conditions.Count; c++)
            {
                // check for OR logic operator
                if (conditions[c].ComparisonOperator == ComparisonOperator.LogicOrJoint)
                {
                    if (isConditionSatisfied)
                    {
                        break;
                    }
                    else
                    {
                        isConditionSatisfied = (c < conditions.Count - 1);
                        continue;
                    }
                }
                //
                bool res = false;
                try
                {
                    res = VerifyCondition(conditions[c]);
                }
                catch
                {
                    // TODO: report/handle exception
                }
                isConditionSatisfied = (isConditionSatisfied && res);
            }
            //
            bool lastResult = script.LastConditionEvaluationResult;
            script.LastConditionEvaluationResult = isConditionSatisfied;
            //
            if (script.ConditionType == ConditionType.OnSwitchTrue)
            {
                isConditionSatisfied = (isConditionSatisfied == true && isConditionSatisfied != lastResult);
            }
            else if (script.ConditionType == ConditionType.OnSwitchFalse)
            {
                isConditionSatisfied = (isConditionSatisfied == false && isConditionSatisfied != lastResult);
            }
            else if (script.ConditionType == ConditionType.OnTrue ||
                     script.ConditionType == ConditionType.Once)
            {
                // noop
            }
            else if (script.ConditionType == ConditionType.OnFalse)
            {
                isConditionSatisfied = !isConditionSatisfied;
            }
            return isConditionSatisfied;
        }

        private void ExecuteScript(IReadOnlyList<ScriptCommand> commands)
        {
            int repeatStartLine = 0;
            int repeatCount = 0;

            if (script.ConditionType == ConditionType.Once)
            {
                // execute only once
                ProgramBlock.IsEnabled = false;
            }

            for (int x = 0; x < commands.Count; x++)
            {
                if (commands[x].Domain == Domains.HomeAutomation_HomeGenie &&
                    commands[x].Target == SourceModule.Automation)
                {
                    string cs = commands[x].CommandString;
                    switch (cs)
                    {
                        case "Program.Pause":
                            Thread.Sleep((int) (double.Parse(commands[x].CommandArguments,
                                                    CultureInfo.InvariantCulture) * 1000));
                            break;
                        case "Program.Repeat":
                            // TODO: implement check for contiguous repeat statements
                            if (repeatCount <= 0)
                            {
                                repeatCount = (int) (double.Parse(commands[x].CommandArguments,
                                    CultureInfo.InvariantCulture));
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
                        case "Program.Run":
                            string programId = commands[x].CommandArguments;
                            var programToRun = HomeGenie.ProgramManager.Programs
                                .Find(p => p.Address.ToString() == programId || p.Name == programId);
                            if (programToRun != null /*&& programToRun.Address != program.Address*/
                                && !programToRun.IsRunning)
                            {
                                programToRun.Engine.StartProgram();
                            }
                            break;
                        case "Program.WaitFor":
                            hgScriptingHost.Program.WaitFor(commands[x].CommandArguments);
                            break;
                        case "Program.Play":
                            hgScriptingHost.Program.Play(commands[x].CommandArguments);
                            break;
                        case "Program.Say":
                            var language = "en-US";
                            var sentence = commands[x].CommandArguments;
                            int lidx = sentence.LastIndexOf("/");
                            if (lidx > 0)
                            {
                                language = sentence.Substring(lidx + 1);
                                sentence = sentence.Substring(0, lidx);
                            }
                            hgScriptingHost.Program.Say(sentence, language);
                            break;
                        /*
                        default:
                            var programCommand = commands[x];
                            string wrequest = programCommand.Domain + "/" + programCommand.Target + "/" +
                                              programCommand.CommandString + "/" +
                                              programCommand.CommandArguments;
                            HomeGenie.ExecuteAutomationRequest(new MigInterfaceCommand(wrequest));
                            break;*/ // deprecated - HG 1.4
                    }
                }
                else
                {
                    ExecuteCommand(commands[x]);
                }
                //
                Thread.Sleep(10);
            }
        }

        private bool VerifyCondition(ScriptCondition c)
        {
            bool returnValue = false;
            string comparisonValue = c.ComparisonValue;
            //
            if (c.Domain == Domains.HomeAutomation_HomeGenie &&
                (c.Target == SourceModule.Scheduler || c.Target == SourceModule.Automation) &&
                (c.Property == "Scheduler.TimeEvent" || c.Property == "Scheduler.CronEvent"))
            {
                return HomeGenie.ProgramManager.SchedulerService.IsScheduling(DateTime.Now, c.ComparisonValue);
            }
            //
            // if the comparison value starts with ":", then the value is read from another module property
            // eg: ":HomeAutomation.X10/B3/Status.Level"
            //
            if (comparisonValue.StartsWith(":"))
            {
                string[] propertyPath = comparisonValue.Substring(1).Split('/');
                comparisonValue = "";
                if (propertyPath.Length >= 3)
                {
                    string domain = propertyPath[0];
                    string address = propertyPath[1];
                    string propertyName = propertyPath[2];
                    var targetModule = HomeGenie.Modules.Find(m => m.Domain == domain && m.Address == address);
                    if (targetModule == null)
                    {
                        // try with abbreviated path then, eg: ":X10/B3/Status.Level"
                        targetModule =
                            HomeGenie.Modules.Find(m => m.Domain.EndsWith("." + domain) && m.Address == address);
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
            // the following "Programs.*" parameters are deprecated, just left for compatibility with HG < r340
            // Also the target SourceModule.Automation is deprecated and left for compatibility with HG < 499
            //
            ModuleParameter parameter = null;
            if (c.Domain == Domains.HomeAutomation_HomeGenie &&
                (c.Target == SourceModule.Scheduler || c.Target == SourceModule.Automation))
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
                        parameter.Value = ((int) DateTime.Now.DayOfWeek).ToString();
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
                    //default:
                    //    Module module = homegenie.Modules.Find(m => m.Address == c.Target && m.Domain == c.Domain);
                    //    if (module != null)
                    //        parameter = module.Properties.Find(mp => mp.Name == c.Property);
                    //    break;
                }
            }
            else
            {
                Module module = HomeGenie.Modules.Find(m => m.Address == c.Target && m.Domain == c.Domain);
                if (module != null)
                    parameter = module.Properties.Find(mp => mp.Name == c.Property);
            }

            if (parameter != null)
            {
                IComparable lvalue = parameter.Value;
                IComparable rvalue = comparisonValue;
                //
                double dval = 0;
                DateTime dtval = new DateTime();
                //
                if (double.TryParse(parameter.Value.Replace(",", "."),
                    NumberStyles.Float | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out dval))
                {
                    lvalue = dval;
                    rvalue = double.Parse(comparisonValue.Replace(",", "."),
                        NumberStyles.Float | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
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

        private void ExecuteCommand(ScriptCommand programCommand)
        {
            string command = programCommand.Domain + "/" + programCommand.Target + "/" + programCommand.CommandString +
                             "/" + System.Uri.EscapeDataString(programCommand.CommandArguments);
            var interfaceCommand = new MigInterfaceCommand(command);
            HomeGenie.InterfaceControl(interfaceCommand);
        }
    }
}
