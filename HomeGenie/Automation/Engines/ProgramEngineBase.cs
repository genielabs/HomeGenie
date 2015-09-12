using System;
using System.Collections.Generic;
using System.Threading;
using HomeGenie.Service;

namespace HomeGenie.Automation.Engines
{
    public class ProgramEngineBase
    {
        protected ProgramBlock programBlock;
        protected HomeGenieService homegenie;


        // System events handlers
        internal Func<bool> SystemStarted;
        internal Func<bool> SystemStopping;
        internal Func<bool> Stopping;
        internal Func<HomeGenie.Automation.Scripting.ModuleHelper, HomeGenie.Data.ModuleParameter, bool> ModuleChangedHandler;
        internal Func<HomeGenie.Automation.Scripting.ModuleHelper, HomeGenie.Data.ModuleParameter, bool> ModuleIsChangingHandler;
        internal List<string> registeredApiCalls = new List<string>();

        // Main program thread
        internal Thread ProgramThread;

        internal ManualResetEvent RoutedEventAck = new ManualResetEvent(false);

        public ProgramEngineBase(ProgramBlock pb)
        {
            programBlock = pb;
        }

        public void SetHost(HomeGenieService hg)
        {
            (this as IProgramEngine).Unload();
            homegenie = hg;
            (this as IProgramEngine).Load();
        }

        public void Start()
        {
            // TODO: move thread allocation from ProgramManager.cs to here
        }

        public void Stop()
        {
            if (this.Stopping != null)
            {
                try { Stopping(); } catch { }
            }
            programBlock.IsRunning = false;
            //
            //TODO: complete cleanup and deallocation stuff here
            //
            ModuleIsChangingHandler = null;
            ModuleChangedHandler = null;
            SystemStarted = null;
            SystemStopping = null;
            Stopping = null;
            //
            foreach (string apiCall in registeredApiCalls)
            {
                homegenie.ProgramManager.UnRegisterDynamicApi(apiCall);
            }
            registeredApiCalls.Clear();
            //
            (this as IProgramEngine).Unload();

            if (ProgramThread != null)
            {
                try
                {
                    if (!ProgramThread.Join(1000))
                        ProgramThread.Abort();
                } catch { }
                ProgramThread = null;
            }

        }

    }
}

