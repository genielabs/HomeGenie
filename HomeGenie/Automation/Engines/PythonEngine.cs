using System;
using Microsoft.Scripting.Hosting;
using HomeGenie.Automation.Scripting;
using IronPython.Hosting;
using System.Collections.Generic;

namespace HomeGenie.Automation.Engines
{
    public class PythonEngine : ProgramEngineBase, IProgramEngine
    {
        internal ScriptEngine scriptEngine;
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

            if (homegenie == null)
                return false;

            scriptEngine = Python.CreateEngine();

            hgScriptingHost = new ScriptingHost();
            hgScriptingHost.SetHost(homegenie, programBlock.Address);
            dynamic scope = scriptScope = scriptEngine.CreateScope();
            scope.hg = hgScriptingHost;

            return true;
        }

        public MethodRunResult EvaluateCondition()
        {
            MethodRunResult result = null;
            string pythonScript = programBlock.ScriptCondition;
            result = new MethodRunResult();
            try
            {
                var sh = (scriptScope as dynamic).hg as ScriptingHost;
                if (!pythonScript.ToLower().Contains("hg.program.setup"))
                {
                    sh.Program.Setup(()=>{
                        scriptEngine.Execute(pythonScript, scriptScope);
                    });
                }
                else
                {
                    scriptEngine.Execute(pythonScript, scriptScope);
                }
                result.ReturnValue = sh.executeProgramCode || programBlock.Autostart;
            }
            catch (Exception e)
            {
                result.Exception = e;
            }
            return result;
        }

        public MethodRunResult Run(string options)
        {
            MethodRunResult result = null;
            string pythonScript = programBlock.ScriptSource;
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
            if (hgScriptingHost != null)
                hgScriptingHost.Reset();
        }

        public ProgramError GetFormattedError(Exception e, bool isTriggerBlock)
        {
            ProgramError error = new ProgramError() {
                CodeBlock = isTriggerBlock ? "TC" : "CR",
                Column = 0,
                Line = 0,
                ErrorNumber = "-1",
                ErrorMessage = e.Message
            };
            string[] message = ((ScriptEngine)scriptEngine).GetService<ExceptionOperations>().FormatException(e).Split(',');
            if (message.Length > 2)
            {
                int line = 0;
                int.TryParse(message[1].Substring(5), out line);
                error.Line = line;
            }
            return error;
        }

        public List<ProgramError> Compile()
        {
            List<ProgramError> errors = new List<ProgramError>();

            var source = scriptEngine.CreateScriptSourceFromString(programBlock.ScriptCondition);
            var errorListener = new ScriptEngineErrors("TC");
            source.Compile(errorListener);
            errors.AddRange(errorListener.Errors);
            errorListener = new ScriptEngineErrors("CR");
            source = scriptEngine.CreateScriptSourceFromString(programBlock.ScriptSource);
            source.Compile(errorListener);
            errors.AddRange(errorListener.Errors);

            return errors;
        }
    }
}

