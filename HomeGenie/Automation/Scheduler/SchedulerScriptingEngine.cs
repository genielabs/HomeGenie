using System;
using Jint;
using HomeGenie.Service;
using System.Threading;
using System.Collections.Generic;
using HomeGenie.Service.Constants;

namespace HomeGenie.Automation.Scheduler
{
    public class SchedulerScriptingEngine
    {
        private HomeGenieService homegenie;
        private SchedulerItem eventItem;
        private Thread programThread;
        private bool isRunning;

        private Engine scriptEngine;
        private SchedulerScriptingHost hgScriptingHost;
        private string initScript = @"var $$ = {
          // ModulesManager
          modules: hg.modules,
          // SettingsHelper
          settings: hg.settings,
          // NetHelper
          net: hg.net,
          // SerialPortHelper
          serial: hg.serialPort,
          // TcpClientHelper
          tcp: hg.tcpClient,
          // UdpClientHelper
          udp: hg.udpClient, 
          // MqttClientHelper
          mqtt: hg.mqttClient,
          // KnxClientHelper
          knx: hg.knxClient,
          // SchedulerHelper
          scheduler: hg.scheduler,
          // Miscellaneous functions
          pause: function(seconds) { hg.pause(seconds); },
          delay: function(seconds) { this.pause(seconds); }
        }
        ";

        public SchedulerScriptingEngine()
        {
        }

        public void SetHost(HomeGenieService hg, SchedulerItem item)
        {
            homegenie = hg;
            eventItem = item;
            scriptEngine = new Engine();
            hgScriptingHost = new SchedulerScriptingHost();
            hgScriptingHost.SetHost(homegenie);
            scriptEngine.SetValue("hg", hgScriptingHost);
        }

        public void Dispose()
        {
            StopScript();
        }

        public bool IsRunning
        {
            get { return isRunning; }
        }

        public void StartScript()
        {
            if (homegenie == null || eventItem == null || isRunning || String.IsNullOrWhiteSpace(eventItem.Script))
                return;
            
            if (programThread != null)
                StopScript();

            isRunning = true;
            homegenie.RaiseEvent(this, Domains.HomeAutomation_HomeGenie, SourceModule.Scheduler, eventItem.Name, "EventScript.Status", "Start");

            programThread = new Thread(() =>
            {
                try
                {
                    MethodRunResult result = null;
                    try
                    {
                        scriptEngine.Execute(initScript+eventItem.Script);
                    }
                    catch (Exception ex)
                    {
                        result = new MethodRunResult();
                        result.Exception = ex;
                    }
                    programThread = null;
                    isRunning = false;
                    if (result != null && result.Exception != null && !result.Exception.GetType().Equals(typeof(System.Reflection.TargetException)))
                    {
                        // runtime error occurred, script is being disabled
                        // so user can notice and fix it
                        //List<ProgramError> error = new List<ProgramError>() { scriptEngine.GetFormattedError(result.Exception, false) };
                        //programBlock.ScriptErrors = JsonConvert.SerializeObject(error);
                        //programBlock.IsEnabled = false;
                        //homegenie.ProgramManager.RaiseProgramModuleEvent(programBlock, Properties.RuntimeError, "CR: " + result.Exception.Message.Replace('\n', ' ').Replace('\r', ' '));
                    }
                    homegenie.RaiseEvent(this, Domains.HomeAutomation_HomeGenie, SourceModule.Scheduler, eventItem.Name, "EventScript.Status", "End");
                    //homegenie.RaiseEvent(programBlock, Properties.ProgramStatus, programBlock.IsEnabled ? "Idle" : "Stopped");
                }
                catch (ThreadAbortException)
                {
                    programThread = null;
                    isRunning = false;
                    if (homegenie.ProgramManager != null)
                        homegenie.RaiseEvent(this, Domains.HomeAutomation_HomeGenie, SourceModule.Scheduler, eventItem.Name, "EventScript.Status", "Interrupted");
                }
            });

            try
            {
                programThread.Start();
            }
            catch
            {
                StopScript();
            }
        }

        public void StopScript()
        {
            isRunning = false;
            if (programThread != null)
            {
                try
                {
                    if (!programThread.Join(1000))
                        programThread.Abort();
                } catch { }
                programThread = null;
            }
            if (hgScriptingHost != null)
                hgScriptingHost.Reset();
        }


    }
}

