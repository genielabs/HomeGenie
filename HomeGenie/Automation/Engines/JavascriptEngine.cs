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
using HomeGenie.Automation.Scripting;

using Jint;
using System.Collections.Generic;
using Jint.Parser;

namespace HomeGenie.Automation.Engines
{
    public class JavascriptEngine : ProgramEngineBase, IProgramEngine
    {
        internal Engine scriptEngine;
        private ScriptingHost hgScriptingHost;

        public JavascriptEngine(ProgramBlock pb) : base(pb)
        {
        }

        public void Unload()
        {
            Reset();
            scriptEngine = null;
            hgScriptingHost = null;
        }

        public bool Load()
        {
            Unload();

            if (homegenie == null)
                return false;

            scriptEngine = new Engine();

            hgScriptingHost = new ScriptingHost();
            hgScriptingHost.SetHost(homegenie, programBlock.Address);
            scriptEngine.SetValue("hg", hgScriptingHost);

            return true;
        }

        public MethodRunResult EvaluateCondition()
        {
            MethodRunResult result = null;
            string jsScript = programBlock.ScriptCondition;
            result = new MethodRunResult();
            try
            {
                var sh = (scriptEngine.GetValue("hg").ToObject() as ScriptingHost);
                scriptEngine.Execute(jsScript);
                result.ReturnValue = sh.executeProgramCode || programBlock.WillRun;
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
            string jsScript = programBlock.ScriptSource;
            //scriptEngine.Options.AllowClr(false);
            result = new MethodRunResult();
            try
            {
                scriptEngine.Execute(jsScript);
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

            return error;
        }

        public List<ProgramError> Compile()
        {
            List<ProgramError> errors = new List<ProgramError>();

            JavaScriptParser jp = new JavaScriptParser(false);
            //ParserOptions po = new ParserOptions();
            try
            {
                jp.Parse(programBlock.ScriptCondition);
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
                jp.Parse(programBlock.ScriptSource);
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

    }
}

