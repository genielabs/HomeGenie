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
using HomeGenie.WCF;

namespace HomeGenieService
{
    class HomeGenieService : ServiceBase
    {
        private HomeGenie.Service.HomeGenieService hg = null;
        private ServiceHost hgmanagerservice = null;
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
            hg = new HomeGenie.Service.HomeGenieService();
            hg.LogEventAction += new Action<HomeGenie.Data.LogEntry>(hg_LogEventAction);
            hg.Start();

            if (hgmanagerservice != null)
            {
                hgmanagerservice.Close();
            }
            // Create a ServiceHost for the CalculatorService type and 
            // provide the base address.
            ManagerService mgs = new ManagerService();
            hgmanagerservice = new ServiceHost(mgs);

            // Open the ServiceHostBase to create listeners and start 
            // listening for messages.
            hgmanagerservice.Open();
            //
            mgs.SetHomeGenieHost(hg);
        }

        void hg_LogEventAction(HomeGenie.Data.LogEntry obj)
        {
            if (hgmanagerservice != null)
            {
                (hgmanagerservice.SingletonInstance as ManagerService).RaiseOnEventLogged(new LogEntry(){ Description = obj.Description, Domain = obj.Domain, Property = obj.Property, Source = obj.Source, Value = obj.Value });
            }
        }

        protected override void OnStop()
        {
            base.OnStop();
            //
            hg.Stop();
            //
            if (hgmanagerservice != null)
            {
                hgmanagerservice.Close();
            }
            hgmanagerservice = null;
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
