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
 *     Project Homepage: https://homegenie.it
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
#if NETCOREAPP
using System.Diagnostics;
using System.Dynamic;
using System.Net;
using System.Threading;
using HomeGenie.Service;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CSharp.RuntimeBinder;
#else
using Microsoft.CSharp;
using System.CodeDom.Compiler;
#endif

namespace HomeGenie.Automation.Engines
{
    public static class CSharpAppFactory
    {
        public const int ConditionCodeOffset = 7;

        // TODO: move this to a config file
        private static readonly List<string> Includes = new List<string>()
        {
            "System",
            "System.Text",
            "System.Globalization",
            "System.IO",
            "System.Linq",
            "System.Collections.Generic",
            "System.Dynamic",
            "System.Net",
            "System.Threading",
            "System.Security.Cryptography",
            "System.Security.Cryptography.X509Certificates",
#if NETCOREAPP
            "System.Device.Gpio",
            "System.Device.I2c",
            "Iot.Device",
            "Iot.Device.Common",
#endif
            "Newtonsoft.Json",
            "Newtonsoft.Json.Linq",
            "HomeGenie",
            "HomeGenie.Service",
            "HomeGenie.Service.Logging",
            "HomeGenie.Automation",
            "HomeGenie.Data",
            "NetClientLib",
            "OnvifDiscovery.Models",
            "MIG",
            "MIG.Interfaces.HomeAutomation.Commons",
            "CM19Lib", "X10 = CM19Lib.X10",
            "Innovative.Geometry",
            "Innovative.SolarCalculator",
            "LiteDB",
            "OpenSource.UPnP",
            "NWaves.Signals",
            "NWaves.Filters",
            "NWaves.Filters.Base",
            "NWaves.Operations",
            "NWaves.Utils",
#if !NETCOREAPP
            "Raspberry",
            "Raspberry.Timers",
            "Raspberry.IO",
            "Raspberry.IO.Components.Controllers.Pca9685",
            "Raspberry.IO.Components.Controllers.Tlc59711",
            "Raspberry.IO.Components.Converters.Mcp3002",
            "Raspberry.IO.Components.Converters.Mcp3008",
            "Raspberry.IO.Components.Converters.Mcp4822",
            "Raspberry.IO.Components.Displays.Hd44780",
            "Raspberry.IO.Components.Displays.Ssd1306",
            "Raspberry.IO.Components.Displays.Ssd1306.Fonts",
            "Raspberry.IO.Components.Displays.Sda5708",
            "Raspberry.IO.Components.Expanders.Mcp23017",
            "Raspberry.IO.Components.Expanders.Pcf8574",
            "Raspberry.IO.Components.Expanders.Mcp23008",
            "Raspberry.IO.Components.Leds.GroveBar",
            "Raspberry.IO.Components.Leds.GroveRgb",
            "Raspberry.IO.Components.Sensors",
            "Raspberry.IO.Components.Sensors.Distance.HcSr04",
            "Raspberry.IO.Components.Sensors.Pressure.Bmp085",
            "Raspberry.IO.Components.Sensors.Temperature.Dht",
            "Raspberry.IO.Components.Sensors.Temperature.Tmp36",
            "Raspberry.IO.Components.Devices.PiFaceDigital",
            "Raspberry.IO.GeneralPurpose",
            "Raspberry.IO.GeneralPurpose.Behaviors",
            "Raspberry.IO.GeneralPurpose.Configuration",
            "Raspberry.IO.InterIntegratedCircuit",
            "Raspberry.IO.SerialPeripheralInterface",
#endif
            "UnitsNet", // used both by Raspberry.IO and Microsoft.IoT 
            "Utility = HomeGenie.Service.Utility"
        };

        public static int ProgramCodeOffset => Includes.Count() + 11;

#if NETCOREAPP
        public static EmitResult CompileScript(string scriptSetup, string scriptSource, string outputDllFile)
#else
        public static CompilerResults CompileScript(string scriptSetup, string scriptSource, string outputDllFile)
#endif
        {
            var source = @"# pragma warning disable 0168 // variable declared but not used.
# pragma warning disable 0219 // variable assigned but not used.
# pragma warning disable 0414 // private field assigned but not used.

{using}

namespace HomeGenie.Automation.Scripting
{
    [Serializable]
    public class ScriptingInstance : ScriptingHost
    {
//////////////////////////////////////////////////////////////////
{context}
//////////////////////////////////////////////////////////////////

        private void RunCode(string PROGRAM_OPTIONS_STRING)
        {
//////////////////////////////////////////////////////////////////
{source}
//////////////////////////////////////////////////////////////////
        }

        #pragma warning disable 0162
        private bool SetupCode()
        {
//////////////////////////////////////////////////////////////////
{setup}
//////////////////////////////////////////////////////////////////
            return false;
        }
        #pragma warning restore 0162

        private HomeGenie.Automation.MethodRunResult Run(string PROGRAM_OPTIONS_STRING)
        {
            Exception ex = null;
            try
            {
                RunCode(PROGRAM_OPTIONS_STRING);
            }
            catch (Exception e)
            {
                ex = e;
            }
            return new HomeGenie.Automation.MethodRunResult(){ Exception = ex, ReturnValue = null };
        }

        private HomeGenie.Automation.MethodRunResult Setup()
        {
            Exception ex = null;
            bool retval = false;
            try
            {
                retval = SetupCode();
            }
            catch (Exception e)
            {
                ex = e;
            }
            return new HomeGenie.Automation.MethodRunResult(){ Exception = ex, ReturnValue = retval };
        }

        public ScriptingHost hg { get { return (ScriptingHost)this; } }
    }
}";
            var parsedCode = ParseCode(scriptSetup, scriptSource);
            source = source
                .Replace("{using}", parsedCode.UsingNamespaces)
                .Replace("{source}", parsedCode.MainCode)
                .Replace("{setup}", parsedCode.SetupCode)
                .Replace("{context}", parsedCode.ContextCode);
#if NETCOREAPP
            var dotNetCoreDir = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);
            var homeGenieDir = Path.GetDirectoryName(typeof(HomeGenieService).GetTypeInfo().Assembly.Location);
            var compilation = CSharpCompilation.Create("a")
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(

                    MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Enum).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Queryable).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Uri).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(HttpListener).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(DynamicObject).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.Runtime.dll")),
                    MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
                    MetadataReference.CreateFromFile(Assembly.Load("mscorlib").Location),
                    MetadataReference.CreateFromFile(typeof(Thread).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Stopwatch).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.Windows.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.Threading.Thread.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.Collections.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.Net.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.Net.Primitives.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.Net.NameResolution.dll")),
                    
                    // Microsoft IoT Framework
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "System.Device.Gpio.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "Iot.Device.Bindings.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "UnitsNet.dll")),
                    
                    // Data / Signal processing
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "NWaves.dll")),
                    
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "HomeGenie.dll")),

                    MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.Core.dll")),
                    MetadataReference.CreateFromFile(typeof(CSharpArgumentInfo).GetTypeInfo().Assembly.Location),

                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "MIG.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "MIG.HomeAutomation.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "CM19Lib.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "LiteDB.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "NLog.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "Newtonsoft.Json.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "SerialPortLib.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "NetClientLib.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "UPnP.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "MQTTnet.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "OnvifDiscovery.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "Raspberry.IO.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "Raspberry.IO.Components.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "Raspberry.IO.GeneralPurpose.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir,
                        "Raspberry.IO.InterIntegratedCircuit.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir,
                        "Raspberry.IO.SerialPeripheralInterface.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "Raspberry.System.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "Innovative.Geometry.Angle.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(homeGenieDir, "Innovative.SolarCalculator.dll"))
                )
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source));

            var assemblyPdbFile = outputDllFile + ".pdb";
            using var assemblyStream = File.Open(outputDllFile, FileMode.Create, FileAccess.ReadWrite);
            using var pdbStream = File.Open(assemblyPdbFile, FileMode.Create, FileAccess.ReadWrite);
            var opts = new EmitOptions()
                .WithPdbFilePath(assemblyPdbFile);
            var pdbStreamHelper = pdbStream;

            if (Environment.OSVersion.Platform == PlatformID.Unix)
                opts = opts.WithDebugInformationFormat(DebugInformationFormat.PortablePdb);

            var result = compilation.Emit(assemblyStream, pdbStreamHelper, options: opts);

            if (result.Success)
            {
                // TODO:
            }
            else
            {
                // TODO:
            }
            return result;
#else
            var providerOptions = new Dictionary<string, string>
            {
                //{ "CompilerVersion", "v4.0" }
            };
            var provider = new CSharpCodeProvider(providerOptions);
            var compilerParams = new CompilerParameters
            {
                GenerateInMemory = false,
                GenerateExecutable = false,
                IncludeDebugInformation = true,
                TreatWarningsAsErrors = false,
                OutputAssembly = outputDllFile
                // *** Useful for debugging
                //,TempFiles = new TempFileCollection {KeepFiles = true}
            };

            // Mono runtime 2/3 compatibility fix
            // TODO: this may not be required anymore
            var relocateSystemAsm = false;
            var type = Type.GetType("Mono.Runtime");
            if (type != null)
            {
                MethodInfo displayName = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (displayName != null)
                {
                    int major;
                    if (Int32.TryParse(displayName.Invoke(null, null).ToString().Substring(0, 1), out major) && major > 2)
                    {
                        relocateSystemAsm = true;
                    }
                }
            }
            if (!relocateSystemAsm)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    var assemblyName = assembly.GetName();
                    switch (assemblyName.Name.ToLower())
                    {
                        case "system":
                            compilerParams.ReferencedAssemblies.Add(assembly.Location);
                            break;
                        case "system.core":
                            compilerParams.ReferencedAssemblies.Add(assembly.Location);
                            break;
                        case "microsoft.csharp":
                            compilerParams.ReferencedAssemblies.Add(assembly.Location);
                            break;
                    }
                }
            }
            else
            {
                compilerParams.ReferencedAssemblies.Add("System.dll");
                compilerParams.ReferencedAssemblies.Add("System.Core.dll");
                compilerParams.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
            }

            compilerParams.ReferencedAssemblies.Add("HomeGenie.exe");
            compilerParams.ReferencedAssemblies.Add("MIG.dll");
            compilerParams.ReferencedAssemblies.Add("MIG.HomeAutomation.dll");
            compilerParams.ReferencedAssemblies.Add("CM19Lib.dll");
            compilerParams.ReferencedAssemblies.Add("LiteDB.dll");
            compilerParams.ReferencedAssemblies.Add("NLog.dll");
            compilerParams.ReferencedAssemblies.Add("Newtonsoft.Json.dll");

            compilerParams.ReferencedAssemblies.Add("SerialPortLib.dll");
            compilerParams.ReferencedAssemblies.Add("NetClientLib.dll");

            compilerParams.ReferencedAssemblies.Add("OnvifDiscovery.dll");

            compilerParams.ReferencedAssemblies.Add("UPnP.dll");

            compilerParams.ReferencedAssemblies.Add("MQTTnet.dll");
                    
            compilerParams.ReferencedAssemblies.Add("NWaves.dll");

            compilerParams.ReferencedAssemblies.Add("Raspberry.IO.dll");
            compilerParams.ReferencedAssemblies.Add("Raspberry.IO.Components.dll");
            compilerParams.ReferencedAssemblies.Add("Raspberry.IO.GeneralPurpose.dll");
            compilerParams.ReferencedAssemblies.Add("Raspberry.IO.InterIntegratedCircuit.dll");
            compilerParams.ReferencedAssemblies.Add("Raspberry.IO.SerialPeripheralInterface.dll");
            compilerParams.ReferencedAssemblies.Add("Raspberry.System.dll");
            compilerParams.ReferencedAssemblies.Add("UnitsNet.dll");

            compilerParams.ReferencedAssemblies.Add(Path.Combine("Innovative.Geometry.Angle.dll"));
            compilerParams.ReferencedAssemblies.Add(Path.Combine("Innovative.SolarCalculator.dll"));

            // compile and generate script assembly
            return provider.CompileAssemblyFromSource(compilerParams, source);
#endif
        }

        public static ParseCodeResult ParseCode(string scriptSetup, string scriptSource)
        {
            var userIncludes = new List<string>();
            var scriptContext = "";
            scriptSetup = GetIncludes(scriptSetup, ref userIncludes);
            scriptSetup = GetContext(scriptSetup, ref scriptContext);
            scriptSource = GetIncludes(scriptSource, ref userIncludes);
            var usingNs = String.Join(" ", Includes.Concat(userIncludes)
                .Select(x => String.Format("using {0};" + Environment.NewLine, x)));
            return new ParseCodeResult()
            {
                UsingNamespaces = usingNs,
                UserIncludes = userIncludes,
                ContextCode = scriptContext,
                MainCode = scriptSource,
                SetupCode = scriptSetup
            };
        }
        
        /**
         * Parse custom "using" pre-processor directive to allow including namespaces in HG programs
         */
        private static string GetIncludes(string codeBlock, ref List<string> userIncludes)
        {
            if (userIncludes == null) userIncludes = new List<string>();
            var codeBlockLines = codeBlock.Split('\n');
            codeBlock = "";
            foreach (var codeLine in codeBlockLines)
            {
                if (codeLine.StartsWith("#using "))
                {
                    userIncludes.Add(codeLine.Substring(7));
                    codeBlock += "//" + codeLine + "\n";
                }
                else
                {
                    codeBlock += codeLine + "\n";
                }
            }
            return codeBlock;
        }

        private static string GetContext(string codeBlock, ref string contextCode)
        {
            var codeBlockLines = codeBlock.Split('\n');
            codeBlock = "";
            bool contextOpen = false;
            int currentLine = 0;
            foreach (var codeLine in codeBlockLines)
            {
                if (contextOpen)
                {
                    if ((codeLine.Trim() + " ").StartsWith("#endregion "))
                    {
                        contextOpen = false;
                    }
                    contextCode += codeLine + "\n";
                }
                else if ((codeLine.Trim() + " ").StartsWith("#region program-context "))
                {
                    if (currentLine == 0)
                    {
                        contextOpen = true;
                    }
                    else
                    {
                        throw new Exception("Directive '#region program-context' must be on first line");
                    }
                    contextCode += codeLine + "\n";
                }
                else
                {
                    codeBlock += codeLine + "\n";
                }
                currentLine++;
            }
            if (contextOpen)
            {
                throw new Exception("Missing #endregion preprocessor directive");
            }
            return codeBlock;
        }

        public class ParseCodeResult
        {
            public string UsingNamespaces { get; set; }
            public string ContextCode { get; set; }
            public string MainCode { get; set; }
            public string SetupCode { get; set; }
            public List<string> UserIncludes { get; set; }
        }
    }
}
