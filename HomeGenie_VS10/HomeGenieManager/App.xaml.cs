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
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

using System.Threading;
using OpenSource.UPnP;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace HomeGenieManager
{
    /// <summary>
    /// Logica di interazione per App.xaml
    /// </summary>
    /// 
    public partial class App : Application
    {
        static bool createdNew;
        static Mutex m_Mutex;

        private UPnPControlPoint upnpcontrol;
        private Dictionary<string, UPnPDevice> upnpservice;

        //Add this method override
        protected override void OnStartup(StartupEventArgs e)
        {
            m_Mutex = new Mutex(true, "HomeGenieManagerMutex", out createdNew);
            //e.Args is the string[] of command line argruments
            //
            upnpservice = new Dictionary<string, UPnPDevice>();
            upnpcontrol = new UPnPControlPoint();
            upnpcontrol.OnSearch += upnpcontrol_OnSearch;
            upnpcontrol.OnCreateDevice += upnpcontrol_OnCreateDevice;
            upnpcontrol.FindDeviceAsync("urn:schemas-upnp-org:device:HomeAutomationServer:1");
            //
            Application.Current.ShutdownMode = System.Windows.ShutdownMode.OnLastWindowClose;
            LoadingDialog myDialogWindow = new LoadingDialog();
            myDialogWindow.Show();
            //
            Task loader = new Task(() =>
            {
                int t = 0;
                while (t < 10)
                {
                    if (upnpservice.Count > 0)
                    {
                        Thread.Sleep(2000);
                        System.Diagnostics.Process.Start(UPnPDevices[UPnPDevices.Keys.ElementAt(0)].PresentationURL);
                        break;
                    }
                    else
                    {
                        t++;
                        Thread.Sleep(1000);
                    }
                }
                //
                Thread.Sleep(2000);
                //
                myDialogWindow.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!createdNew)
                    {
                        myDialogWindow.Close();
                        Application.Current.Shutdown();
                    }
                    else
                    {
                        MainWindow m = new MainWindow();
                        m.Show();
                        myDialogWindow.Close();
                    }
                }), null);
            });
            loader.Start();
        }

        private void upnpcontrol_OnSearch(System.Net.IPEndPoint ResponseFromEndPoint, System.Net.IPEndPoint ResponseReceivedOnEndPoint, Uri DescriptionLocation, string USN, string SearchTarget, int MaxAge)
        {
            upnpcontrol.CreateDeviceAsync(DescriptionLocation, MaxAge);
            //Console.WriteLine(USN + "\n" + SearchTarget);
        }

        private void upnpcontrol_OnCreateDevice(UPnPDevice Device, Uri DescriptionURL)
        {
            //Console.WriteLine(DescriptionURL + "\n" + Device.PresentationURL);
            lock (upnpservice)
                if (!upnpservice.ContainsKey(DescriptionURL.ToString()))
                {
                    upnpservice.Add(DescriptionURL.ToString(), Device);
                }
        }

        public Dictionary<string, UPnPDevice> UPnPDevices
        {
            get { return upnpservice; }
        }

    }
}
