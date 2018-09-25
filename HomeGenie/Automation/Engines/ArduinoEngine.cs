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
using System.IO;
using HomeGenie.Automation.Scripting;
using HomeGenie.Service.Constants;
using System.Threading;
using HomeGenie.Automation.Scheduler;

namespace HomeGenie.Automation.Engines
{
    public class ArduinoEngine  : ProgramEngineBase, IProgramEngine
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

        public MethodRunResult EvaluateCondition()
        {
            return null;
        }

        public MethodRunResult Run(string options)
        {
            var result = new MethodRunResult();
            result = new MethodRunResult();
            Homegenie.RaiseEvent(
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
            for (int x = 0; x < outputResult.Length; x++)
            {
                if (!String.IsNullOrWhiteSpace(outputResult[x]))
                {
                    Homegenie.RaiseEvent(
                        Domains.HomeGenie_System,
                        Domains.HomeAutomation_HomeGenie_Automation,
                        ProgramBlock.Address.ToString(),
                        "Arduino Sketch",
                        "Arduino.UploadOutput",
                        outputResult[x]
                    );
                    Thread.Sleep(500);
                }
            }
            //
            Homegenie.RaiseEvent(
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

        public ProgramError GetFormattedError(Exception e, bool isTriggerBlock)
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

        public List<ProgramError> Compile()
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
                // Makefile source is stored in the ScriptCondition property
                File.WriteAllText(sketchMakefile, ProgramBlock.ScriptCondition);
                errors = ArduinoAppFactory.CompileSketch(sketchFileName, sketchMakefile);
            }
            catch (Exception e)
            { 
                errors.Add(new ProgramError() {
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

