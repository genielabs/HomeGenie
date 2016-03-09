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
using System.Reflection;
using HomeGenie.Service;
using HomeGenie.Service.Constants;
using System.IO;
using HomeGenie.Automation;
using System.Collections.Generic;
using HomeGenie.Automation.Scripting;
using System.Diagnostics;

namespace HomeGenie.Automation.Engines
{
    public class CSharpEngine : ProgramEngineBase, IProgramEngine
    {
        // c# program fields
        private AppDomain programDomain = null;
        private Type assemblyType = null;
        private Object assembly = null;
        private MethodInfo methodRun = null;
        private MethodInfo methodReset = null;
        private MethodInfo methodEvaluateCondition = null;
        private System.Reflection.Assembly appAssembly;

        private static bool isShadowCopySet = false;

        public CSharpEngine(ProgramBlock pb) : base(pb) 
        {
            // TODO: SetShadowCopyPath/SetShadowCopyFiles methods are deprecated... 
            // TODO: create own AppDomain for "programDomain" instead of using CurrentDomain
            // TODO: and use AppDomainSetup to set shadow copy for each app domain
            // TODO: !!! verify AppDomain compatibility with mono !!!
            if (!isShadowCopySet)
            {
                isShadowCopySet = true;
                var domain = AppDomain.CurrentDomain;
                domain.SetShadowCopyPath(Path.Combine(domain.BaseDirectory, "programs"));
                domain.SetShadowCopyFiles();
            }
        }

        public void Unload()
        {
            Reset();
            programBlock.ActivationTime = null;
            programBlock.TriggerTime = null;
            if (programDomain != null)
            {
                // Unloading program app domain...
                try { AppDomain.Unload(programDomain); } catch { }
                programDomain = null;
            }
        }

        public bool Load()
        {
            bool success = AssemblyLoad();
            if (!success)
            {
                programBlock.ScriptErrors = "Program update is required.";
            }
            return success;
        }

        public List<ProgramError> Compile()
        {
            List<ProgramError> errors = new List<ProgramError>();

            // check for output directory
            if (!Directory.Exists(Path.GetDirectoryName(AssemblyFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AssemblyFile));
            }

            // dispose assembly and interrupt current task (if any)
            programBlock.IsEnabled = false;


            // clean up old assembly files
            try
            {
                if (File.Exists(this.AssemblyFile))
                {
                    File.Delete(this.AssemblyFile);
                }
                if (File.Exists(this.AssemblyFile + ".mdb"))
                {
                    File.Delete(this.AssemblyFile + ".mdb");
                }
                if (File.Exists(this.AssemblyFile.Replace(".dll", ".mdb")))
                {
                    File.Delete(this.AssemblyFile.Replace(".dll", ".mdb"));
                }
                if (File.Exists(this.AssemblyFile + ".pdb"))
                {
                    File.Delete(this.AssemblyFile + ".pdb");
                }
                if (File.Exists(this.AssemblyFile.Replace(".dll", ".pdb")))
                {
                    File.Delete(this.AssemblyFile.Replace(".dll", ".pdb"));
                }
            }
            catch (Exception ee)
            {
                HomeGenieService.LogError(ee);
            }


            // DO NOT CHANGE THE FOLLOWING LINES OF CODE
            // it is a lil' trick for mono compatibility
            // since it will be caching the assembly when using the same name
            // and use the old one instead of the new one
            string tmpfile = Path.Combine("programs", Guid.NewGuid().ToString() + ".dll");
            System.CodeDom.Compiler.CompilerResults result = new System.CodeDom.Compiler.CompilerResults(null);
            try
            {
                result = CSharpAppFactory.CompileScript(programBlock.ScriptCondition, programBlock.ScriptSource, tmpfile);
            }
            catch (Exception ex)
            {
                // report errors during post-compilation process
                result.Errors.Add(new System.CodeDom.Compiler.CompilerError(programBlock.Name, 0, 0, "-1", ex.Message));
            }

            if (result.Errors.Count > 0)
            {
                int sourceLines = programBlock.ScriptSource.Split('\n').Length;
                foreach (System.CodeDom.Compiler.CompilerError error in result.Errors)
                {
                    int errorRow = (error.Line - CSharpAppFactory.PROGRAM_CODE_OFFSET);
                    string blockType = "CR";
                    if (errorRow >= sourceLines + CSharpAppFactory.CONDITION_CODE_OFFSET)
                    {
                        errorRow -= (sourceLines + CSharpAppFactory.CONDITION_CODE_OFFSET);
                        blockType = "TC";
                    }
                    if (!error.IsWarning)
                    {
                        errors.Add(new ProgramError() {
                            Line = errorRow,
                            Column = error.Column,
                            ErrorMessage = error.ErrorText,
                            ErrorNumber = error.ErrorNumber,
                            CodeBlock = blockType
                        });
                    }
                    else
                    {
                        var warning = String.Format("{0},{1},{2}: {3}", blockType, errorRow, error.Column, error.ErrorText);
                        homegenie.ProgramManager.RaiseProgramModuleEvent(programBlock, Properties.CompilerWarning, warning);
                    }
                }
            }
            if (errors.Count == 0)
            {

                // move/copy new assembly files
                // rename temp file to production file
                appAssembly = result.CompiledAssembly;
                try
                {
                    //string tmpfile = new Uri(value.CodeBase).LocalPath;
                    File.Move(tmpfile, this.AssemblyFile);
                    if (File.Exists(tmpfile + ".mdb"))
                    {
                        File.Move(tmpfile + ".mdb", this.AssemblyFile + ".mdb");
                    }
                    if (File.Exists(tmpfile.Replace(".dll", ".mdb")))
                    {
                        File.Move(tmpfile.Replace(".dll", ".mdb"), this.AssemblyFile.Replace(".dll", ".mdb"));
                    }
                    if (File.Exists(tmpfile + ".pdb"))
                    {
                        File.Move(tmpfile + ".pdb", this.AssemblyFile + ".pdb");
                    }
                    if (File.Exists(tmpfile.Replace(".dll", ".pdb")))
                    {
                        File.Move(tmpfile.Replace(".dll", ".pdb"), this.AssemblyFile.Replace(".dll", ".pdb"));
                    }
                }
                catch (Exception ee)
                {
                    HomeGenieService.LogError(ee);
                }
            }

            return errors;
        }

        public MethodRunResult EvaluateCondition()
        {
            MethodRunResult result = null;
            if (appAssembly != null && CheckAppInstance())
            {
                result = (MethodRunResult)methodEvaluateCondition.Invoke(assembly, null);
                result.ReturnValue = (bool)result.ReturnValue || programBlock.WillRun;
            }
            return result;
        }

        public MethodRunResult Run(string options)
        {
            MethodRunResult result = null;
            if (appAssembly != null && CheckAppInstance())
            {
                result = (MethodRunResult)methodRun.Invoke(assembly, new object[1] { options });
            }
            return result;
        }

        public void Reset()
        {
            if (appAssembly != null && methodReset != null)
            {
                methodReset.Invoke(assembly, null);
            }
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
            var st = new StackTrace(e, true);
            error.Line = st.GetFrame(0).GetFileLineNumber();
            if (isTriggerBlock)
            {
                int sourceLines = programBlock.ScriptSource.Split('\n').Length;
                error.Line -=  (CSharpAppFactory.CONDITION_CODE_OFFSET + CSharpAppFactory.PROGRAM_CODE_OFFSET + sourceLines);
            }
            else
            {
                error.Line -=  CSharpAppFactory.PROGRAM_CODE_OFFSET;
            }
            return error;
        }


        internal string AssemblyFile
        {
            get
            {
                string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs");
                file = Path.Combine(file, programBlock.Address + ".dll");
                return file;
            }
        }

        internal bool AssemblyLoad()
        {
            bool succeed = false;
            if (programBlock.Type.ToLower() == "csharp")
            {
                try
                {
                    byte[] assemblyData = File.ReadAllBytes(this.AssemblyFile);
                    byte[] debugData = null;
                    if (File.Exists(this.AssemblyFile + ".mdb"))
                    {
                        debugData = File.ReadAllBytes(this.AssemblyFile + ".mdb");
                    }
                    else if (File.Exists(this.AssemblyFile + ".pdb"))
                    {
                        debugData = File.ReadAllBytes(this.AssemblyFile + ".pdb");
                    }
                    if (debugData != null)
                    {
                        appAssembly = Assembly.Load(assemblyData, debugData);
                    }
                    else
                    {
                        appAssembly = Assembly.Load(assemblyData);
                    }
                    succeed = true;
                }
                catch (Exception e)
                {

                    programBlock.ScriptErrors = e.Message + "\n" + e.StackTrace;
                }
            }
            return succeed;
        }

        private bool CheckAppInstance()
        {
            bool success = false;
            if (programDomain != null)
            {
                success = true;
            }
            else
            {
                try
                {
                    // Creating app domain
                    programDomain = AppDomain.CurrentDomain;

                    assemblyType = appAssembly.GetType("HomeGenie.Automation.Scripting.ScriptingInstance");
                    assembly = Activator.CreateInstance(assemblyType);

                    MethodInfo miSetHost = assemblyType.GetMethod("SetHost");
                    miSetHost.Invoke(assembly, new object[2] { homegenie, programBlock.Address });

                    methodRun = assemblyType.GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    // TODO: v1.1 !!!IMPORTANT!!! the method EvaluateCondition will be renamed to EvaluateStartupCode,
                    // TODO: v1.1 !!!IMPORTANT!!! so if EvaluateCondition is not found look for EvaluateStartupCode method instead
                    methodEvaluateCondition = assemblyType.GetMethod("EvaluateCondition", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    methodReset = assemblyType.GetMethod("Reset");

                    success = true;
                }
                catch (Exception ex)
                {
                    HomeGenieService.LogError(
                        Domains.HomeAutomation_HomeGenie_Automation,
                        programBlock.Address.ToString(),
                        ex.Message,
                        "Exception.StackTrace",
                        ex.StackTrace
                    );
                }
            }
            return success;
        }


    }
}

