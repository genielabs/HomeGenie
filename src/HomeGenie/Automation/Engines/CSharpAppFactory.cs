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
using System.IO;
using System.Linq;
using System.Reflection;
using HomeGenie.Service;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace HomeGenie.Automation.Engines
{
    public static class CSharpAppFactory
    {
        public const int ContextCodeOffset = 7;
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
            "System.Threading.Tasks",
            "System.Security.Cryptography",
            "System.Security.Cryptography.X509Certificates",
            "System.Device.Gpio",
            "System.Device.I2c",
            "Iot.Device",
            "Iot.Device.Common",
            "System.Text.Json",
            "JsonSerializer = System.Text.Json.JsonSerializer",
            // yolo
            "Compunet.YoloSharp",
            "Compunet.YoloSharp.Plotting",
            "YoloData = Compunet.YoloSharp.Data",
            // llama
            "LLama",
            "LLama.Abstractions",
            "LLama.Batched",
            "LLama.Common",
            "LLama.Exceptions",
            "LLama.Extensions",
            "LLama.Native",
            "LLama.Sampling",
            "LLama.Transformers",
            "LLamaSharp.KernelMemory",
            "LLamaSharp.SemanticKernel",
            "LLamaSharp.SemanticKernel.TextCompletion",
            "Microsoft.SemanticKernel",
            "Microsoft.Extensions.Options",
            "Microsoft.Extensions.Logging.Abstractions",
            "Newtonsoft.Json",
            "Newtonsoft.Json.Linq",
            "HomeGenie",
            "HomeGenie.Service",
            "HomeGenie.Service.Logging",
            "HomeGenie.Automation",
            "HomeGenie.Data",
            "NetClientLib",
            "OnvifDiscovery",
            "OnvifDiscovery.Models",
            "MIG",
            "MIG.Interfaces.HomeAutomation.Commons",
            "MessagePack",
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
            "SixLabors.ImageSharp",
            "SixLabors.ImageSharp.PixelFormats",
            "SixLabors.ImageSharp.Processing",
            "SixLabors.ImageSharp.Formats.Png",

/*
// Must be explicitly referenced with `#using` (prefer using .NET IoT)
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
*/

            "UnitsNet", // used both by Raspberry.IO and Microsoft.IoT
            "Utility = HomeGenie.Service.Utility",
            "YamlDotNet.Serialization",
            "YamlDotNet.Serialization.NamingConventions"
        };

        public static int ProgramCodeOffset => Includes.Count() + 12;

        public static EmitResult CompileScript(string scriptSetup, string scriptSource, string scriptContext, string outputDllFile)
        {
            var source = @"#pragma warning disable 0168 // variable declared but not used.
#pragma warning disable 0219 // variable assigned but not used.
#pragma warning disable 0414 // private field assigned but not used.
#pragma warning disable 0162 // Unreachable code detected

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
            var parsedCode = ParseCode(scriptSetup, scriptSource, scriptContext);
            source = source
                .Replace("{using}", parsedCode.UsingNamespaces)
                .Replace("{source}", parsedCode.MainCode)
                .Replace("{setup}", parsedCode.SetupCode)
                .Replace("{context}", parsedCode.ContextCode);

            var homeGenieDir = Path.GetDirectoryName(typeof(HomeGenieService).GetTypeInfo().Assembly.Location);

            var diagnosticOptions = new Dictionary<string, ReportDiagnostic>
            {
                { "CS1701", ReportDiagnostic.Suppress }
            };
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(diagnosticOptions);

            var assemblyPaths = new HashSet<string>();
            var basicAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in basicAssemblies)
            {
                // Aggiungiamo solo assembly "reali" (non dinamici) che hanno un percorso fisico
                if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                {
                    assemblyPaths.Add(assembly.Location);
                }
            }

            // Core librarires
            assemblyPaths.Add(typeof(object).Assembly.Location);
            assemblyPaths.Add(typeof(HomeGenieService).Assembly.Location);
            assemblyPaths.Add(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly.Location); // dynamic

            // Old libraries dotNetCoreDir
            assemblyPaths.Add(typeof(System.Net.Dns).Assembly.Location); // NameResolution
            assemblyPaths.Add(typeof(System.Net.HttpStatusCode).Assembly.Location); // Primitives
            assemblyPaths.Add(typeof(System.ComponentModel.Component).Assembly.Location); // ComponentModel.Primitives
            assemblyPaths.Add(typeof(System.Collections.ObjectModel.ObservableCollection<>).Assembly.Location); // ObjectModel

            // Data / Signal processing / Machine Learning / Computer Vision / ONXX
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.Extensions.Logging.Abstractions.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.Extensions.Options.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.ML.CpuMath.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.ML.Core.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.ML.Data.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.ML.DataView.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.ML.ImageAnalytics.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.ML.KMeansClustering.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.ML.OnnxRuntime.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.ML.OnnxTransformer.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.ML.PCA.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.ML.Probabilistic.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.ML.Probabilistic.Compiler.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.ML.Probabilistic.Learners.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.ML.Probabilistic.Learners.Classifier.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.ML.Probabilistic.Learners.Recommender.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.ML.StandardTrainers.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.ML.Transforms.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "NWaves.dll"));

            assemblyPaths.Add(Path.Combine(homeGenieDir, "SixLabors.ImageSharp.dll"));

            assemblyPaths.Add(Path.Combine(homeGenieDir, "YoloSharp.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "LLamaSharp.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "LLamaSharp.KernelMemory.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "LLamaSharp.SemanticKernel.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Microsoft.KernelMemory.Abstractions.dll"));

            // IO and Utility
            assemblyPaths.Add(Path.Combine(homeGenieDir, "MessagePack.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "MessagePack.Annotations.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "MIG.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "MIG.HomeAutomation.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "CM19Lib.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "LiteDB.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "GLabs.Logging.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Newtonsoft.Json.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "SerialPortLib.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "NetClientLib.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "UPnP.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "MQTTnet.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "OnvifDiscovery.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "YamlDotNet.dll"));

            // RaspberrySharp / Microsoft IoT Framework
            assemblyPaths.Add(Path.Combine(homeGenieDir, "System.Device.Gpio.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Iot.Device.Bindings.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "UnitsNet.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Raspberry.IO.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Raspberry.IO.Components.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Raspberry.IO.GeneralPurpose.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Raspberry.IO.InterIntegratedCircuit.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Raspberry.IO.SerialPeripheralInterface.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Raspberry.System.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Innovative.Geometry.Angle.dll"));
            assemblyPaths.Add(Path.Combine(homeGenieDir, "Innovative.SolarCalculator.dll"));

            var references = assemblyPaths.Select(path => MetadataReference.CreateFromFile(path)).ToList();
            var compilation = CSharpCompilation.Create("a")
                .WithOptions(compilationOptions)
                .AddReferences(references)
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
        }

        public static ParseCodeResult ParseCode(string scriptSetup, string scriptSource, string scriptContext)
        {
            var userIncludes = new List<string>();
            scriptSetup = GetIncludes(scriptSetup, ref userIncludes);
            scriptContext = GetIncludes(scriptContext, ref userIncludes);
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
