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

