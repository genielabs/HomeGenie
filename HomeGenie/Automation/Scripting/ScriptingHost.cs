using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HomeGenie.Service;

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
        private SchedulerHelper schedulerHelper;

        public void SetHost(HomeGenieService hg, int programId)
        {
            homegenie = hg;
            netHelper = new NetHelper(homegenie);
            programHelper = new ProgramHelper(homegenie, programId);
            eventsHelper = new EventsHelper(homegenie, programId);
            serialPortHelper = new SerialPortHelper();
            tcpClientHelper = new TcpClientHelper();
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
            programHelper.Reset();
            serialPortHelper.Disconnect();
            tcpClientHelper.Disconnect();
        }

    }

}
