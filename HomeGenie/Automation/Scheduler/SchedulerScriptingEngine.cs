using System;
using Jint;
using HomeGenie.Service;
using System.Threading;
using System.Collections.Generic;
using HomeGenie.Service.Constants;
using Newtonsoft.Json;

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
          boundModules: hg.boundModules,
          // ProgramHelperBase
          program: hg.Program,
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
          delay: function(seconds) { this.pause(seconds); },
          event: hg.event
        };
        $$.onNext = function() {
          var nextMin = new Date();
          nextMin.setSeconds(0);
          nextMin = new Date(nextMin.getTime()+60000);
          return $$.scheduler.isOccurrence(nextMin, event.CronExpression);
        };
        $$.onPrevious = function() {
          var prevMin = new Date();
          prevMin.setSeconds(0);
          prevMin = new Date(prevMin.getTime()-60000);
          return $$.scheduler.isOccurrence(prevMin, event.CronExpression);
        };
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
            hgScriptingHost.SetHost(homegenie, item);
            scriptEngine.SetValue("hg", hgScriptingHost);
            scriptEngine.SetValue("event", eventItem);
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
            homegenie.RaiseEvent(this, Domains.HomeAutomation_HomeGenie, SourceModule.Scheduler, eventItem.Name, "EventScript.Status", eventItem.Name+":Start");

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
                        homegenie.RaiseEvent(this, Domains.HomeAutomation_HomeGenie, SourceModule.Scheduler, eventItem.Name, "EventScript.Status", eventItem.Name+":Error ("+result.Exception.Message.Replace('\n', ' ').Replace('\r', ' ')+")");
                }
                catch (ThreadAbortException)
                {
                    programThread = null;
                    isRunning = false;
                    homegenie.RaiseEvent(this, Domains.HomeAutomation_HomeGenie, SourceModule.Scheduler, eventItem.Name, "EventScript.Status", eventItem.Name+":Interrupted");
                }
                homegenie.RaiseEvent(this, Domains.HomeAutomation_HomeGenie, SourceModule.Scheduler, eventItem.Name, "EventScript.Status", eventItem.Name+":End");
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

