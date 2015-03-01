﻿/*
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

/*! \mainpage Extend, customize, create!
 *
 * \section docs HomeGenie Documentation
 * \subsection doc_1 User Guide
 * <a href="http://www.homegenie.it/docs/quickstart.php">http://www.homegenie.it/docs/quickstart.php</a>
 * \subsection doc_3 Web Service API
 * <a href="http://www.homegenie.it/docs/api/overview.html">http://www.homegenie.it/docs/api/overview.html</a>
 * \subsection doc_2 Automation Programming
 * <a href="http://www.homegenie.it/docs/automation_introduction.php">http://www.homegenie.it/docs/automation_introduction.php</a>
 * \subsection doc_4 Helper Classes Reference
 * <a href="annotated.html">Class List</a>
 * 
 */

namespace HomeGenie.Automation.Scripting
{

    public class MethodRunResult
    {
        public Exception Exception = null;
        public object ReturnValue = null;
    }

    public class ScriptingHost
    {

        private HomeGenieService homegenie = null;
        internal bool executeCodeToRun = false;
        //
        private NetHelper netHelper;
        private ProgramHelper programHelper;
        private EventsHelper eventsHelper;
        private SerialPortHelper serialPortHelper;
        private TcpClientHelper tcpClientHelper;
        private UdpClientHelper udpClientHelper;
        private MqttClientHelper mqttClientHelper;
        private KnxClientHelper knxClientHelper;
        private SchedulerHelper schedulerHelper;

        public void SetHost(HomeGenieService hg, int programId)
        {
            homegenie = hg;
            netHelper = new NetHelper(homegenie);
            programHelper = new ProgramHelper(homegenie, programId);
            eventsHelper = new EventsHelper(homegenie, programId);
            serialPortHelper = new SerialPortHelper();
            tcpClientHelper = new TcpClientHelper();
            udpClientHelper = new UdpClientHelper();
            mqttClientHelper = new MqttClientHelper();
            knxClientHelper = new KnxClientHelper();
            schedulerHelper = new SchedulerHelper(homegenie);
        }

        public ModulesManager Modules
        {
            get
            {
                return new ModulesManager(homegenie);
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

        public ProgramHelper Program
        {
            get
            {
                return programHelper;
            }
        }

        public EventsHelper Events
        {
            get
            {
                return eventsHelper;
            }
        }

        public EventsHelper When
        {
            get
            {
                return eventsHelper;
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

        public void SetConditionTrue()
        {
            executeCodeToRun = true;
        }

        public void SetConditionFalse()
        {
            executeCodeToRun = false;
        }

        public void Reset()
        {
            serialPortHelper.Reset();
            tcpClientHelper.Reset();
            udpClientHelper.Reset();
            netHelper.Reset();
            mqttClientHelper.Reset();
            knxClientHelper.Reset();
            programHelper.Reset();
        }

    }

}
