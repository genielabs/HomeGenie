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

using HomeGenie.Service;

using HomeGenie.Automation.Scripting;
using HomeGenie.Data;

namespace HomeGenie.Automation.Scheduler
{

    public class MethodRunResult
    {
        public Exception Exception = null;
        public object ReturnValue = null;
    }

    [Serializable]
    public class SchedulerScriptingHost
    {

        private HomeGenieService homegenie = null;
        private SchedulerItem schedulerItem = null;
        //
        private NetHelper netHelper;
        private SerialPortHelper serialPortHelper;
        private TcpClientHelper tcpClientHelper;
        private UdpClientHelper udpClientHelper;
        private MqttClientHelper mqttClientHelper;
        private KnxClientHelper knxClientHelper;
        private SchedulerHelper schedulerHelper;
        private ProgramHelperBase programHelper;

        public void SetHost(HomeGenieService hg, SchedulerItem item)
        {
            homegenie = hg;
            schedulerItem = item;
            netHelper = new NetHelper(homegenie);
            serialPortHelper = new SerialPortHelper();
            tcpClientHelper = new TcpClientHelper();
            udpClientHelper = new UdpClientHelper();
            mqttClientHelper = new MqttClientHelper();
            knxClientHelper = new KnxClientHelper();
            schedulerHelper = new SchedulerHelper(homegenie);
            programHelper = new ProgramHelperBase(homegenie);
        }

        public ProgramHelperBase Program
        {
            get
            {
                return programHelper;
            }
        }

        public ModulesManager Modules
        {
            get
            {
                return new ModulesManager(homegenie);
            }
        }

        public ModulesManager BoundModules
        {
            get
            {
                var boundModulesManager = new ModulesManager(homegenie);
                boundModulesManager.ModulesListCallback = new Func<ModulesManager,TsList<Module>>((sender)=>{
                    TsList<Module> modules = new TsList<Module>();
                    foreach(var m in schedulerItem.BoundModules) {
                        var mod = homegenie.Modules.Find(e=>e.Address == m.Address && e.Domain == m.Domain);
                        if (mod != null)
                            modules.Add(mod);
                    }
                    return modules;
                });
                return boundModulesManager;
            }
        }

        public SettingsHelper Settings
        {
            get
            {
                return new SettingsHelper(homegenie);
            }
        }

        public NetHelper Net
        {
            get
            {
                return netHelper;
            }
        }

        public SerialPortHelper SerialPort
        {
            get
            {
                return serialPortHelper;
            }
        }

        public TcpClientHelper TcpClient
        {
            get
            {
                return tcpClientHelper;
            }
        }

        public UdpClientHelper UdpClient
        {
            get
            {
                return udpClientHelper;
            }
        }

        public MqttClientHelper MqttClient
        {
            get
            {
                return mqttClientHelper;
            }
        }

        public KnxClientHelper KnxClient
        {
            get
            {
                return knxClientHelper;
            }
        }

        public SchedulerHelper Scheduler
        {
            get
            {
                return schedulerHelper;
            }
        }

        public void Pause(double seconds)
        {
            System.Threading.Thread.Sleep((int)(seconds * 1000));
        }

        public void Delay(double seconds)
        {
            Pause(seconds);
        }

        public void Say(string sentence, string locale = null, bool goAsync = false)
        {
            if (String.IsNullOrWhiteSpace(locale))
            {
                locale = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
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
            try { serialPortHelper.Reset(); } catch { }
            try { tcpClientHelper.Reset(); } catch { }
            try { udpClientHelper.Reset(); } catch { }
            try { netHelper.Reset(); } catch { }
            try { mqttClientHelper.Reset(); } catch { }
            try { knxClientHelper.Reset(); } catch { }
        }

    }

}
