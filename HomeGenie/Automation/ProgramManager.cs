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
using System.Threading;
using System.IO;
using HomeGenie.Data;
using HomeGenie.Service;
using MIG;
using HomeGenie.Service.Constants;
using HomeGenie.Automation.Scheduler;
using System.Linq;

namespace HomeGenie.Automation
{
    public class ProgramManager
    {
        private readonly TsList<ProgramBlock> _automationPrograms = new TsList<ProgramBlock>();
        private readonly HomeGenieService _hgService;
        private readonly SchedulerService _schedulerService;
        private readonly MacroRecorder _macroRecorder;

        //private object lockObject = new object();
        private bool _isEngineEnabled;

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
            _hgService = hg;
            _macroRecorder = new MacroRecorder(this);
            _schedulerService = new SchedulerService(this);
        }

        public bool Enabled
        {
            get { return _isEngineEnabled; }
            set 
            {
                bool wasEnabled = _isEngineEnabled;
                _isEngineEnabled = value;
                if (wasEnabled && !_isEngineEnabled)
                {
                    _schedulerService.Stop();
                    foreach (ProgramBlock program in _automationPrograms)
                    {
                        program.Engine.StopScheduler();
                    }
                }
                else if (!wasEnabled && _isEngineEnabled)
                {
                    _schedulerService.Start();
                    foreach (ProgramBlock program in _automationPrograms)
                    {
                        if (program.IsEnabled)
                            program.Engine.StartScheduler();
                    }
                }
            }
        }

        public HomeGenieService HomeGenie
        {
            get { return _hgService; }
        }

        public MacroRecorder MacroRecorder
        {
            get { return _macroRecorder; }
        }

        public SchedulerService SchedulerService
        {
            get { return _schedulerService; }
        }

        public List<ProgramError> CompileScript(ProgramBlock program)
        {
            return program.Compile();
        }

        // TODO: v1.1 !!!IMPORTANT!!! move thread allocation and starting to ProgramEngineBase.cs class
        public void Run(ProgramBlock program, string options)
        {
            program.Engine.StartProgram(options);
        }

        public TsList<ProgramBlock> Programs { get { return _automationPrograms; } }

        public int GeneratePid()
        {
            int pid = USERSPACE_PROGRAMS_START;
            var userPrograms = _automationPrograms
                .FindAll(p => p.Address >= ProgramManager.USERSPACE_PROGRAMS_START && p.Address < ProgramManager.PACKAGE_PROGRAMS_START)
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
            _automationPrograms.Add(program);
            program.EnabledStateChanged += program_EnabledStateChanged;
            program.Engine.SetHost(_hgService);
            RaiseProgramModuleEvent(program, Properties.ProgramStatus, "Added");
            if (_isEngineEnabled && program.IsEnabled)
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
            _automationPrograms.Remove(program);
            // delete program files
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs");
            // remove csharp assembly
            try
            {
                File.Delete(Path.Combine(file, program.Address + ".dll"));
            }
            catch
            {
            }
            // remove arduino folder files 
            try
            {
                Directory.Delete(Path.Combine(file, "arduino", program.Address.ToString()), true);
            } catch { }
        }

        internal void RaiseProgramModuleEvent(ProgramBlock program, string property, string value)
        {
            var programModule = _hgService.Modules.Find(m => m.Domain == Domains.HomeAutomation_HomeGenie_Automation && m.Address == program.Address.ToString());
            if (programModule != null)
            {
                Utility.ModuleParameterSet(programModule, property, value);
                _hgService.RaiseEvent(program.Address, programModule.Domain, programModule.Address, "Automation Program", property, value);
                //homegenie.MigService.RaiseEvent(actionEvent);
                //homegenie.SignalModulePropertyChange(this, programModule, actionEvent);
            }
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
                ThreadPool.QueueUserWorkItem(new WaitCallback(RoutePropertyChangedEvent), routedEvent);
                // Route event to SchedulerService->OnModuleUpdate
                ThreadPool.QueueUserWorkItem(new WaitCallback(_schedulerService.OnModuleUpdate), routedEvent);
            }
        }

        public bool RoutePropertyBeforeChangeEvent(object eventData)
        {
            var moduleEvent = (RoutedEvent)eventData;
            var moduleHelper = new Automation.Scripting.ModuleHelper(_hgService, moduleEvent.Module);
            string originalValue = moduleEvent.Parameter.Value;
            for (int p = 0; p < Programs.Count; p++)
            {
                var program = Programs[p];
                if (!program.IsEnabled) continue;
                if ((moduleEvent.Sender == null || !moduleEvent.Sender.Equals(program.Address)))
                {
                    if (program.Engine.ModuleIsChangingHandler != null)
                    {
                        bool handled = !program.Engine.ModuleIsChangingHandler(moduleHelper, moduleEvent.Parameter);
                        if (handled)
                        {
                            // stop routing event if "false" is returned
                            MigService.Log.Debug("Event propagation halted by automation program '{0}' ({1}) (Name={2}, OldValue={3}, NewValue={4})", program.Name, program.Address, moduleEvent.Parameter.Name, originalValue, moduleEvent.Parameter.Value);
                            return false;
                        }
                        else  if (moduleEvent.Parameter.Value != originalValue)
                        {
                            // If manipulated, the event is not routed anymore.
                            MigService.Log.Debug("Event propagation halted - parameter manipulated by automation program '{0}' ({1}) (Name={2}, OldValue={3}, NewValue={4})", program.Name, program.Address, moduleEvent.Parameter.Name, originalValue, moduleEvent.Parameter.Value);
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
            var moduleHelper = new Automation.Scripting.ModuleHelper(_hgService, moduleEvent.Module);
            string originalValue = moduleEvent.Parameter.Value;
            for (int p = 0; p < Programs.Count; p++)
            {
                var program = Programs[p];
                if (program == null || !program.IsEnabled || !_isEngineEnabled) continue;
                if ((moduleEvent.Sender == null || !moduleEvent.Sender.Equals(program)))
                {
                    try
                    {
                        program.Engine.RoutedEventAck.Set();
                        if (program.Engine.ModuleChangedHandler != null && moduleEvent.Parameter != null) // && proceed)
                        {
                            bool handled = !program.Engine.ModuleChangedHandler(moduleHelper, moduleEvent.Parameter);
                            if (handled)
                            {
                                // stop routing event if "false" is returned
                                MigService.Log.Debug("Event propagation halted by automation program '{0}' ({1}) (Name={2}, OldValue={3}, NewValue={4})", program.Name, program.Address, moduleEvent.Parameter.Name, originalValue, moduleEvent.Parameter.Value);
                                break;
                            }
                            else if (moduleEvent.Parameter.Value != originalValue)
                            {
                                // If manipulated, the event is not routed anymore.
                                MigService.Log.Debug("Event propagation halted - parameter manipulated by automation program '{0}' ({1}) (Name={2}, OldValue={3}, NewValue={4})", program.Name, program.Address, moduleEvent.Parameter.Name, originalValue, moduleEvent.Parameter.Value);
                                break;
                            }
                        }
                    }
                    catch (System.Exception ex)
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
                _hgService.modules_RefreshPrograms();
                _hgService.modules_RefreshVirtualModules();
                RaiseProgramModuleEvent(program, Properties.ProgramStatus, "Enabled");
                program.Engine.StartScheduler();
            }
            else
            {
                program.Engine.StopScheduler();
                RaiseProgramModuleEvent(program, Properties.ProgramStatus, "Disabled");
                _hgService.modules_RefreshPrograms();
                _hgService.modules_RefreshVirtualModules();
            }
            _hgService.modules_Sort();
        }
    }
}