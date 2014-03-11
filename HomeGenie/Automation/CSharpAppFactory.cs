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
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.CSharp;
using System.CodeDom.Compiler;

using HomeGenie.Service;
using System.IO;

namespace HomeGenie.Automation
{
    public class MethodRunResult
    {
        public Exception Exception = null;
        public object ReturnValue = null;
    }

    public class CSharpAppFactory
    {

        private ProgramEngine masterControlProgram = null;
        private HomeGenieService homegenie = null;

        public CSharpAppFactory(HomeGenieService hg)
        {
            homegenie = hg;
            masterControlProgram = homegenie.ProgramEngine;
            //AppDomain currentDomain = AppDomain.CurrentDomain;
            //currentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyLookup);
        }

        public CompilerResults CompileScript(string condition, string statement, string outputDllFile)
        {
            List<string> errors = new List<string>();
            string source = @"using System;
using System.Collections.Generic;

using HomeGenie;
using HomeGenie.Service;
using HomeGenie.Automation; using HomeGenie.Data;

using Newtonsoft.Json.Linq; using Raspberry; using Raspberry.IO.GeneralPurpose; using Raspberry.IO.GeneralPurpose.Behaviors; using Raspberry.IO.Components.Converters.Mcp4822; using Raspberry.IO.Components.Displays.Hd44780; using Raspberry.IO.Components.Expanders.Mcp23017; using Raspberry.IO.Components.Sensors.HcSr04; using Raspberry.IO.InterIntegratedCircuit; using Raspberry.IO.Components.Converters.Mcp3008;
namespace HomeGenie.Automation.Scripting
{
    public class ScriptingInstance
    {
        private void RunCode(string PROGRAM_OPTIONS_STRING)
        {
//////////////////////////////////////////////////////////////////
// NOTE: user code start line is 16 *** please add new code after this method, do not alter start line! ***
{statement}
//////////////////////////////////////////////////////////////////
        }

        private bool EvaluateConditionBlock()
        {
//////////////////////////////////////////////////////////////////
// NOTE: user code start line is ??? *** please add new code after this method, do not alter start line! ***
{condition}
//////////////////////////////////////////////////////////////////
        }

		private HomeGenie.Service.HomeGenieService homegenie = null;
		//
        private HomeGenie.Automation.Scripting.NetHelper netHelper;
        private HomeGenie.Automation.Scripting.ProgramHelper programHelper;
        private HomeGenie.Automation.Scripting.EventsHelper eventsHelper;
        private HomeGenie.Automation.Scripting.SerialPortHelper serialPortHelper;
        private HomeGenie.Automation.Scripting.TcpClientHelper tcpClientHelper;
        private HomeGenie.Automation.Scripting.SchedulerHelper schedulerHelper;

        public void SetHost(HomeGenie.Service.HomeGenieService hg, int programId)
        {
            homegenie = hg;
            netHelper = new NetHelper(homegenie);
            programHelper = new ProgramHelper(homegenie, programId);
            eventsHelper = new EventsHelper(homegenie, programId);
            serialPortHelper = new SerialPortHelper();
            tcpClientHelper = new TcpClientHelper();
            schedulerHelper = new SchedulerHelper(homegenie);
        }

        public ModulesManager Modules
        {
            get
            {
                return new ModulesManager(homegenie);
            }
        }

        public SettingsHelper Settings
        {
            get
            {
                return new SettingsHelper(homegenie);
            }
        }

        public NetHelper Net
        {
            get
            {
                return netHelper;
            }
        }

        public ProgramHelper Program
        {
            get
            {
                return programHelper;
            }
        }

        public EventsHelper Events
        {
            get
            {
                return eventsHelper;
            }
        }

        public EventsHelper When
        {
            get
            {
                return eventsHelper;
            }
        }

        public SerialPortHelper SerialPort
        {
            get
            {
                return serialPortHelper;
            }
        }

        public TcpClientHelper TcpClient
        {
            get
            {
                return tcpClientHelper;
            }
        }

        public SchedulerHelper Scheduler
        {
            get
            {
                return schedulerHelper;
            }
        }

        public void Pause(double seconds)
        {
            System.Threading.Thread.Sleep( (int)(seconds * 1000) );
        }

        public void Delay(double seconds)
        {
            Pause(seconds);
        }

        //public void Say(string s)
        //{
        //    
        //}

        public MethodRunResult Run(string PROGRAM_OPTIONS_STRING)
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

        public MethodRunResult EvaluateCondition()
        {
            Exception ex = null;
            bool retval = false;
            //
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

        public void Reset()
        {
            programHelper.Reset();
            serialPortHelper.Disconnect();
            tcpClientHelper.Disconnect();
        }

    }
}
";
            source = source.Replace("{statement}", statement);
            source = source.Replace("{condition}", condition);
            //
            Dictionary<string, string> providerOptions = new Dictionary<string, string>
                {
                    //                    { "CompilerVersion", "v4.0" }
                };
            CSharpCodeProvider provider = new CSharpCodeProvider(providerOptions);
            CompilerParameters compilerParams = new CompilerParameters
                {
                    GenerateInMemory = true,
                    GenerateExecutable = false,
                    IncludeDebugInformation = false,
                    TreatWarningsAsErrors = true,
                    OutputAssembly = outputDllFile
                };
            //
            // Mono runtime 2/3 compatibility fix 
            bool relocateSystemAsm = false;
            Type type = Type.GetType("Mono.Runtime");
            if (type != null)
            {
                MethodInfo displayName = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (displayName != null)
                {
                    int major = 0;
                    if (int.TryParse(displayName.Invoke(null, null).ToString().Substring(0, 1), out major) && major > 2)
                    {
                        relocateSystemAsm = true;
                    }
                }
            }
            if (relocateSystemAsm)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    var name = assembly.GetName();
                    if (name.Name.ToLower() == "system")
                    {
                        compilerParams.ReferencedAssemblies.Add(assembly.Location);
                    }
                    else if (name.Name.ToLower() == "system.core")
                    {
                        compilerParams.ReferencedAssemblies.Add(assembly.Location);
                    }
                    else if (name.Name.ToLower() == "microsoft.csharp")
                    {
                        compilerParams.ReferencedAssemblies.Add(assembly.Location);
                    }
                }
            }
            else
            {
                compilerParams.ReferencedAssemblies.Add("System.dll");
                compilerParams.ReferencedAssemblies.Add("System.Core.dll");
                compilerParams.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
            }
            //
            compilerParams.ReferencedAssemblies.Add("HomeGenie.exe");
            compilerParams.ReferencedAssemblies.Add("MIG.dll");
            compilerParams.ReferencedAssemblies.Add("Newtonsoft.Json.dll");
            //
            compilerParams.ReferencedAssemblies.Add("SerialPortLib.dll");
            compilerParams.ReferencedAssemblies.Add("TcpClientLib.dll");
            //
            //if (Raspberry.Board.Current.IsRaspberryPi)
            {
                compilerParams.ReferencedAssemblies.Add("Raspberry.IO.Components.dll");
                compilerParams.ReferencedAssemblies.Add("Raspberry.IO.GeneralPurpose.dll");
                compilerParams.ReferencedAssemblies.Add("Raspberry.IO.InterIntegratedCircuit.dll");
                compilerParams.ReferencedAssemblies.Add("Raspberry.IO.SerialPeripheralInterface.dll");
                compilerParams.ReferencedAssemblies.Add("Raspberry.System.dll");
            }
            //
            // compile and generate script assembly
            return provider.CompileAssemblyFromSource(compilerParams, source);
        }

        //private static Assembly AssemblyLookup(object sender, ResolveEventArgs args)
        //{
        //    string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //    string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
        //    if (File.Exists(assemblyPath) == false)
        //    {
        //        assemblyPath = Path.Combine("/usr/local/lib/mono/4.0", new AssemblyName(args.Name).Name + ".dll");
        //    }
        //    if (File.Exists(assemblyPath) == false)
        //    {
        //        assemblyPath = Path.Combine("/usr/lib/mono/4.0", new AssemblyName(args.Name).Name + ".dll");
        //    }
        //    if (File.Exists(assemblyPath) == false)
        //    {
        //        return null;
        //    }
        //    Assembly assembly = Assembly.LoadFrom(assemblyPath);
        //    return assembly;
        //}

    }
}
