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
using HomeGenie.Service;
using HomeGenie.Service.Constants;
using HomeGenie.Automation.Scripting;
using Newtonsoft.Json;
using MIG;

namespace HomeGenie.Automation.Engines
{
    public class ProgramEngineBase
    {
        // Main program threads
        private Thread startupThread;
        private Thread programThread;

        private List<string> registeredApi = new List<string>();

        protected ProgramBlock programBlock;
        protected HomeGenieService homegenie;

        // System events handlers
        public Func<bool> SystemStarted;
        public Func<bool> SystemStopping;
        public Func<bool> Stopping;
        public Func<HomeGenie.Automation.Scripting.ModuleHelper, HomeGenie.Data.ModuleParameter, bool> ModuleChangedHandler;
        public Func<HomeGenie.Automation.Scripting.ModuleHelper, HomeGenie.Data.ModuleParameter, bool> ModuleIsChangingHandler;

        public AutoResetEvent RoutedEventAck = new AutoResetEvent(false);

        public ProgramEngineBase(ProgramBlock pb)
        {
            programBlock = pb;
        }

        public void SetHost(HomeGenieService hg)
        {
            (this as IProgramEngine).Unload();
            homegenie = hg;
            (this as IProgramEngine).Load();
        }

        public void StartScheduler()
        {
            StopScheduler();
            homegenie.ProgramManager.RaiseProgramModuleEvent(programBlock, Properties.ProgramStatus, "Idle");
            startupThread = new Thread(CheckProgramSchedule);
            startupThread.Start();
        }

        public void StopScheduler()
        {
            if (startupThread != null)
            {
                try
                {
                    RoutedEventAck.Set();
                    if (!startupThread.Join(1000))
                        startupThread.Abort();
                } catch { }
                startupThread = null;
            }
            if (programThread != null)
                StopProgram();
        }

        public void StartProgram(string options)
        {
            if (programBlock.IsRunning)
                return;

            // TODO: since if !program.IsRunning also thread should be null
            // TODO: so this is probably useless here and could be removed?
            if (programThread != null)
                StopProgram();

            programBlock.IsRunning = true;
            homegenie.ProgramManager.RaiseProgramModuleEvent(programBlock, Properties.ProgramStatus, "Running");

            programBlock.TriggerTime = DateTime.UtcNow;

            programThread = new Thread(() =>
            {
                try
                {
                    MethodRunResult result = null;
                    try
                    {
                        result = programBlock.Run(options);
                    }
                    catch (Exception ex)
                    {
                        result = new MethodRunResult();
                        result.Exception = ex;
                    }
                    programThread = null;
                    programBlock.IsRunning = false;
                    if (result != null && result.Exception != null && !result.Exception.GetType().Equals(typeof(System.Reflection.TargetException)))
                    {
                        // runtime error occurred, script is being disabled
                        // so user can notice and fix it
                        List<ProgramError> error = new List<ProgramError>() { programBlock.GetFormattedError(result.Exception, false) };
                        programBlock.ScriptErrors = JsonConvert.SerializeObject(error);
                        programBlock.IsEnabled = false;
                        homegenie.ProgramManager.RaiseProgramModuleEvent(programBlock, Properties.RuntimeError, "CR: " + result.Exception.Message.Replace('\n', ' ').Replace('\r', ' '));
                    }
                    homegenie.ProgramManager.RaiseProgramModuleEvent(programBlock, Properties.ProgramStatus, programBlock.IsEnabled ? "Idle" : "Stopped");
                }
                catch (ThreadAbortException)
                {
                    programThread = null;
                    programBlock.IsRunning = false;
                    homegenie.ProgramManager.RaiseProgramModuleEvent(programBlock, Properties.ProgramStatus, "Interrupted");
                }
            });

            if (programBlock.ConditionType == ConditionType.Once)
            {
                programBlock.IsEnabled = false;
            }

            try
            {
                programThread.Start();
            }
            catch
            {
                StopProgram();
                homegenie.ProgramManager.RaiseProgramModuleEvent(programBlock, Properties.ProgramStatus, "Idle");
            }
        }

        public void StopProgram()
        {
            if (this.Stopping != null)
            {
                try { Stopping(); } catch { }
            }
            programBlock.IsRunning = false;
            //
            //TODO: complete cleanup and deallocation stuff here
            //
            ModuleIsChangingHandler = null;
            ModuleChangedHandler = null;
            SystemStarted = null;
            SystemStopping = null;
            Stopping = null;
            //
            foreach (string apiCall in registeredApi)
            {
                ProgramDynamicApi.UnRegister(apiCall);
            }
            registeredApi.Clear();
            //
            (this as IProgramEngine).Unload();

            if (programThread != null)
            {
                try
                {
                    if (!programThread.Join(1000))
                        programThread.Abort();
                } catch { }
                programThread = null;
            }

        }


        #region Automation Programs Dynamic API 

        public void RegisterDynamicApi(string apiCall, Func<object, object> handler)
        {
            registeredApi.Add(apiCall);
            ProgramDynamicApi.Register(apiCall, handler);
        }

        #endregion


        private void CheckProgramSchedule()
        {
            // set initial state to signaled
            RoutedEventAck.Set();
            while ( homegenie.ProgramManager.Enabled && programBlock.IsEnabled)
            {
                // if no event is received this will ensure that the StartupCode is run at least every minute for checking scheduler conditions if any
                RoutedEventAck.WaitOne((60 - DateTime.Now.Second) * 1000);
                // the startup code is not evaluated while the program is running
                if (programBlock.IsRunning || !programBlock.IsEnabled || !homegenie.ProgramManager.Enabled)
                {
                    continue;
                }
                else if (WillProgramRun())
                {
                    StartProgram(null);
                }
            }
        }

        private bool WillProgramRun()
        {
            bool isConditionSatisfied = false;
            // evaluate and get result from the code
            lock (programBlock.OperationLock)
                try
            {
                programBlock.WillRun = false;
                //
                var result = programBlock.EvaluateCondition();
                if (result != null && result.Exception != null)
                {
                    // runtime error occurred, script is being disabled
                    // so user can notice and fix it
                    List<ProgramError> error = new List<ProgramError>() { programBlock.GetFormattedError(result.Exception, true) };
                    programBlock.ScriptErrors = JsonConvert.SerializeObject(error);
                    programBlock.IsEnabled = false;
                    homegenie.ProgramManager.RaiseProgramModuleEvent(programBlock, Properties.RuntimeError, "TC: " + result.Exception.Message.Replace('\n', ' ').Replace('\r', ' '));
                }
                else
                {
                    isConditionSatisfied = (result != null ? (bool)result.ReturnValue : false);
                }
                //
                bool lastResult = programBlock.LastConditionEvaluationResult;
                programBlock.LastConditionEvaluationResult = isConditionSatisfied;
                //
                if (programBlock.ConditionType == ConditionType.OnSwitchTrue)
                {
                    isConditionSatisfied = (isConditionSatisfied == true && isConditionSatisfied != lastResult);
                }
                else if (programBlock.ConditionType == ConditionType.OnSwitchFalse)
                {
                    isConditionSatisfied = (isConditionSatisfied == false && isConditionSatisfied != lastResult);
                }
                else if (programBlock.ConditionType == ConditionType.OnTrue || programBlock.ConditionType == ConditionType.Once)
                {
                    // noop
                }
                else if (programBlock.ConditionType == ConditionType.OnFalse)
                {
                    isConditionSatisfied = !isConditionSatisfied;
                }
            }
            catch (Exception ex)
            {
                // a runtime error occured
                if (!ex.GetType().Equals(typeof(System.Reflection.TargetException)))
                {
                    List<ProgramError> error = new List<ProgramError>() { programBlock.GetFormattedError(ex, true) };
                    programBlock.ScriptErrors = JsonConvert.SerializeObject(error);
                    programBlock.IsEnabled = false;
                    homegenie.ProgramManager.RaiseProgramModuleEvent(programBlock, Properties.RuntimeError, "TC: " + ex.Message.Replace('\n', ' ').Replace('\r', ' '));
                }
            }
            return isConditionSatisfied && programBlock.IsEnabled;
        }

    }
}

