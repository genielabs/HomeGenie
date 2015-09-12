using System;
using System.Collections.Generic;
using System.IO;
using HomeGenie.Automation.Scripting;
using HomeGenie.Service.Constants;
using System.Threading;

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
            homegenie.RaiseEvent(
                Domains.HomeAutomation_HomeGenie_Automation,
                programBlock.Address.ToString(),
                "Arduino Sketch Upload",
                "Arduino.UploadOutput",
                "Upload started"
            );
            string[] outputResult = ArduinoAppFactory.UploadSketch(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "programs",
                "arduino",
                programBlock.Address.ToString()
            )).Split('\n');
            //
            for (int x = 0; x < outputResult.Length; x++)
            {
                if (!String.IsNullOrWhiteSpace(outputResult[x]))
                {
                    homegenie.RaiseEvent(
                        Domains.HomeAutomation_HomeGenie_Automation,
                        programBlock.Address.ToString(),
                        "Arduino Sketch",
                        "Arduino.UploadOutput",
                        outputResult[x]
                    );
                    Thread.Sleep(500);
                }
            }
            //
            homegenie.RaiseEvent(
                Domains.HomeAutomation_HomeGenie_Automation,
                programBlock.Address.ToString(),
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

            // Generate, compile and upload Arduino Sketch
            string sketchFileName = ArduinoAppFactory.GetSketchFile(programBlock.Address.ToString());
            if (!Directory.Exists(Path.GetDirectoryName(sketchFileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(sketchFileName));
            }
            string sketchMakefile = Path.Combine(Path.GetDirectoryName(sketchFileName), "Makefile");

            try
            {
                // .ino source is stored in the ScriptSource property
                File.WriteAllText(sketchFileName, programBlock.ScriptSource);
                // Makefile source is stored in the ScriptCondition property
                File.WriteAllText(sketchMakefile, programBlock.ScriptCondition);
                errors = ArduinoAppFactory.CompileSketch(sketchFileName, sketchMakefile);
            }
            catch (Exception e)
            { 
                errors.Add(new ProgramError() {
                    Line = 0,
                    Column = 0,
                    ErrorMessage = "General failure: is 'arduino-mk' package installed?\n\n" + e.Message,
                    ErrorNumber = "500",
                    CodeBlock = "CR"
                });
            }

            return errors;
        }


    }
}

