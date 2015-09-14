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
using HomeGenie.Automation.Engines;

namespace HomeGenie.Automation
{

    [Serializable()]
    public class ProgramBlock
    {
        private IProgramEngine csScriptEngine;
        private bool isProgramEnabled;
        private string codeType = "";

        // event delegates
        public delegate void EnabledStateChangedEventHandler(object sender, bool isEnabled);
        public event EnabledStateChangedEventHandler EnabledStateChanged;

        // TODO: v1.1 !!!IMPORTANT!!! deprecate this and move to WizardEngine.cs
        // wizard script public members
        public ConditionType ConditionType { get; set; }
        public List<ProgramCondition> Conditions { get; set; }
        public List<ProgramCommand> Commands { get; set; }

        // TODO: v1.1 !!!IMPORTANT!!! refactor ScriptCondition to ScriptSetup
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
        public bool WillRun { get; set; }
        [XmlIgnore]
        public bool IsRunning { get; set; }
        [XmlIgnore]
        public bool LastConditionEvaluationResult { get; set; }

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
            Commands = new List<ProgramCommand>();
            Conditions = new List<ProgramCondition>();
            ConditionType = ConditionType.None;
            //
            isProgramEnabled = false;
            IsRunning = false;
        }

        public string Type
        {
            get { return codeType; }
            set
            {
                bool changed = codeType != value;
                codeType = value;
                if (changed || csScriptEngine == null)
                {
                    if (csScriptEngine != null)
                    {
                        csScriptEngine.Unload();
                        csScriptEngine = null;
                    }
                    switch (codeType.ToLower())
                    {
                    case "csharp":
                        csScriptEngine = new CSharpEngine(this);
                        break;
                    case "python":
                        csScriptEngine = new PythonEngine(this);
                        break;
                    case "ruby":
                        csScriptEngine = new RubyEngine(this);
                        break;
                    case "javascript":
                        csScriptEngine = new JavascriptEngine(this);
                        break;
                    case "wizard":
                        csScriptEngine = new WizardEngine(this);
                        break;
                    case "arduino":
                        csScriptEngine = new ArduinoEngine(this);
                        break;
                    }
                }
            }
        }

        public bool IsEnabled
        {
            get { return isProgramEnabled; }
            set
            {
                if (isProgramEnabled != value)
                {
                    isProgramEnabled = value;
                    LastConditionEvaluationResult = false;
                    if (isProgramEnabled)
                    {
                        ActivationTime = DateTime.UtcNow;
                        if (csScriptEngine != null)
                            csScriptEngine.Load();
                    }
                    else
                    {
                        if (csScriptEngine != null)
                            csScriptEngine.Unload();
                    }
                    if (EnabledStateChanged != null) EnabledStateChanged(this, value);
                }
            }
        }

        public ProgramEngineBase Engine
        {
            get { return (ProgramEngineBase)csScriptEngine; }
        }





        // TODO: v1.1 !!!IMPORTANT!!! move this region to a ProgramEngineBase.cs class
        #region ProgramBase methods

        internal List<ProgramError>  Compile()
        {
            return csScriptEngine.Compile();
        }

        internal MethodRunResult Run(string options)
        {
            return csScriptEngine.Run(options);
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
            // TODO: can it be null at this point???
            if (csScriptEngine != null)
                error = csScriptEngine.GetFormattedError(e, isTriggerBlock);
            return error;
        }

        // TODO: v1.1 !!!IMPORTANT!!! rename to EvaluateStartupCode
        internal MethodRunResult EvaluateCondition()
        {
            return csScriptEngine.EvaluateCondition();
        }

        #endregion




    }

    public class ProgramError
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorNumber { get; set; }
        public string CodeBlock { get; set; }
    }

}

