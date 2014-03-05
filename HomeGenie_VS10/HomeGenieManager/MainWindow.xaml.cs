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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


using WinForms = System.Windows.Forms;
using System.Threading;

using System.ServiceProcess;
using System.ComponentModel;
using System.ServiceModel;
using System.Configuration;

using HomeGenieManager.HomeGenie.WCF;
using OpenSource.UPnP;


namespace HomeGenieManager
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Single)]
    public partial class MainWindow : Window, IManagerCallback
    {
        private WinForms.NotifyIcon notifierIcon = new WinForms.NotifyIcon();
        private ServiceController serviceController = null;

        private InstanceContext instanceContext = null;

        private bool isAppRunning = true;

        private MenuItem startStopItem;

        public MainWindow()
        {
            InitializeComponent();
            //
            Initialize();
        }

        public void OnEventLogged(HomeGenie.WCF.LogEntry message, DateTime timestamp)
        {
            string logMessage = timestamp.ToString() + " " + timestamp.ToString() + " " + message.Domain + " " + message.Description + " " + message.Source + " " + message.Property + " " + message.Value;
            Console.WriteLine(logMessage);
            //log.Text = s + "\n" + log.Text;
            //
            if (message.Property == "Status.Level" || message.Property.StartsWith("Sensor."))
            {
                notifierIcon.BalloonTipTitle = "HomeGenie Message";
                notifierIcon.BalloonTipText = message.Description + "\n[" + message.Domain.Split('.')[1] + "] " + message.Source + " --> " + message.Property.Split('.')[1] + " = " + message.Value;
                notifierIcon.ShowBalloonTip(1000);
            }
        }

        private void Initialize()
        {
            this.Visibility = System.Windows.Visibility.Hidden;
            this.Left = System.Windows.SystemParameters.PrimaryScreenWidth - this.Width;
            this.Top = System.Windows.SystemParameters.PrimaryScreenHeight - 30 - this.Height;
            //
            serviceController = new ServiceController();
            notifierIcon.MouseDown += new WinForms.MouseEventHandler(_notifier_MouseDown);
            notifierIcon.Icon = HomeGenieManager.Properties.Resources.TrayIcon;
            notifierIcon.Visible = true;
            notifierIcon.Text = "HomeGenie Automation Server";
            //
            var menu = (ContextMenu)this.FindResource("NotifierContextMenu");
            startStopItem = (MenuItem)menu.Items[4];
            //
            serviceController.ServiceName = "HomeGenieService";
            var serviceChecker = new Thread(checkServiceStatus);
            serviceChecker.Start();
            //
            instanceContext = new InstanceContext(this);
        }

        private void checkServiceStatus()
        {
            //
            while (isAppRunning)
            {
                string status = "HomeGenie Service not installed on local host.";
                serviceController.Refresh();
                try
                {
                    status = "HomeGenie Service - " + serviceController.Status;
                    if (serviceController.Status != ServiceControllerStatus.Running)
                    {
                        if (serviceController.Status == ServiceControllerStatus.Stopped)
                        {
                            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new ThreadStart(() =>
                            {
                                startStopItem.Header = "Start Local Service";
                                startStopItem.IsEnabled = true;
                            }));
                        }
                        else
                        {
                            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new ThreadStart(() =>
                            {
                                //_startstopitem.Header = "Start Service";
                                startStopItem.IsEnabled = false;
                            }));
                        }
                    }
                    else
                    {
                        this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new ThreadStart(() =>
                        {
                            startStopItem.Header = "Stop Local Service";
                            startStopItem.IsEnabled = true;
                        }));
                    }
                }
                catch { }
                if (notifierIcon.Text != status)
                {
                    notifierIcon.BalloonTipTitle = "HomeGenie Message";
                    notifierIcon.BalloonTipText = status;
                    notifierIcon.ShowBalloonTip(1000);
                }
                notifierIcon.Text = status;
                Thread.Sleep(5000);
            }
        }


        private void _notifier_MouseDown(object sender, WinForms.MouseEventArgs e)
        {
            if (e.Button == WinForms.MouseButtons.Right)
            {
                var menu = (ContextMenu)this.FindResource("NotifierContextMenu");
                menu.IsOpen = true;
            }
        }


        private void MenuStartStopService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (startStopItem.Header.ToString().StartsWith("Stop"))
                {
                    serviceController.Stop();
                }
                else
                {
                    serviceController.Start();
                }
            }
            catch (Exception ex)
            {
                notifierIcon.BalloonTipTitle = "HomeGenie Message";
                notifierIcon.BalloonTipText = ex.Message;
                notifierIcon.ShowBalloonTip(3000);
            }
        }

        private void MenuOpenHomeGenie_Click(object sender, RoutedEventArgs e)
        {
            bool serviceUp = false;
            try
            {
                ManagerClient managerClient = null;
                if (serviceController != null && serviceController.Status == ServiceControllerStatus.Running)
                {
                    managerClient = new ManagerClient(instanceContext);
                    managerClient.Subscribe();
                    if (managerClient != null)
                    {
                        string port = managerClient.GetHttpServicePort().ToString();
                        System.Diagnostics.Process.Start("http://localhost:" + port + "/");
                        managerClient.Close();
                        serviceUp = true;
                    }
                }
            }
            catch (Exception ex)
            {
            }
            if (!serviceUp)
            {
                if ((App.Current as HomeGenieManager.App).UPnPDevices.Count > 0)
                {
                    System.Diagnostics.Process.Start((App.Current as HomeGenieManager.App).UPnPDevices[(App.Current as HomeGenieManager.App).UPnPDevices.Keys.ElementAt(0)].PresentationURL);
                }
                else
                {
                    System.Diagnostics.Process.Start("http://localhost/");
                }
                //MessageBox.Show("HomeGenie Service not responding.\nStart service first.");
            }
        }

        private void MenuSupport_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://generoso.info/homegenie");
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Quit();
        }

        public void Quit()
        {
            notifierIcon.Dispose();
            isAppRunning = false;
            Application.Current.Shutdown();
        }

    }
}
