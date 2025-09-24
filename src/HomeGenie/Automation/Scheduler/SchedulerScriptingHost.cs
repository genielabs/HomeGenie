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
using System.Threading;
using System.Threading.Tasks;
using HomeGenie.Automation.Scripting;
using HomeGenie.Data;
using HomeGenie.Service;
using HomeGenie.Service.Constants;

namespace HomeGenie.Automation.Scheduler
{
    [Serializable]
    public class SchedulerScriptingHost
    {
        private HomeGenieService homegenie;
        private SchedulerItem schedulerItem;

        private Store localStore;

        //
        private NetHelper netHelper;
        private SerialPortHelper serialPortHelper;
        private TcpClientHelper tcpClientHelper;
        private UdpClientHelper udpClientHelper;
        private MqttClientHelper mqttClientHelper;
        private KnxClientHelper knxClientHelper;
        private SchedulerHelper schedulerHelper;
        private ProgramHelperBase programHelper;
        private readonly StoreHelper storeHelper;
        private Action<ModuleHelper, ModuleParameter> moduleUpdateHandler;

        public SchedulerScriptingHost()
        {
            localStore = new Store();
            storeHelper = new StoreHelper(new TsList<Store>() {localStore}, "local");
        }

        public void SetHost(HomeGenieService hg, SchedulerItem item)
        {
            homegenie = hg;
            schedulerItem = item;
            Reset();
            netHelper = new NetHelper(homegenie);
            serialPortHelper = new SerialPortHelper();
            tcpClientHelper = new TcpClientHelper();
            udpClientHelper = new UdpClientHelper();
            mqttClientHelper = new MqttClientHelper();
            knxClientHelper = new KnxClientHelper();
            schedulerHelper = new SchedulerHelper(homegenie);
            programHelper = new ProgramHelperBase(homegenie);
        }

        public void RouteModuleEvent(ProgramManager.RoutedEvent eventData)
        {
            if (moduleUpdateHandler == null) return;
            var module = new ModuleHelper(homegenie, eventData.Module);
            var parameter = eventData.Parameter;
            var callback = new WaitCallback((state) =>
            {
                try
                {
                    homegenie.MigService.RaiseEvent(
                        this,
                        Domains.HomeAutomation_HomeGenie,
                        SourceModule.Scheduler,
                        "Scheduler Routed Event",
                        Properties.SchedulerModuleUpdateStart,
                        schedulerItem.Name);
                    moduleUpdateHandler(module, parameter);
                    homegenie.MigService.RaiseEvent(
                        this,
                        Domains.HomeAutomation_HomeGenie,
                        SourceModule.Scheduler,
                        "Scheduler Routed Event",
                        Properties.SchedulerModuleUpdateEnd,
                        schedulerItem.Name);
                }
                catch (Exception e)
                {
                    homegenie.MigService.RaiseEvent(
                        this,
                        Domains.HomeAutomation_HomeGenie,
                        SourceModule.Scheduler,
                        e.Message.Replace('\n', ' ').Replace('\r', ' '),
                        Properties.SchedulerError,
                        schedulerItem.Name);
                }
            });
            Task.Run(() => callback);
        }

        public SchedulerScriptingHost OnModuleUpdate(Action<ModuleHelper, ModuleParameter> handler)
        {
            moduleUpdateHandler = handler;
            return this;
        }

        public ProgramHelperBase Program
        {
            get { return programHelper; }
        }

        public ModulesManager Modules
        {
            get { return new ModulesManager(homegenie); }
        }

        public ModulesManager BoundModules
        {
            get
            {
                var boundModulesManager = new ModulesManager(homegenie);
                boundModulesManager.ModulesListCallback = (sender) =>
                {
                    TsList<Module> modules = new TsList<Module>();
                    foreach (var m in schedulerItem.BoundModules)
                    {
                        var mod = homegenie.Modules.Find(e => e.Address == m.Address && e.Domain == m.Domain);
                        if (mod != null)
                            modules.Add(mod);
                    }
                    return modules;
                };
                return boundModulesManager;
            }
        }

        public SettingsHelper Settings
        {
            get { return new SettingsHelper(homegenie); }
        }

        public NetHelper Net
        {
            get { return netHelper; }
        }

        public SerialPortHelper SerialPort
        {
            get { return serialPortHelper; }
        }

        public TcpClientHelper TcpClient
        {
            get { return tcpClientHelper; }
        }

        public UdpClientHelper UdpClient
        {
            get { return udpClientHelper; }
        }

        public MqttClientHelper MqttClient
        {
            get { return mqttClientHelper; }
        }

        public KnxClientHelper KnxClient
        {
            get { return knxClientHelper; }
        }

        public SchedulerHelper Scheduler
        {
            get { return schedulerHelper; }
        }

        public ModuleParameter Data(string name)
        {
            return storeHelper.Get(name);
        }

        public void Pause(double seconds)
        {
            Thread.Sleep((int) (seconds * 1000));
        }

        public void Delay(double seconds)
        {
            Pause(seconds);
        }

        public void Say(string sentence, string locale = null, bool goAsync = false)
        {
            if (String.IsNullOrWhiteSpace(locale))
            {
                locale = Thread.CurrentThread.CurrentCulture.Name;
            }
            try
            {
                Utility.Say(sentence, locale, goAsync);
            }
            catch (Exception e)
            {
                HomeGenieService.LogError(e);
            }
        }

        public void Reset()
        {
            try
            {
                serialPortHelper.Reset();
            }
            catch
            {
                // ignored
            }
            try
            {
                tcpClientHelper.Reset();
            }
            catch
            {
                // ignored
            }
            try
            {
                udpClientHelper.Reset();
            }
            catch
            {
                // ignored
            }
            try
            {
                netHelper.Reset();
            }
            catch
            {
                // ignored
            }
            try
            {
                mqttClientHelper.Reset();
            }
            catch
            {
                // ignored
            }
            try
            {
                knxClientHelper.Reset();
            }
            catch
            {
                // ignored
            }
        }
    }
}
