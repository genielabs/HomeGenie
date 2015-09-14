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

        // Main program thread
        internal Thread ProgramThread;

        // System events handlers
        public Func<bool> SystemStarted;
        public Func<bool> SystemStopping;
        public Func<bool> Stopping;
        public Func<HomeGenie.Automation.Scripting.ModuleHelper, HomeGenie.Data.ModuleParameter, bool> ModuleChangedHandler;
        public Func<HomeGenie.Automation.Scripting.ModuleHelper, HomeGenie.Data.ModuleParameter, bool> ModuleIsChangingHandler;
        public List<string> RegisteredApi = new List<string>();
        public ManualResetEvent RoutedEventAck = new ManualResetEvent(false);

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
            foreach (string apiCall in RegisteredApi)
            {
                homegenie.ProgramManager.UnRegisterDynamicApi(apiCall);
            }
            RegisteredApi.Clear();
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

