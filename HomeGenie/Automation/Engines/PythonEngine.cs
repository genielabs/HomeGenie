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

            return true;
        }

        public override MethodRunResult EvaluateStartupCode()
        {
            MethodRunResult result = null;
            string pythonScript = ProgramBlock.ScriptSetup;
            result = new MethodRunResult();
            try
            {
                var sh = (scriptScope as dynamic).hg as ScriptingHost;
                scriptEngine.Execute(pythonScript, scriptScope);
                result.ReturnValue = sh.ExecuteProgramCode || ProgramBlock.WillRun;
            }
            catch (Exception e)
            {
                result.Exception = e;
            }
            return result;
        }

        public override MethodRunResult Run(string options)
        {
            MethodRunResult result = null;
            string pythonScript = ProgramBlock.ScriptSource;
            result = new MethodRunResult();
            try
            {
                scriptEngine.Execute(pythonScript, scriptScope);
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

        public override ProgramError GetFormattedError(Exception e, bool isTriggerBlock)
        {
            ProgramError error = new ProgramError() {
                CodeBlock = isTriggerBlock ? CodeBlockEnum.TC : CodeBlockEnum.CR,
                Column = 0,
                Line = 0,
                ErrorNumber = "-1",
                ErrorMessage = e.Message
            };
            string[] message = scriptEngine.GetService<ExceptionOperations>().FormatException(e).Split(',');
            if (message.Length > 2)
            {
                int line = 0;
                int.TryParse(message[1].Substring(5), out line);
                error.Line = line;
            }
            return error;
        }

        public override List<ProgramError> Compile()
        {
            List<ProgramError> errors = new List<ProgramError>();

            var engine = Python.CreateEngine();
            var source = scriptEngine.CreateScriptSourceFromString(ProgramBlock.ScriptSetup);
            var errorListener = new ScriptEngineErrors(CodeBlockEnum.TC);
            source.Compile(errorListener);
            errors.AddRange(errorListener.Errors);
            errorListener = new ScriptEngineErrors(CodeBlockEnum.CR);
            source = scriptEngine.CreateScriptSourceFromString(ProgramBlock.ScriptSource);
            source.Compile(errorListener);
            errors.AddRange(errorListener.Errors);
            engine.Runtime.Shutdown();
            engine = null;

            return errors;
        }
    }
}
