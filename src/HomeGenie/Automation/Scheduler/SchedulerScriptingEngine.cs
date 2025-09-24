/*
   Copyright 2012-2025 G-Labs (https://github.com/genielabs)

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

/*
*     Author: Generoso Martello <gene@homegenie.it>
*     Project Homepage: https://homegenie.it
*/

using System;
using System.Reflection;
using System.Threading;

using Jint;

using HomeGenie.Service;
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
        private readonly SchedulerScriptingHost hgScriptingHost;

        private const string InitScript = @"var $$ = {
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
          // The scheduler event
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
        $$.data = function(k,v) {
            if (typeof v == 'undefined') {
                return hg.data(k).value;
            } else {
                hg.data(k).value = v;
                return $$;
            }
        };
        $$.onUpdate = function(handler) {
            hg.onModuleUpdate(handler);
        };
        ";

        public SchedulerScriptingEngine()
        {
            // we do not dispose the scripting host to keep volatile data persistent across instances
            hgScriptingHost = new SchedulerScriptingHost();
        }

        public void SetHost(HomeGenieService hg, SchedulerItem item)
        {
            homegenie = hg;
            eventItem = item;
            hgScriptingHost.SetHost(homegenie, item);
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

            if (scriptEngine == null)
            {
                scriptEngine = new Engine();
                scriptEngine.SetValue("hg", hgScriptingHost);
                scriptEngine.SetValue("event", eventItem);
                scriptEngine.Execute(InitScript + "\nfunction __action__() {\n" + eventItem.Script + "\n}\n");
            }

            isRunning = true;
            homegenie.RaiseEvent(this, Domains.HomeAutomation_HomeGenie, SourceModule.Scheduler, eventItem.Name,
                Properties.SchedulerScriptStatus, eventItem.Name + ":Start");

            programThread = new Thread(() =>
            {
                try
                {
                    MethodRunResult result = null;
                    try
                    {
                        scriptEngine.Execute("__action__();");
                    }
                    catch (Exception ex)
                    {
                        result = new MethodRunResult();
                        result.Exception = ex;
                    }
                    programThread = null;
                    isRunning = false;
                    if (result != null && result.Exception != null &&
                        result.Exception.GetType() != typeof(TargetException) &&
                        result.Exception.GetType() != typeof(ThreadInterruptedException))
                    {
                        homegenie.RaiseEvent(this, Domains.HomeAutomation_HomeGenie, SourceModule.Scheduler,
                            eventItem.Name, Properties.SchedulerScriptStatus,
                            eventItem.Name + ":Error (" + result.Exception.Message.Replace('\n', ' ').Replace('\r', ' ') + ")");
                    }
                }
                catch (ThreadAbortException)
                {
                    programThread = null;
                    isRunning = false;
                    homegenie.RaiseEvent(this, Domains.HomeAutomation_HomeGenie, SourceModule.Scheduler, eventItem.Name,
                        Properties.SchedulerScriptStatus, eventItem.Name + ":Interrupted");
                }
                catch (ThreadInterruptedException)
                {
                    programThread = null;
                    isRunning = false;
                    homegenie.RaiseEvent(this, Domains.HomeAutomation_HomeGenie, SourceModule.Scheduler, eventItem.Name,
                        Properties.SchedulerScriptStatus, eventItem.Name + ":Interrupted");
                }
                homegenie.RaiseEvent(this, Domains.HomeAutomation_HomeGenie, SourceModule.Scheduler, eventItem.Name,
                    Properties.SchedulerScriptStatus, eventItem.Name + ":End");
            });
            programThread.IsBackground = true;
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
            scriptEngine?.Dispose();
            scriptEngine = null;
            if (programThread != null)
            {
                try
                {
                    if (!programThread.Join(1000))
                    {
                        programThread.Interrupt();
                    }
                }
                catch
                {
                }
                programThread = null;
            }
            hgScriptingHost?.OnModuleUpdate(null);
            hgScriptingHost?.Reset();
        }

        public void RouteModuleEvent(object eventData)
        {
            var moduleEvent = (HomeGenie.Automation.ProgramManager.RoutedEvent) eventData;
            hgScriptingHost.RouteModuleEvent(moduleEvent);
        }
    }
}
