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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using NLog;

using HomeGenie.Automation.Scripting;
using HomeGenie.Service;
using HomeGenie.Service.Constants;

namespace HomeGenie.Automation.Engines
{
    public abstract class ProgramEngineBase
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        // Main program threads
        private Thread _startupThread;
        private Thread _programThread;

        private readonly List<string> _registeredApi = new List<string>();

        protected readonly ProgramBlock ProgramBlock;
        protected HomeGenieService HomeGenie;

        // System events handlers
        public Func<bool> SystemStarted;
        public Func<bool> SystemStopping;
        public Func<bool> Stopping;
        public Func<ModuleHelper, Data.ModuleParameter, bool> ModuleChangedHandler;
        public Func<ModuleHelper, Data.ModuleParameter, bool> ModuleIsChangingHandler;

        public readonly AutoResetEvent RoutedEventAck = new AutoResetEvent(false);

        protected ProgramEngineBase(ProgramBlock pb)
        {
            ProgramBlock = pb;
        }

        public void SetHost(HomeGenieService hg)
        {
            ((IProgramEngine) this).Unload();
            HomeGenie = hg;
            ((IProgramEngine) this).Load();
        }

        public void StartScheduler()
        {
            StopScheduler();
            HomeGenie.ProgramManager.RaiseProgramModuleEvent(ProgramBlock, Properties.ProgramStatus, "Idle");
            _startupThread = new Thread(CheckProgramSchedule);
            _startupThread.Start();
        }

        public void StopScheduler()
        {
            if (_startupThread != null)
            {
                try
                {
                    RoutedEventAck.Set();
                    if (!_startupThread.Join(1000))
                    {
                        _startupThread.Interrupt();
                    }
                }
                catch
                {
                    // ignored
                }
                _startupThread = null;
            }
            if (_programThread != null)
                StopProgram();
        }

        public void StartProgram(string options = null)
        {
            if (ProgramBlock.IsRunning)
                return;

            // This seems to occur only when restoring from a backup
            if (_programThread != null)
            {
                StopProgram();
            }

            if (!ProgramBlock.IsEnabled)
                return;
            
            ProgramBlock.IsRunning = true;
            HomeGenie.ProgramManager.RaiseProgramModuleEvent(ProgramBlock, Properties.ProgramStatus, "Running");

            _programThread = new Thread(() =>
            {
                try
                {
                    MethodRunResult result;
                    try
                    {
                        result = Run(options);
                    }
                    catch (Exception ex)
                    {
                        result = new MethodRunResult {Exception = ex};
                    }
                    _programThread = null;
                    ProgramBlock.IsRunning = false;
                    if (result != null && result.Exception != null &&
                        result.Exception.GetType() != typeof(TargetException) &&
                        result.Exception.GetType() != typeof(ThreadInterruptedException))
                    {
                        // runtime error occurred, script is being disabled
                        // so user can notice and fix it
                        var error = new List<ProgramError> {GetFormattedError(result.Exception, false)};
                        ProgramBlock.ScriptErrors = JsonConvert.SerializeObject(error);
                        _log.Error(result.Exception, "Error while running program {0}", ProgramBlock.Address);
                        if (HomeGenie.ProgramManager != null)
                        {
                            HomeGenie.ProgramManager.RaiseProgramModuleEvent(ProgramBlock, Properties.RuntimeError,
                                PrepareExceptionMessage(CodeBlockEnum.CR, result.Exception));
                        }
                        TryToAutoRestart();
                    }
                    if (HomeGenie.ProgramManager != null)
                    {
                        HomeGenie.ProgramManager.RaiseProgramModuleEvent(ProgramBlock, Properties.ProgramStatus,
                            ProgramBlock.IsEnabled ? "Idle" : "Stopped");
                    }
                }
                catch (ThreadAbortException)
                {
                    _programThread = null;
                    ProgramBlock.IsRunning = false;
                    if (HomeGenie.ProgramManager != null)
                    {
                        HomeGenie.ProgramManager.RaiseProgramModuleEvent(
                            ProgramBlock,
                            Properties.ProgramStatus,
                            "Interrupted"
                        );
                    }
                }
                catch (ThreadInterruptedException)
                {
                    _programThread = null;
                    ProgramBlock.IsRunning = false;
                    if (HomeGenie.ProgramManager != null)
                    {
                        HomeGenie.ProgramManager.RaiseProgramModuleEvent(
                            ProgramBlock,
                            Properties.ProgramStatus,
                            "Interrupted"
                        );
                    }
                }
            });

            try
            {
                _programThread.Start();
            }
            catch
            {
                StopProgram();
                HomeGenie.ProgramManager.RaiseProgramModuleEvent(ProgramBlock, Properties.ProgramStatus, "Idle");
            }
        }

        public void StopProgram()
        {
            if (Stopping != null)
            {
                try
                {
                    Stopping();
                }
                catch
                {
                    // ignored
                }
            }
            ProgramBlock.IsRunning = false;
            //
            // cleanup and deallocation stuff here
            //
            ModuleIsChangingHandler = null;
            ModuleChangedHandler = null;
            SystemStarted = null;
            SystemStopping = null;
            Stopping = null;
            //
            foreach (string apiCall in _registeredApi)
            {
                ProgramDynamicApi.UnRegister(apiCall);
            }
            _registeredApi.Clear();
            //
            ((IProgramEngine) this).Unload();

            if (_programThread == null) return;
            HomeGenie.ProgramManager.RaiseProgramModuleEvent(ProgramBlock, Properties.ProgramStatus, "Stopping");
            try
            {
                if (!_programThread.Join(HomeGenie.ProgramManager.Enabled ? 15000: 1000))
                {
                    _programThread.Interrupt();
                }
            }
            catch
            {
                // ignored
            }
            _programThread = null;
        }

        public virtual List<ProgramError> Compile()
        {
            throw new NotImplementedException();
        }

        public virtual MethodRunResult Run(string options)
        {
            throw new NotImplementedException();
        }

        public virtual ProgramError GetFormattedError(Exception e, bool isSetupBlock)
        {
            throw new NotImplementedException();
        }

        public virtual MethodRunResult Setup()
        {
            throw new NotImplementedException();
        }

        #region Automation Programs Dynamic API 

        public void RegisterDynamicApi(string apiCall, Func<object, object> handler)
        {
            _registeredApi.Add(apiCall);
            ProgramDynamicApi.Register(apiCall, handler);
        }

        #endregion

        private void CheckProgramSchedule()
        {
            // set initial state to signaled
            RoutedEventAck.Set();
            while (HomeGenie.ProgramManager.Enabled && ProgramBlock.IsEnabled)
            {
                // if no event is received this will ensure that the StartupCode is run at least every minute for checking scheduler conditions if any
                try
                {
                    RoutedEventAck.WaitOne((60 - DateTime.Now.Second) * 1000);
                }
                catch (Exception e)
                {
                    _log.Error(e.Message, "Error in program scheduler",
                        ProgramBlock.Address);
                    //throw;
                }
                // the setup code is not evaluated while the program is running the main code
                if (ProgramBlock.IsRunning || !ProgramBlock.IsEnabled || !HomeGenie.ProgramManager.Enabled)
                {
                    continue;
                }
                // Run asynchronously to prevent RoutedEventAck loops
                Task.Run(() =>
                {
                    if (ProgramBlock.IsEnabled && WillProgramRun())
                    {
                        ProgramBlock.WillRun = false;
                        StartProgram(null);
                    }
                }).Wait();
            }
        }

        private bool WillProgramRun()
        {
            bool isConditionSatisfied = false;
            // evaluate and get result from the setup code
            try
            {
                var result = Setup();
                if (result != null && result.Exception != null)
                {
                    // runtime error occurred, script is being disabled
                    // so user can notice and fix it
                    var error = new List<ProgramError> {GetFormattedError(result.Exception, true)};
                    ProgramBlock.ScriptErrors = JsonConvert.SerializeObject(error);
                    _log.Error(result.Exception, "Error while running setup code in program {0}",
                        ProgramBlock.Address);
                    HomeGenie.ProgramManager.RaiseProgramModuleEvent(ProgramBlock, Properties.RuntimeError,
                        PrepareExceptionMessage(CodeBlockEnum.TC, result.Exception));

                    TryToAutoRestart();
                }
                else
                {
                    isConditionSatisfied = (result != null ? (bool) result.ReturnValue : false);
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(TargetException) || ex is ThreadAbortException || ex is ThreadInterruptedException)
                {
                    return isConditionSatisfied && ProgramBlock.IsEnabled;
                }
                // a runtime error occured
                List<ProgramError> error = new List<ProgramError>() {GetFormattedError(ex, true)};
                ProgramBlock.ScriptErrors = JsonConvert.SerializeObject(error);
                HomeGenie.ProgramManager.RaiseProgramModuleEvent(ProgramBlock, Properties.RuntimeError,
                    PrepareExceptionMessage(CodeBlockEnum.TC, ex));
                TryToAutoRestart();
            }
            return isConditionSatisfied && ProgramBlock.IsEnabled;
        }

        private void TryToAutoRestart()
        {
            if (ProgramBlock.AutoRestartEnabled)
            {
                Thread.Sleep(2000); // sleep 2 secs to avoid fast fail loops
                ProgramBlock.IsEnabled = true;
            }
            else
            {
                ProgramBlock.IsEnabled = false;
            }
        }

        private static string PrepareExceptionMessage(CodeBlockEnum codeType, Exception ex)
        {
            return codeType + ": " + ex.Message.Replace('\n', ' ').Replace('\r', ' ');
        }
    }
}
