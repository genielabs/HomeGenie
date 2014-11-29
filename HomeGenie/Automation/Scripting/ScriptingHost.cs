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

/*! \mainpage Extend, customize, create!
 *
 * \section terms_used Actors you will find in here
 *
 * **Program Engine** (also known as 'Master Control Program' or 'mcp')
 * \n
 * \n
 * **Automation Program Plugin** (also known as just 'program' or 'app')
 * \n
 * \n
 * **Module**
 * \n It consists of a fixed number of properties (Domain, Address, Name, Description) and a certain number of variant properties (see *Parameters*).
 * \n
 * \n
 * **Module Event**
 * \n .....
 * \n
 * \n
 * **Parameter**
 * \n .....
 * \n
 * \n
 * **Helper Class**
 * \n .....
 * \n
 *
 * \section faq_sec FAQ
 * \subsection faq_1 Where do I find some examples?
 * etc...
 * \subsection faq_2 How do I create a new app?
 * etc...
 * \subsection faq_3 What support libraries are referenced in a app?
 * (Raspberry#, nMQTT, NewtonSoft.Json,...)
 * etc...
 * \subsection faq_4 Can I create my robot's intelligence using HG?
 * Well... good luck! =)
 * \subsection faq_5 Can I add a custom module parameter to the Statistics page?
 * Any parameter with a valid numeric value is automatically added to the Statistics.
 * etc...
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
            //programHelper.Reset();
            serialPortHelper.Disconnect();
            tcpClientHelper.Disconnect();
            udpClientHelper.Disconnect();
            netHelper.Reset();
        }

    }

}
