﻿/*
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
using Esprima;
using Jint;

using HomeGenie.Automation.Scripting;
using Jint.Runtime;

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
        
        private int setupCodeLineOffset;
        private int mainCodeLineOffset;

        public JavascriptEngine(ProgramBlock pb) : base(pb)
        {
        }

        public void Unload()
        {
            Reset();
            scriptEngine?.Dispose();
            scriptEngine = null;
            hgScriptingHost = null;
        }

        public bool Load()
        {
            Unload();

            if (HomeGenie == null)
                return false;

            scriptEngine = new Engine(options => options.AllowClr());

            hgScriptingHost = new ScriptingHost();
            hgScriptingHost.SetHost(HomeGenie, ProgramBlock.Address);
            scriptEngine.SetValue("hg", hgScriptingHost);
            string script = initScript + "\nfunction __setup__() {\n";
            setupCodeLineOffset = script.Split('\n').Length - 1; 
            script += ProgramBlock.ScriptSetup + "\n}\n";
            script += "function __main__() {\n";
            mainCodeLineOffset = script.Split('\n').Length - 1;
            script += ProgramBlock.ScriptSource + "\n}\n";
            try
            {
                scriptEngine.Execute(script);
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
            try
            {
                var sh = (scriptEngine.GetValue("hg").ToObject() as ScriptingHost);
                scriptEngine.Execute("__setup__();");
                result.ReturnValue = ProgramBlock.WillRun;
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
            //scriptEngine.Options.AllowClr(false);
            result = new MethodRunResult();
            try
            {
                scriptEngine.Execute("__main__();");
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
            ProgramError error = new ProgramError();
            try
            {
                error = new ProgramError()
                {
                    CodeBlock = isSetupBlock ? CodeBlockEnum.TC : CodeBlockEnum.CR,
                    Column = (e as JavaScriptException).Location.Start.Column,
                    Line = (e as JavaScriptException).Location.Start.Line -
                           (isSetupBlock ? setupCodeLineOffset : mainCodeLineOffset),
                    ErrorNumber = "-1",
                    ErrorMessage = e.Message
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(e.Message + " -- " + ex.Message);
            }

            return error;
        }

        public override List<ProgramError> Compile()
        {
            List<ProgramError> errors = new List<ProgramError>();
            JavaScriptParser jp = new JavaScriptParser();
            //ParserOptions po = new ParserOptions();
            // Setup code
            try
            {
                jp.ParseScript(ProgramBlock.ScriptSetup);
            }
            catch (ParserException e)
            {
                errors.Add(new ProgramError()
                {
                    Line = e.LineNumber,
                    ErrorMessage = e.Message,
                    CodeBlock = CodeBlockEnum.TC
                });
            }
            // Main code
            try
            {
                jp.ParseScript(ProgramBlock.ScriptSource);
            }
            catch (ParserException e)
            {
                errors.Add(new ProgramError()
                {
                    Line = e.LineNumber,
                    ErrorMessage = e.Message,
                    CodeBlock = CodeBlockEnum.CR
                });
            }
            return errors;
        }
    }
}
