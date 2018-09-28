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

using Jint;
using Jint.Parser;

using HomeGenie.Automation.Scripting;

namespace HomeGenie.Automation.Engines
{
    public class JavascriptEngine : ProgramEngineBase, IProgramEngine
    {
        private Engine scriptEngine;
        private ScriptingHost hgScriptingHost;
        private string initScript = @"var $$ = {
          // ModulesManager
          modules: hg.modules,
          // SettingsHelper
          settings: hg.settings,
          // NetHelper
          net: hg.net,
          //  ProgramHelper
          program: hg.program,
          // EventsHelper
          on: hg.when,
          // SerialPortHelper
          serial: hg.serialPort,
          // TcpClientHelper
          tcp: hg.tcpClient,
          // UdpClientHelper
          udp: hg.udpClient, 
          // MqttClientHelper
          mqtt: hg.mqttClient,
          // KnxClientHelper
          knx: hg.knxClient,
          // SchedulerHelper
          scheduler: hg.scheduler,
          // Miscellaneous functions
          pause: function(seconds) { hg.pause(seconds); },
          delay: function(seconds) { this.pause(seconds); }
        }
        ";

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

            if (HomeGenie == null)
                return false;

            scriptEngine = new Engine();

            hgScriptingHost = new ScriptingHost();
            hgScriptingHost.SetHost(HomeGenie, ProgramBlock.Address);
            scriptEngine.SetValue("hg", hgScriptingHost);
            return true;
        }
        public override MethodRunResult EvaluateStartupCode()
        {
            MethodRunResult result = null;
            string jsScript = initScript + ProgramBlock.ScriptSetup;
            result = new MethodRunResult();
            try
            {
                var sh = (scriptEngine.GetValue("hg").ToObject() as ScriptingHost);
                scriptEngine.Execute(jsScript);
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
            var jsScript = initScript + ProgramBlock.ScriptSource;
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
            return error;
        }

        public override List<ProgramError> Compile()
        {
            List<ProgramError> errors = new List<ProgramError>();
            JavaScriptParser jp = new JavaScriptParser(false);
            //ParserOptions po = new ParserOptions();
            try
            {
                jp.Parse(ProgramBlock.ScriptSetup);
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
                            CodeBlock = CodeBlockEnum.TC
                        });
                    }
                }
            }
            //
            try
            {
                jp.Parse(ProgramBlock.ScriptSource);
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
                            CodeBlock = CodeBlockEnum.CR
                        });
                    }
                }
            }
            return errors;
        }

    }
}
