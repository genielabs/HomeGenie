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
using System.Reflection;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;

using Microsoft.CSharp;

namespace HomeGenie.Automation.Engines
{
    public static class CSharpAppFactory
    {
        public const int ConditionCodeOffset = 8;

        public static int ProgramCodeOffset
        {
            get { return Includes.Count() + 15; }
        }

        static readonly string[] Includes =
        {
            "System",
            "System.Text",
            "System.Globalization",
            "System.Linq",
            "System.Collections.Generic",
            "System.Dynamic",
            "System.Net",
            "System.Threading",
            "Newtonsoft.Json",
            "Newtonsoft.Json.Linq",
            "HomeGenie",
            "HomeGenie.Service",
            "HomeGenie.Service.Logging",
            "HomeGenie.Automation",
            "HomeGenie.Data",
            "MIG",
            "Innovative.Geometry",
            "Innovative.SolarCalculator",
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
        };

        public static CompilerResults CompileScript(string conditionSource, string scriptSource, string outputDllFile)
        {
            var source = @"# pragma warning disable 0168 // variable declared but not used.
# pragma warning disable 0219 // variable assigned but not used.
# pragma warning disable 0414 // private field assigned but not used.

{usings}

namespace HomeGenie.Automation.Scripting
{
    [Serializable]
    public class ScriptingInstance : ScriptingHost
    {
        private void RunCode(string PROGRAM_OPTIONS_STRING)
        {
//////////////////////////////////////////////////////////////////
// NOTE: user code start line is 16 *** please add new code after this method, do not alter start line! ***
{statement}
//////////////////////////////////////////////////////////////////
        }

        #pragma warning disable 0162
        private bool EvaluateConditionBlock()
        {
//////////////////////////////////////////////////////////////////
// NOTE: user code start line is ??? *** please add new code after this method, do not alter start line! ***
{condition}
//////////////////////////////////////////////////////////////////
            return false;
        }
        #pragma warning restore 0162

        private MethodRunResult Run(string PROGRAM_OPTIONS_STRING)
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
            return new MethodRunResult(){ Exception = ex, ReturnValue = null };
        }

        private MethodRunResult EvaluateCondition()
        {
            Exception ex = null;
            bool retval = false;
            try
            {
                    retval = EvaluateConditionBlock();
            }
            catch (Exception e)
            {
                ex = e;
            }
            return new MethodRunResult(){ Exception = ex, ReturnValue = retval };
        }

        public ScriptingHost hg { get { return (ScriptingHost)this; } }
    }
}";
            var usings = string.Join(" ", Includes.Select(x => string.Format("using {0};" + Environment.NewLine, x)));
            source = source
                .Replace("{usings}", usings)
                .Replace("{statement}", scriptSource)
                .Replace("{condition}", conditionSource);

            var providerOptions = new Dictionary<string, string> {
                //                    { "CompilerVersion", "v4.0" }
            };
            var provider = new CSharpCodeProvider(providerOptions);
            var compilerParams = new CompilerParameters {
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
                    if (int.TryParse(displayName.Invoke(null, null).ToString().Substring(0, 1), out major) && major > 2)
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
            compilerParams.ReferencedAssemblies.Add("NLog.dll");
            compilerParams.ReferencedAssemblies.Add("Newtonsoft.Json.dll");

            compilerParams.ReferencedAssemblies.Add("SerialPortLib.dll");
            compilerParams.ReferencedAssemblies.Add("NetClientLib.dll");

            //if (Raspberry.Board.Current.IsRaspberryPi)
            {
                compilerParams.ReferencedAssemblies.Add("Raspberry.IO.dll");
                compilerParams.ReferencedAssemblies.Add("Raspberry.IO.Components.dll");
                compilerParams.ReferencedAssemblies.Add("Raspberry.IO.GeneralPurpose.dll");
                compilerParams.ReferencedAssemblies.Add("Raspberry.IO.InterIntegratedCircuit.dll");
                compilerParams.ReferencedAssemblies.Add("Raspberry.IO.SerialPeripheralInterface.dll");
                compilerParams.ReferencedAssemblies.Add("Raspberry.System.dll");
                compilerParams.ReferencedAssemblies.Add("UnitsNet.dll");
            }

            compilerParams.ReferencedAssemblies.Add("M2Mqtt.Net.dll");
            compilerParams.ReferencedAssemblies.Add(Path.Combine("lib", "shared", "Innovative.Geometry.dll"));
            compilerParams.ReferencedAssemblies.Add(Path.Combine("lib", "shared", "Innovative.SolarCalculator.dll"));

            // compile and generate script assembly
            return provider.CompileAssemblyFromSource(compilerParams, source);
        }

    }
}
