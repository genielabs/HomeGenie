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
            scriptEngine = new Engine();
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
            homegenie.RaiseEvent(this, Domains.HomeAutomation_HomeGenie, SourceModule.Scheduler, eventItem.Name,
                Properties.SchedulerScriptStatus, eventItem.Name + ":Start");

            programThread = new Thread(() =>
            {
                try
                {
                    MethodRunResult result = null;
                    try
                    {
                        scriptEngine.Execute(InitScript + eventItem.Script);
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
                    {
#if NETCOREAPP
                        // _programThread.Abort(); => System.PlatformNotSupportedException: Thread abort is not supported on this platform.
                        programThread.Interrupt();
#else
                        programThread.Abort();
#endif
                    }
                }
                catch
                {
                }
                programThread = null;
            }
            if (hgScriptingHost != null)
            {
                hgScriptingHost.OnModuleUpdate(null);
                hgScriptingHost.Reset();
            }
        }

        public void RouteModuleEvent(object eventData)
        {
            var moduleEvent = (HomeGenie.Automation.ProgramManager.RoutedEvent) eventData;
            hgScriptingHost.RouteModuleEvent(moduleEvent);
        }
    }
}
