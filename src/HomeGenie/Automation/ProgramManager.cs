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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HomeGenie.Automation.Engines;
using MIG;

using HomeGenie.Automation.Scheduler;
using HomeGenie.Data;
using HomeGenie.Service;
using HomeGenie.Service.Constants;

namespace HomeGenie.Automation
{
    public class ProgramManager
    {
        private readonly TsList<ProgramBlock> automationPrograms = new TsList<ProgramBlock>();
        private readonly HomeGenieService hgService;
        private readonly SchedulerService schedulerService;
        private readonly MacroRecorder macroRecorder;

        //private object lockObject = new object();
        private bool isEngineEnabled;

        public const int USERSPACE_PROGRAMS_START = 1000;
        public const int PACKAGE_PROGRAMS_START = 100000;

        public class RoutedEvent
        {
            public object Sender;
            public Module Module;
            public ModuleParameter Parameter;
        }

        public ProgramManager(HomeGenieService hg)
        {
            hgService = hg;
            macroRecorder = new MacroRecorder(this);
            schedulerService = new SchedulerService(this);
        }

        public bool Enabled
        {
            get { return isEngineEnabled; }
            set
            {
                bool wasEnabled = isEngineEnabled;
                isEngineEnabled = value;
                if (wasEnabled && !isEngineEnabled)
                {
                    schedulerService.Stop();
                    for (int p = 0; p < automationPrograms.Count; p++)
                    {
                        automationPrograms[p].Engine.StopScheduler();
                    }
                }
                else if (!wasEnabled && isEngineEnabled)
                {
                    schedulerService.Start();
                    for (int p = 0; p < automationPrograms.Count; p++)
                    {
                        var program = automationPrograms[p];
                        if (program.IsEnabled)
                        {
                            program.Engine.StartScheduler();
                        }
                    }
                }
            }
        }

        public HomeGenieService HomeGenie
        {
            get { return hgService; }
        }

        public MacroRecorder MacroRecorder
        {
            get { return macroRecorder; }
        }

        public SchedulerService SchedulerService
        {
            get { return schedulerService; }
        }

        public List<ProgramError> ProgramCompile(ProgramBlock program)
        {
            return program.Engine.Compile();
        }

        public TsList<ProgramBlock> Programs { get { return automationPrograms; } }

        public int GeneratePid()
        {
            int pid = USERSPACE_PROGRAMS_START;
            var userPrograms = automationPrograms
                .FindAll(p => p.Address >= USERSPACE_PROGRAMS_START && p.Address < PACKAGE_PROGRAMS_START)
                .OrderBy(p => p.Address);
            foreach (ProgramBlock program in userPrograms)
            {
                if (pid == program.Address)
                    pid = program.Address + 1;
                else
                    break;
            }
            // TODO: it should return -1 if all user programs are already allocated
            return pid;
        }

        public void ProgramAdd(ProgramBlock program)
        {
            if (program.Type.ToLower() == "wizard")
            {
                // TODO: convert Wizard Scripts (old HG < 1.4) to Visual Code (new in HG >= 1.4)
                WizardEngine.ConvertToVisualCode(hgService, program);
            }
            automationPrograms.Add(program);
            program.EnabledStateChanged += program_EnabledStateChanged;
            program.Engine.SetHost(hgService);
            RaiseProgramModuleEvent(program, Properties.ProgramStatus, "Added");
            if (isEngineEnabled && program.IsEnabled)
            {
                program.Engine.StartScheduler();
            }
        }

        public ProgramBlock ProgramGet(int pid)
        {
            return Programs.Find(p => p.Address == pid);
        }

        public void ProgramRemove(ProgramBlock program)
        {
            RaiseProgramModuleEvent(program, Properties.ProgramStatus, "Removed");
            program.IsEnabled = false;
            automationPrograms.Remove(program);
            // delete program files
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs");
            // remove csharp assembly
            try
            {
                File.Delete(Path.Combine(file, program.Address + ".dll"));
                File.Delete(Path.Combine(file, program.Address + ".dll.pdb"));
            }
            catch
            {
            }
            // TODO: implement also deleting of data/programs/<pid> folder!
            // remove arduino folder files
            try
            {
                Directory.Delete(Path.Combine(file, "arduino", program.Address.ToString()), true);
            } catch { }
        }

        public ProgramBlock ProgramClone(int pid, string newName = null)
        {
            var program = ProgramGet(pid);
            var copy = new ProgramBlock
            {
                Address = GeneratePid(),
                PackageInfo = program.PackageInfo,
                Domain = program.Domain,
                Type = program.Type,
                Group = program.Group,
                Name = String.IsNullOrEmpty(newName) ? "Copy of " + program.Name : newName,
                Description = program.Description,
                ScriptSetup = program.ScriptSetup,
                ScriptSource = program.ScriptSource,
                ScriptContext = program.ScriptContext,
                Data = program.Data,
                AutoRestartEnabled = program.AutoRestartEnabled,
                IsEnabled = program.IsEnabled
            };
            ProgramAdd(copy);
            ProgramCompile(copy);
            return copy;
        }

        public ProgramBlock GetProgram(int programId)
        {
            return automationPrograms.Find(p => p.Address.ToString() == programId.ToString());
        }

        internal void RaiseProgramModuleEvent(ProgramBlock program, string property, string value)
        {
            var programModule = hgService.Modules.Find(m => m.Domain == Domains.HomeAutomation_HomeGenie_Automation && m.Address == program.Address.ToString());
            if (programModule != null)
            {
                Utility.ModuleParameterSet(programModule, property, value);
            }
            hgService.RaiseEvent(
                program.Address, 
                Domains.HomeAutomation_HomeGenie_Automation, 
                program.Address.ToString(CultureInfo.InvariantCulture), 
                "Automation Program", 
                property, 
                value
            );
        }

        #region Module/Interface Events handling and propagation

        public void SignalPropertyChange(object sender, Module module, MigEvent eventData)
        {
            ModuleParameter parameter = Utility.ModuleParameterGet(module, eventData.Property);

            var routedEvent = new RoutedEvent() {
                Sender = sender,
                Module = module,
                Parameter = parameter
            };

            // Route event to Programs->ModuleIsChangingHandler
            if (RoutePropertyBeforeChangeEvent(routedEvent))
            {
                // Route event to Programs->ModuleChangedHandler
                Task.Run(() => RoutePropertyChangedEvent(routedEvent));
                // Route event to SchedulerService->OnModuleUpdate
                Task.Run(() => schedulerService.OnModuleUpdate(routedEvent));
            }
        }

        public bool RoutePropertyBeforeChangeEvent(object eventData)
        {
            var moduleEvent = (RoutedEvent)eventData;
            var moduleHelper = new Automation.Scripting.ModuleHelper(hgService, moduleEvent.Module);
            var param = moduleEvent.Parameter.DeepClone();
            for (int p = 0; p < Programs.Count; p++)
            {
                var program = Programs[p];
                if (!program.IsEnabled) continue;
                if ((moduleEvent.Sender == null || !moduleEvent.Sender.Equals(program.Address)))
                {
                    if (program.Engine.ModuleIsChangingHandler != null)
                    {
                        bool handled = !program.Engine.ModuleIsChangingHandler(moduleHelper, param);
                        if (handled)
                        {
                            // stop routing event if "false" is returned
                            MigService.Log.Debug("Event propagation halted by automation program '{0}' ({1}) (Name={2})", program.Name, program.Address, param.Name);
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public void RoutePropertyChangedEvent(object eventData)
        {
            var moduleEvent = (RoutedEvent)eventData;
            var moduleHelper = new Scripting.ModuleHelper(hgService, moduleEvent.Module);
            for (int p = 0; p < Programs.Count; p++)
            {
                var program = Programs[p];
                if (program == null || !program.IsEnabled || !isEngineEnabled) continue;
                if ((moduleEvent.Sender == null || !moduleEvent.Sender.Equals(program)))
                {
                    try
                    {
                        program.Engine.RoutedEventAck.Set();
                        if (program.Engine.ModuleChangedHandler != null && moduleEvent.Parameter != null) // && proceed)
                        {
                            var param = moduleEvent.Parameter.DeepClone();
                            bool handled = !program.Engine.ModuleChangedHandler(moduleHelper, param);
                            if (handled)
                            {
                                // stop routing event if "false" is returned
                                MigService.Log.Debug("Event propagation halted by automation program '{0}' ({1}) (Name={2})", program.Name, program.Address, param.Name);
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        HomeGenieService.LogError(
                            program.Domain,
                            program.Address.ToString(),
                            ex.Message,
                            "Exception.StackTrace",
                            ex.StackTrace
                        );
                    }
                }
            }
        }

        #endregion

        private void program_EnabledStateChanged(object sender, bool isEnabled)
        {
            ProgramBlock program = (ProgramBlock)sender;
            if (isEnabled)
            {
                program.ScriptErrors = "";
                hgService.modules_RefreshPrograms();
                hgService.modules_RefreshVirtualModules();
                RaiseProgramModuleEvent(program, Properties.ProgramStatus, "Enabled");
                program.Engine.StartScheduler();
            }
            else
            {
                RaiseProgramModuleEvent(program, Properties.ProgramStatus, "Disabled");
                program.Engine.StopScheduler();
                hgService.modules_RefreshPrograms();
                hgService.modules_RefreshVirtualModules();
            }
            //hgService.modules_Sort();
        }
    }
}
