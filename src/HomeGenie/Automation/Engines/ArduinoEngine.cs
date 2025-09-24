/*
   Copyright 2012-2025 G-Labs (https://github.com/genielabs)

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: https://homegenie.it
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using HomeGenie.Automation.Scripting;
using HomeGenie.Service.Constants;

namespace HomeGenie.Automation.Engines
{
    public class ArduinoEngine : ProgramEngineBase, IProgramEngine
    {
        public ArduinoEngine(ProgramBlock pb) : base(pb)
        {
        }

        public void Unload()
        {
        }

        public bool Load()
        {
            return true;
        }

        public override MethodRunResult Setup()
        {
            return null;
        }

        public override MethodRunResult Run(string options)
        {
            var result = new MethodRunResult();
            HomeGenie.RaiseEvent(
                Domains.HomeGenie_System,
                Domains.HomeAutomation_HomeGenie_Automation,
                ProgramBlock.Address.ToString(),
                "Arduino Sketch Upload",
                "Arduino.UploadOutput",
                "Upload started"
            );
            string[] outputResult = ArduinoAppFactory.UploadSketch(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "programs",
                "arduino",
                ProgramBlock.Address.ToString()
            )).Split('\n');
            //
            foreach (var res in outputResult)
            {
                if (String.IsNullOrWhiteSpace(res)) continue;
                HomeGenie.RaiseEvent(
                    Domains.HomeGenie_System,
                    Domains.HomeAutomation_HomeGenie_Automation,
                    ProgramBlock.Address.ToString(),
                    "Arduino Sketch",
                    "Arduino.UploadOutput",
                    res
                );
                Thread.Sleep(500);
            }
            //
            HomeGenie.RaiseEvent(
                Domains.HomeGenie_System,
                Domains.HomeAutomation_HomeGenie_Automation,
                ProgramBlock.Address.ToString(),
                "Arduino Sketch",
                "Arduino.UploadOutput",
                "Upload finished"
            );
            return result;
        }

        public void Reset()
        {
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
            List<ProgramError> errors = new List<ProgramError>();

            // Generate, compile and upload Arduino Sketch
            string sketchFileName = ArduinoAppFactory.GetSketchFile(ProgramBlock.Address.ToString());
            if (!Directory.Exists(Path.GetDirectoryName(sketchFileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(sketchFileName));
            }
            string sketchMakefile = Path.Combine(Path.GetDirectoryName(sketchFileName), "Makefile");

            try
            {
                // .ino source is stored in the ScriptSource property
                File.WriteAllText(sketchFileName, ProgramBlock.ScriptSource);
                // Makefile source is stored in the ScriptSetup property
                File.WriteAllText(sketchMakefile, ProgramBlock.ScriptSetup);
                errors = ArduinoAppFactory.CompileSketch(sketchFileName, sketchMakefile);
            }
            catch (Exception e)
            {
                errors.Add(new ProgramError()
                {
                    Line = 0,
                    Column = 0,
                    ErrorMessage = "General failure: is 'arduino-mk' package installed?\n\n" + e.Message,
                    ErrorNumber = "500",
                    CodeBlock = CodeBlockEnum.CR
                });
            }

            return errors;
        }
    }
}
