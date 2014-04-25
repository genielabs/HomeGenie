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

using System.Configuration;
using System.Configuration.Install;
using System.ComponentModel;
using System.Reflection;
using System.ServiceModel;
using System.ServiceProcess;

using HomeGenie;
using HomeGenie.Service;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace HomeGenieService
{
    class HomeGenieService : ServiceBase
    {
        private Process homegenie = null;
        //private ServiceHost serviceManager = null;
        //
        public HomeGenieService()
        {
            this.ServiceName = "HomeGenie";
            this.CanHandlePowerEvent = true;
            this.CanHandleSessionChangeEvent = false;
            this.CanPauseAndContinue = false;
            this.CanShutdown = true;
            this.CanStop = true;
        }


        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            //
            StartHomeGenie();
        }

        protected override void OnStop()
        {
            StopHomeGenie();
            //
            base.OnStop();
        }


        private void StartHomeGenie()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(HomeGenieProcess));
        }

        private void HomeGenieProcess(object o)
        {
            homegenie = new Process();
            homegenie.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HomeGenie.exe");
            homegenie.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            homegenie.StartInfo.UseShellExecute = false;
            homegenie.Start();
            homegenie.WaitForExit();
            //
            // if ExitCode is 1 then a restart has been required
            //if (homegenie.ExitCode == 1)
            //{
            //    
                Thread.Sleep(2000);
                StartHomeGenie();
            //}
        }

        private void StopHomeGenie()
        {
            if (homegenie != null)
            {
                try
                {
                    homegenie.Kill();
                }
                catch { }
                try
                {
                    homegenie.Dispose();
                }
                catch { }
            }
            homegenie = null;
        }


    }

    [RunInstaller(true)]
    public class HomeGenieInstaller : Installer
    {
        static void Main(string[] args)
        {

            if (System.Environment.UserInteractive)
            {
                string parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                        ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                        break;
                    case "--uninstall":
                        ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                        break;
                }
            }
            else
            {
                ServiceBase.Run(new HomeGenieService());
            }

        }
        //
        /// <summary>
        /// Public Constructor for WindowsServiceInstaller.
        /// - Put all of your Initialization code here.
        /// </summary>
        public HomeGenieInstaller()
        {
            ServiceProcessInstaller serviceProcessInstaller =
                               new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new ServiceInstaller();

            //# Service Account Information
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;

            //# Service Information
            serviceInstaller.DisplayName = "HomeGenie Automation Server";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            //# This must be identical to the WindowsService.ServiceBase name
            //# set in the constructor of WindowsService.cs
            serviceInstaller.ServiceName = "HomeGenieService";

            this.Installers.Add(serviceProcessInstaller);
            this.Installers.Add(serviceInstaller);
        }

    }

}
