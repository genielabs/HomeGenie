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
        private WinForms.NotifyIcon _notifiericon = new WinForms.NotifyIcon();
        private ServiceController _servicecontroller = null;
        
        private InstanceContext _instancecontext = null;

        private bool _apprunning = true;
        
        private MenuItem _startstopitem;

        public MainWindow()
        {
            InitializeComponent();
            //
            _initialize();
        }

        public void OnEventLogged(HomeGenie.WCF.LogEntry message, DateTime timestamp)
        {
            string s = timestamp.ToString() + " " + timestamp.ToString() + " " + message.Domain + " " + message.Description + " " + message.Source + " " + message.Property + " " + message.Value;
            Console.WriteLine(s);
            //log.Text = s + "\n" + log.Text;
            //
            if (message.Property == "Status.Level" || message.Property.StartsWith("Sensor."))
            {
                _notifiericon.BalloonTipTitle = "HomeGenie Message";
                _notifiericon.BalloonTipText = message.Description + "\n[" + message.Domain.Split('.')[1] + "] " + message.Source + " --> " + message.Property.Split('.')[1] + " = " + message.Value;
                _notifiericon.ShowBalloonTip(1000);
            }
        }

        private void _initialize()
        {
            this.Visibility = System.Windows.Visibility.Hidden;
            this.Left = System.Windows.SystemParameters.PrimaryScreenWidth - this.Width;
            this.Top = System.Windows.SystemParameters.PrimaryScreenHeight - 30 - this.Height;
            //
            _servicecontroller = new ServiceController();
            _notifiericon.MouseDown += new WinForms.MouseEventHandler(_notifier_MouseDown);
            _notifiericon.Icon = HomeGenieManager.Properties.Resources.TrayIcon;
            _notifiericon.Visible = true;
            _notifiericon.Text = "HomeGenie Automation Server";
            //
            ContextMenu menu = (ContextMenu)this.FindResource("NotifierContextMenu");
            _startstopitem = (MenuItem)menu.Items[4];
            //
            _servicecontroller.ServiceName = "HomeGenieService";
            Thread _servicechecker = new Thread(_checkServiceStatus);
            _servicechecker.Start();
            //
            _instancecontext = new InstanceContext(this);
        }

        private void _checkServiceStatus()
        {
            //
            while (_apprunning)
            {
                string status = "HomeGenie Service not installed on local host.";
                _servicecontroller.Refresh();
                try
                {
                    status = "HomeGenie Service - " + _servicecontroller.Status;
                    if (_servicecontroller.Status != ServiceControllerStatus.Running)
                    {
                        if (_servicecontroller.Status == ServiceControllerStatus.Stopped)
                        {
                            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new ThreadStart(() =>
                            {
                                _startstopitem.Header = "Start Local Service";
                                _startstopitem.IsEnabled = true;
                            }));
                        }
                        else
                        {
                            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new ThreadStart(() =>
                            {
                                //_startstopitem.Header = "Start Service";
                                _startstopitem.IsEnabled = false;
                            }));
                        }
                    }
                    else
                    {
                        this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new ThreadStart(() =>
                        {
                            _startstopitem.Header = "Stop Local Service";
                            _startstopitem.IsEnabled = true;
                        }));
                    }
                }
                catch { }
                if (_notifiericon.Text != status)
                {
                    _notifiericon.BalloonTipTitle = "HomeGenie Message";
                    _notifiericon.BalloonTipText = status;
                    _notifiericon.ShowBalloonTip(1000);
                }
                _notifiericon.Text = status;
                Thread.Sleep(5000);
            }
        }


        private void _notifier_MouseDown(object sender, WinForms.MouseEventArgs e)
        {
            if (e.Button == WinForms.MouseButtons.Right)
            {
                ContextMenu menu = (ContextMenu)this.FindResource("NotifierContextMenu");
                menu.IsOpen = true;
            }
        }


        private void MenuStartStopService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_startstopitem.Header.ToString().StartsWith("Stop"))
                {
                    _servicecontroller.Stop();
                }
                else
                {
                    _servicecontroller.Start();
                }
            } 
            catch (Exception ex)
            {
                _notifiericon.BalloonTipTitle = "HomeGenie Message";
                _notifiericon.BalloonTipText = ex.Message;
                _notifiericon.ShowBalloonTip(3000);
            }
        }

        private void MenuOpenHomeGenie_Click(object sender, RoutedEventArgs e)
        {
            bool serviceup = false;
            try
            {
                ManagerClient managerclient = null;
                if (_servicecontroller != null && _servicecontroller.Status == ServiceControllerStatus.Running)
                {
                    managerclient = new ManagerClient(_instancecontext);
                    managerclient.Subscribe();
                    if (managerclient != null)
                    {
                        string port = managerclient.GetHttpServicePort().ToString();
                        System.Diagnostics.Process.Start("http://localhost:" + port + "/");
                        managerclient.Close();
                        serviceup = true;
                    }
                }
            }
            catch (Exception ex)
            {
            }
            if (!serviceup)
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
            _notifiericon.Dispose();
            _apprunning = false;
            Application.Current.Shutdown();
        }

    }
}
