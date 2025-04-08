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
 *     Project Homepage: https://homegenie.it
 */

using System;
using System.Collections.Generic;

using IronPython.Hosting;

using Microsoft.Scripting.Hosting;

using HomeGenie.Automation.Scripting;

namespace HomeGenie.Automation.Engines
{
    public class PythonEngine : ProgramEngineBase, IProgramEngine
    {
        private ScriptEngine scriptEngine;
        private ScriptScope scriptScope;
        private ScriptingHost hgScriptingHost;
        
        private int setupCodeLineOffset;
        private int mainCodeLineOffset;

        public PythonEngine(ProgramBlock pb) : base(pb)
        {
        }

        public void Unload()
        {
            if (scriptEngine != null)
            {
                Reset();
                scriptEngine.Runtime.Shutdown();
                scriptEngine = null;
            }
            hgScriptingHost = null;
            scriptScope = null;
        }

        public bool Load()
        {
            Unload();

            if (HomeGenie == null)
                return false;

            scriptEngine = Python.CreateEngine();

            hgScriptingHost = new ScriptingHost();
            hgScriptingHost.SetHost(HomeGenie, ProgramBlock.Address);
            dynamic scope = scriptScope = scriptEngine.CreateScope();
            scope.hg = hgScriptingHost;

            
            string script = "\ndef __setup__():\n";
            setupCodeLineOffset = script.Split('\n').Length - 1;
            foreach (var line in ProgramBlock.ScriptSetup.Split('\n'))
            {
                script += $"  {line}\n";
            }
            script += "  pass\n\n";
            script += "def __main__():\n";
            mainCodeLineOffset = script.Split('\n').Length - 1;
            foreach (var line in ProgramBlock.ScriptSource.Split('\n'))
            {
                script += $"  {line}\n";
            }
            script += "  pass\n\n";
            try
            {
                scriptEngine.Execute(script, scriptScope);
            }
            catch (Exception e)
            {
                // TODO: report errors
                Console.WriteLine(e.Message);
            }
            
            return true;
        }

        public override MethodRunResult Setup()
        {
            MethodRunResult result = null;
            result = new MethodRunResult();
            result.ReturnValue = false;
            try
            {
                if (scriptEngine == null) Load();
                scriptEngine.Execute("__setup__()", scriptScope);
                result.ReturnValue = ProgramBlock.WillRun;
            }
            catch (Exception e)
            {
                if (scriptEngine != null) result.Exception = e;
            }
            return result;
        }

        public override MethodRunResult Run(string options)
        {
            MethodRunResult result = null;
            result = new MethodRunResult();
            try
            {
                scriptEngine.Execute("__main__()", scriptScope);
            }
            catch (Exception e)
            {
                if (scriptEngine != null) result.Exception = e;
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
            if (scriptEngine != null)
            {
                string[] message = scriptEngine.GetService<ExceptionOperations>().FormatException(e).Split(',');
                if (message.Length > 3)
                {
                    int line;
                    Int32.TryParse(message[3].Substring(5), out line);
                    error.Line = line - (isSetupBlock ? setupCodeLineOffset : mainCodeLineOffset);
                }
            }
            return error;
        }

        public override List<ProgramError> Compile()
        {
            Load();
            ProgramBlock.ScriptErrors = "";
            string setupCode = String.IsNullOrEmpty(ProgramBlock.ScriptSetup) ? "" : ProgramBlock.ScriptSetup;
            string mainCode = String.IsNullOrEmpty(ProgramBlock.ScriptSource) ? "" : ProgramBlock.ScriptSource;
            List<ProgramError> errors = new List<ProgramError>();
            var engine = Python.CreateEngine();
            var source = scriptEngine.CreateScriptSourceFromString(setupCode);
            var errorListener = new ScriptEngineErrors(CodeBlockEnum.TC);
            source.Compile(errorListener);
            errors.AddRange(errorListener.Errors);
            errorListener = new ScriptEngineErrors(CodeBlockEnum.CR);
            source = scriptEngine.CreateScriptSourceFromString(mainCode);
            source.Compile(errorListener);
            errors.AddRange(errorListener.Errors);
            engine.Runtime.Shutdown();
            return errors;
        }
    }

    public class ScriptEngineErrors : ErrorListener
    {
        private readonly CodeBlockEnum blockType;
        public List<ProgramError> Errors = new List<ProgramError>();

        public ScriptEngineErrors(CodeBlockEnum type)
        {
            blockType = type;
        }

        public override void ErrorReported(ScriptSource source, string message, Microsoft.Scripting.SourceSpan span, int errorCode, Microsoft.Scripting.Severity severity)
        {
            Errors.Add(new ProgramError {
                Line = span.Start.Line,
                Column = span.Start.Column,
                ErrorMessage = message,
                ErrorNumber = errorCode.ToString(),
                CodeBlock = blockType
            });
        }
    }
}
