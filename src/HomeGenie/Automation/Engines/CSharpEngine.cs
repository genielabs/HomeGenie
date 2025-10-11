/*
   Copyright 2012-2025 G-Labs (https://github.com/genielabs)

   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU Affero General Public License as
   published by the Free Software Foundation, either version 3 of the
   License, or (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU Affero General Public License for more details.

   You should have received a copy of the GNU Affero General Public License
   along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: https://homegenie.it
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using HomeGenie.Automation.Scripting;
using HomeGenie.Service;
using HomeGenie.Service.Constants;

#if NETCOREAPP
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
#endif

namespace HomeGenie.Automation.Engines
{
    public class CSharpEngine : ProgramEngineBase, IProgramEngine
    {
        // c# program fields
        private AppDomain _programDomain;
        private Type _assemblyType;
        private object _scriptInstance;
        private MethodInfo _methodRun;
        private MethodInfo _methodReset;
        private MethodInfo _methodSetup;
        private Assembly _scriptAssembly;

#if !NETCOREAPP
        private static bool _isShadowCopySet;
#endif

        public CSharpEngine(ProgramBlock pb) : base(pb)
        {
#if NETFRAMEWORK
            // This static flag prevents the setup from running more than once
            if (_isShadowCopySet) return;
            _isShadowCopySet = true;

            var domain = AppDomain.CurrentDomain;

            // This is required for Mono compatibility to allow runtime assembly recompilation.
            // It's obsolete, but necessary.
#pragma warning disable CS0618
            domain.SetShadowCopyPath(Path.Combine(domain.BaseDirectory, "programs"));
            domain.SetShadowCopyFiles();
#pragma warning restore CS0618
#endif
        }

        public bool Load()
        {
            var success = LoadAssembly();
            if (!success && String.IsNullOrEmpty(ProgramBlock.ScriptErrors))
            {
                ProgramBlock.ScriptErrors = "Program is not compiled.";
            }
            return success;
        }

        public void Unload()
        {
            Reset();
            if (_programDomain == null) return;
            // Unloading program app domain...
            try
            {
                AppDomain.Unload(_programDomain);
            }
            catch
            {
                // ignored
            }
            _programDomain = null;
        }

        public override List<ProgramError> Compile()
        {
            var errors = new List<ProgramError>();

            // check for output directory
            if (!Directory.Exists(Path.GetDirectoryName(AssemblyFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AssemblyFile));
            }

            // dispose assembly and interrupt current task (if any)
            bool wasEnabled = ProgramBlock.IsEnabled;
            ProgramBlock.ScriptErrors = "";
            ProgramBlock.IsEnabled = false;

            HomeGenie.ProgramManager.RaiseProgramModuleEvent(ProgramBlock, Properties.ProgramStatus,
                "Compile");

            CleanupFiles();

            // DO NOT CHANGE THE FOLLOWING LINES OF CODE
            // it is a lil' trick for mono compatibility
            // since it will be caching the assembly when using the same name
            // and use the old one instead of the new one
            var tempFile = Path.Combine("programs", Guid.NewGuid() + ".dll");
#if NETCOREAPP
            EmitResult result = null;
            try
            {
                result = CSharpAppFactory.CompileScript(ProgramBlock.ScriptSetup, ProgramBlock.ScriptSource, ProgramBlock.ScriptContext, tempFile);
            }
            catch (Exception ex)
            {
                // report unexpected error during compilation process
                errors.Add(new ProgramError
                {
                    Line = 0,
                    Column = 0,
                    EndLine = 0,
                    EndColumn = 0,
                    ErrorMessage = ex.Message,
                    ErrorNumber = ex.Source,
                    CodeBlock = CodeBlockEnum.PC
                });
            }

            if (result != null && !result.Success)
            {
                var parsedCode = CSharpAppFactory.ParseCode(ProgramBlock.ScriptSetup, ProgramBlock.ScriptSource, ProgramBlock.ScriptContext);
                var contextLines = parsedCode.ContextCode.Split('\n').Length;
                var sourceLines = parsedCode.MainCode.Split('\n').Length;
                foreach (var diagnostic in result.Diagnostics)
                {
                    var errorRow = (diagnostic.Location.GetLineSpan().StartLinePosition.Line - CSharpAppFactory.ProgramCodeOffset) - parsedCode.UserIncludes.Count;
                    var errorEndRow = (diagnostic.Location.GetLineSpan().EndLinePosition.Line - CSharpAppFactory.ProgramCodeOffset) - parsedCode.UserIncludes.Count;
                    var errorCol = diagnostic.Location.GetLineSpan().StartLinePosition.Character + 1;
                    var errorEndCol = diagnostic.Location.GetLineSpan().EndLinePosition.Character + 1;
                    if (errorRow <= 0)
                    {
                        errors.Add(new ProgramError
                        {
                            Line = 0,
                            Column = 0,
                            EndLine = 0,
                            EndColumn = 0,
                            ErrorMessage = diagnostic.GetMessage(),
                            ErrorNumber = diagnostic.Descriptor.Id,
                            CodeBlock = CodeBlockEnum.PC
                        });
                        continue;
                    }
                    var blockType = CodeBlockEnum.CR;
                    if (errorRow <= contextLines)
                    {
                        blockType = CodeBlockEnum.PC;
                    }
                    else
                    {
                        errorRow -= (contextLines - 1 + 6);
                        errorEndRow -= (contextLines - 1 + 6);
                        if (errorRow >= sourceLines + CSharpAppFactory.ConditionCodeOffset)
                        {
                            errorRow -= (sourceLines + CSharpAppFactory.ConditionCodeOffset);
                            errorEndRow -= (sourceLines + CSharpAppFactory.ConditionCodeOffset);
                            blockType = CodeBlockEnum.TC;
                        }
                        //if (contextLines > 0)
                        //{
                        //    errorRow -= contextLines - 1;
                        //    errorEndRow -= contextLines - 1;
                        //}
                    }
                    if (diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        errors.Add(new ProgramError
                        {
                            Line = errorRow,
                            Column = errorCol,
                            EndLine = errorEndRow,
                            EndColumn = errorEndCol,
                            ErrorMessage = diagnostic.GetMessage(),
                            ErrorNumber = diagnostic.Descriptor.Id,
                            CodeBlock = blockType
                        });
                    }
                    else
                    {
                        var warning = String.Format("{0},{1},{2}: {3}", blockType, errorRow, errorCol,
                            diagnostic.GetMessage());
                        HomeGenie.ProgramManager.RaiseProgramModuleEvent(ProgramBlock, Properties.CompilerWarning,
                            warning);
                    }
                }
            }
#else
            var result = new System.CodeDom.Compiler.CompilerResults(null);
            try
            {
                result = CSharpAppFactory.CompileScript(ProgramBlock.ScriptSetup, ProgramBlock.ScriptSource, ProgramBlock.ScriptContext, tempFile);
            }
            catch (Exception ex)
            {
                // report errors during post-compilation process
                result.Errors.Add(new System.CodeDom.Compiler.CompilerError(ProgramBlock.Name, 0, 0, "-1", ex.Message));
            }

            if (result.Errors.Count > 0)
            {
                var parsedCode = CSharpAppFactory.ParseCode(ProgramBlock.ScriptSetup, ProgramBlock.ScriptSource, ProgramBlock.ScriptContext);
                var contextLines = parsedCode.ContextCode.Split('\n').Length;
                var sourceLines = parsedCode.MainCode.Split('\n').Length;
                foreach (System.CodeDom.Compiler.CompilerError error in result.Errors)
                {
                    var errorRow = (error.Line - CSharpAppFactory.ProgramCodeOffset) - parsedCode.UserIncludes.Count - 1;
                    var errorCol = error.Column + 1;
                    if (errorRow <= 0)
                    {
                        errors.Add(new ProgramError
                        {
                            Line = 0,
                            EndLine = 0,
                            Column = 0,
                            EndColumn = 0,
                            ErrorMessage = error.ErrorText,
                            ErrorNumber = error.ErrorNumber,
                            CodeBlock = CodeBlockEnum.PC
                        });
                        continue;
                    }
                    var blockType = CodeBlockEnum.CR;
                    if (errorRow <= contextLines)
                    {
                        blockType = CodeBlockEnum.PC;
                    }
                    else
                    {
                        errorRow -= (contextLines - 1 + 6);
                        if (errorRow >= sourceLines + CSharpAppFactory.ConditionCodeOffset)
                        {
                            errorRow -= (sourceLines + CSharpAppFactory.ConditionCodeOffset);
                            blockType = CodeBlockEnum.TC;
                        }
                        //if (contextLines > 0)
                        //{
                        //    errorRow -= contextLines - 1;
                        //    errorEndRow -= contextLines - 1;
                        //}
                    }
                    if (!error.IsWarning)
                    {
                        errors.Add(new ProgramError
                        {
                            Line = errorRow,
                            EndLine = errorRow,
                            Column = errorCol,
                            EndColumn = error.Column + 1,
                            ErrorMessage = error.ErrorText,
                            ErrorNumber = error.ErrorNumber,
                            CodeBlock = blockType
                        });
                    }
                    else
                    {
                        var warning = String.Format("{0},{1},{2}: {3}", blockType, errorRow, errorCol,
                            error.ErrorText);
                        HomeGenie.ProgramManager.RaiseProgramModuleEvent(ProgramBlock, Properties.CompilerWarning,
                            warning);
                    }
                }
            }
#endif

            if (errors.Count != 0)
                return errors;

            // move/copy new assembly files
            // rename temp file to production file
#if !NETCOREAPP
            _scriptAssembly = result.CompiledAssembly;
#endif
            try
            {
                //string tmpfile = new Uri(value.CodeBase).LocalPath;
                File.Move(tempFile, this.AssemblyFile);
                if (File.Exists(tempFile + ".mdb"))
                {
                    File.Move(tempFile + ".mdb", this.AssemblyFile + ".mdb");
                }
                if (File.Exists(tempFile.Replace(".dll", ".mdb")))
                {
                    File.Move(tempFile.Replace(".dll", ".mdb"), this.AssemblyFile.Replace(".dll", ".mdb"));
                }
                if (File.Exists(tempFile + ".pdb"))
                {
                    File.Move(tempFile + ".pdb", this.AssemblyFile + ".pdb");
                }
                if (File.Exists(tempFile.Replace(".dll", ".pdb")))
                {
                    File.Move(tempFile.Replace(".dll", ".pdb"), this.AssemblyFile.Replace(".dll", ".pdb"));
                }
            }
            catch (Exception ee)
            {
                HomeGenieService.LogError(ee);
            }

            if (errors.Count == 0 && wasEnabled)
            {
                ProgramBlock.IsEnabled = true;
            }

            return errors;
        }

        public override MethodRunResult Setup()
        {
            MethodRunResult result = null;
            if (_scriptAssembly != null && CheckAppInstance())
            {
                result = (MethodRunResult) _methodSetup.Invoke(_scriptInstance, null);
                result.ReturnValue = (bool) result.ReturnValue || ProgramBlock.WillRun;
            }
            return result;
        }

        public override MethodRunResult Run(string options)
        {
            MethodRunResult result = null;
            if (_scriptAssembly != null && CheckAppInstance())
            {
                result = (MethodRunResult) _methodRun.Invoke(_scriptInstance, new object[1] {options});
            }
            return result;
        }

        public void Reset()
        {
            if (_scriptAssembly != null && _methodReset != null)
            {
                try
                {
                    _methodReset.Invoke(_scriptInstance, null);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
//                    throw;
                }
            }
        }

        public void CleanupFiles()
        {
            // clean up old assembly files
            try
            {
                // If the file to be deleted does not exist, no exception is thrown.
                File.Delete(AssemblyFile);
                File.Delete(AssemblyFile + ".mdb");
                File.Delete(AssemblyFile.Replace(".dll", ".mdb"));
                File.Delete(AssemblyFile + ".pdb");
                File.Delete(AssemblyFile.Replace(".dll", ".pdb"));
            }
            catch (Exception ex)
            {
                HomeGenieService.LogError(ex);
            }
        }

        public override ProgramError GetFormattedError(Exception e, bool isSetupBlock)
        {
            var error = new ProgramError()
            {
                CodeBlock = isSetupBlock ? CodeBlockEnum.TC : CodeBlockEnum.CR,
                Column = 0,
                Line = 0,
                ErrorNumber = "-1",
                ErrorMessage = e.Message
            };
            // determine error line number
            var st = new StackTrace(e, true);
            var stackFrames = st.GetFrames();
            if (stackFrames != null && stackFrames.Length > 0)
            {
                error.Line = stackFrames[0].GetFileLineNumber();
                foreach (var frame in stackFrames)
                {
                    var declaringType = frame.GetMethod()?.DeclaringType;
                    if (declaringType != null)
                    {
                        if (declaringType.FullName != null && declaringType.FullName.EndsWith("HomeGenie.Automation.Scripting.ScriptingInstance"))
                        {
                            error.Line = frame.GetFileLineNumber();
                            break;
                        }
                    }
                }
            }
            if (isSetupBlock)
            {
                var sourceLines = ProgramBlock.ScriptSource.Split('\n').Length;
                error.Line -= (CSharpAppFactory.ConditionCodeOffset + CSharpAppFactory.ProgramCodeOffset + sourceLines);
            }
            else
            {
                error.Line -= CSharpAppFactory.ProgramCodeOffset;
            }
            return error;
        }

        private string AssemblyFile
        {
            get
            {
                var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs");
                file = Path.Combine(file, ProgramBlock.Address + ".dll");
                return file;
            }
        }

        private bool LoadAssembly()
        {
            try
            {
                var assemblyData = File.ReadAllBytes(this.AssemblyFile);
                byte[] debugData = null;
                if (File.Exists(this.AssemblyFile + ".mdb"))
                {
                    debugData = File.ReadAllBytes(this.AssemblyFile + ".mdb");
                }
                else if (File.Exists(this.AssemblyFile + ".pdb"))
                {
                    debugData = File.ReadAllBytes(this.AssemblyFile + ".pdb");
                }
                _scriptAssembly = debugData != null
                    ? Assembly.Load(assemblyData, debugData)
                    : Assembly.Load(assemblyData);
                return true;
            }
            catch (Exception e)
            {
                ProgramBlock.ScriptErrors = e.Message + "\n" + e.StackTrace;
                return false;
            }
        }

        private bool CheckAppInstance()
        {
            var success = false;
            if (_programDomain != null)
            {
                success = true;
            }
            else
            {
                try
                {
                    // Creating app domain
                    _programDomain = AppDomain.CurrentDomain;

                    _assemblyType = _scriptAssembly.GetType("HomeGenie.Automation.Scripting.ScriptingInstance");
                    _scriptInstance = Activator.CreateInstance(_assemblyType);

                    var miSetHost = _assemblyType.GetMethod("SetHost");
                    miSetHost.Invoke(_scriptInstance, new object[2] {HomeGenie, ProgramBlock.Address});

                    _methodRun = _assemblyType.GetMethod("Run",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    _methodSetup = _assemblyType.GetMethod("Setup",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    _methodReset = _assemblyType.GetMethod("Reset");

                    success = true;
                }
                catch (Exception ex)
                {
                    HomeGenieService.LogError(
                        Domains.HomeAutomation_HomeGenie_Automation,
                        ProgramBlock.Address.ToString(),
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
