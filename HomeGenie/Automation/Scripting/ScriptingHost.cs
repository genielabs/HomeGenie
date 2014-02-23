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

namespace HomeGenie.Automation.Scripting
{
    public class MethodRunResult
    {
        public Exception Exception = null;
        public object ReturnValue = null;
    }

    public class ScriptingHost
    {

        private ProgramEngine _mastercontrolprogram = null;
        private HomeGenieService _homegenie = null;

        public ScriptingHost(HomeGenieService hgref)
        {
            _homegenie = hgref;
            _mastercontrolprogram = _homegenie.ProgramEngine;
//            AppDomain currentDomain = AppDomain.CurrentDomain;
//            currentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyLookup);
        }

        public CompilerResults CompileScript(string condition, string statement, string outdllfile)
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
        private void RunScript(string PROGRAM_OPTIONS_STRING)
        {
//////////////////////////////////////////////////////////////////
// NOTE: user code start line is 16 *** please add new code after this method, do not alter start line! ***
{statement}
//////////////////////////////////////////////////////////////////
        }

        private bool EvaluateConditionScript()
        {
//////////////////////////////////////////////////////////////////
// NOTE: user code start line is ??? *** please add new code after this method, do not alter start line! ***
{condition}
//////////////////////////////////////////////////////////////////
        }

		private HomeGenie.Service.HomeGenieService _homegenie = null;
		private HomeGenie.Automation.ProgramEngine _mastercontrolprogram = null;
		//
        private HomeGenie.Automation.Scripting.NetHelper _nethelper;
        private HomeGenie.Automation.Scripting.ProgramHelper _programhelper;
        private HomeGenie.Automation.Scripting.EventsHelper _eventshelper;
        private HomeGenie.Automation.Scripting.SerialPortHelper _serialporthelper;
        private HomeGenie.Automation.Scripting.TcpClientHelper _tcpclienthelper;
        private HomeGenie.Automation.Scripting.SchedulerHelper _schedulerhelper;

        public void SetHost(HomeGenie.Service.HomeGenieService hg, int programid)
        {
            _homegenie = hg;
            _mastercontrolprogram = hg.ProgramEngine;
            _nethelper = new NetHelper(_homegenie);
            _programhelper = new ProgramHelper(_homegenie, programid);
            _eventshelper = new EventsHelper(_homegenie, programid);
            _serialporthelper = new SerialPortHelper();
            _tcpclienthelper = new TcpClientHelper();
            _schedulerhelper = new SchedulerHelper(_homegenie);
        }

        public ModulesManager Modules
        {
            get
            {
                return new ModulesManager(_homegenie);
            }
        }

        public SettingsHelper Settings
        {
            get
            {
                return new SettingsHelper(_homegenie);
            }
        }

        public NetHelper Net
        {
            get
            {
                return _nethelper;
            }
        }

        public ProgramHelper Program
        {
            get
            {
                return _programhelper;
            }
        }

        public EventsHelper Events
        {
            get
            {
                return _eventshelper;
            }
        }

        public EventsHelper When
        {
            get
            {
                return _eventshelper;
            }
        }

        public SerialPortHelper SerialPort
        {
            get
            {
                return _serialporthelper;
            }
        }

        public TcpClientHelper TcpClient
        {
            get
            {
                return _tcpclienthelper;
            }
        }

        public SchedulerHelper Scheduler
        {
            get
            {
                return _schedulerhelper;
            }
        }

        public AutomationStatesManager AutomationStates
        {
            get { return _mastercontrolprogram.AutomationStates; }
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
                RunScript(PROGRAM_OPTIONS_STRING);
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
                    retval = EvaluateConditionScript();
            }
            catch (Exception e)
            {
                ex = e;
            }
            return new MethodRunResult(){ Exception = ex, ReturnValue = retval };
        }

        public void Reset()
        {
            _programhelper.Reset();
            _serialporthelper.Disconnect();
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
                    IncludeDebugInformation = true,
                    TreatWarningsAsErrors = true,
                    OutputAssembly = outdllfile 
                };
            //
            // Mono runtime 2/3 compatibility fix 
            bool relocatesystemasm = false;
            Type type = Type.GetType("Mono.Runtime");
            if (type != null)
            {
                MethodInfo displayName = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (displayName != null)
                {
                    int major = 0;
                    if (int.TryParse(displayName.Invoke(null, null).ToString().Substring(0, 1), out major) && major > 2)
                    {
                        relocatesystemasm = true;
                    }
                }
            }
            if (relocatesystemasm)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly a in assemblies)
                {
                    AssemblyName an = a.GetName();
                    if (an.Name.ToLower() == "system")
                    {
                        compilerParams.ReferencedAssemblies.Add(a.Location);
                    }
                    else if (an.Name.ToLower() == "system.core")
                    {
                        compilerParams.ReferencedAssemblies.Add(a.Location);
                    }
                    else if (an.Name.ToLower() == "microsoft.csharp")
                    {
                        compilerParams.ReferencedAssemblies.Add(a.Location);
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
