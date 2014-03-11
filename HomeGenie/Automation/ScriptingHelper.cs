using HomeGenie.Automation.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HomeGenie.Automation
{

    public class ScriptingHelper
    {

        private HomeGenie.Service.HomeGenieService homegenie = null;
        //
        private HomeGenie.Automation.Scripting.NetHelper netHelper;
        private HomeGenie.Automation.Scripting.ProgramHelper programHelper;
        private HomeGenie.Automation.Scripting.EventsHelper eventsHelper;
        private HomeGenie.Automation.Scripting.SerialPortHelper serialPortHelper;
        private HomeGenie.Automation.Scripting.TcpClientHelper tcpClientHelper;
        private HomeGenie.Automation.Scripting.SchedulerHelper schedulerHelper;

        internal void SetHost(HomeGenie.Service.HomeGenieService hg, int programId)
        {
            homegenie = hg;
            netHelper = new NetHelper(homegenie);
            programHelper = new ProgramHelper(homegenie, programId);
            eventsHelper = new EventsHelper(homegenie, programId);
            serialPortHelper = new SerialPortHelper();
            tcpClientHelper = new TcpClientHelper();
            schedulerHelper = new SchedulerHelper(homegenie);
        }

        public dynamic Modules
        {
            get
            {
                return new ModulesManager(homegenie);
            }
        }

        public dynamic Settings
        {
            get
            {
                return new SettingsHelper(homegenie);
            }
        }

        public dynamic Net
        {
            get
            {
                return netHelper;
            }
        }

        public dynamic Program
        {
            get
            {
                return programHelper;
            }
        }

        public dynamic Events
        {
            get
            {
                return eventsHelper;
            }
        }

        public dynamic When
        {
            get
            {
                return eventsHelper;
            }
        }

        public dynamic SerialPort
        {
            get
            {
                return serialPortHelper;
            }
        }

        public dynamic TcpClient
        {
            get
            {
                return tcpClientHelper;
            }
        }

        public dynamic Scheduler
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

        public void Reset()
        {
            programHelper.Reset();
            serialPortHelper.Disconnect();
            tcpClientHelper.Disconnect();
        }


    }
}
