/*
   Copyright 2012-2026 G-Labs (https://github.com/genielabs)

   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU Affero General Public License as
   published by the Free Software Foundation, either version 3 of the
   License, or (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU Affero General Public License for more details.

   You should have received a copy of the GNU Affero General Public License
   along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: https://homegenie.it
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using GLabs.Logging;
using MIG;

using HomeGenie.Service;
using HomeGenie.Service.Constants;
using HomeGenie.Service.Logging;

namespace HomeGenie
{
    class Program
    {
        public static bool StartBrowser = false;

        private static IHost _serviceHost;
        private const string ServiceName = "HomeGenie";
        private const string AppGuid = "02E944DC-EECB-480F-B6DE-D6D93522F19E";

        private static async Task Main(string[] args)
        {
            using (Mutex mutex = new Mutex(false, @"Global\" + AppGuid))
            {
                if (!mutex.WaitOne(0, false))
                {
                    // already running
                    try
                    {
                        string addressFile = "serviceaddress.txt";
                        if (File.Exists(addressFile))
                        {
                            var lines = File.ReadAllLines(addressFile);
                            var portLine = lines.FirstOrDefault(l => l.Contains("HG_SERVICE_PORT="));
                            if (portLine != null)
                            {
                                string port = portLine.Split('=')[1].Trim();
                                LaunchPwa($"http://localhost:{port}");
                            }
                        }
                    }
                    catch
                    {
                        // could not open file
                    }
                    return;
                }

                if (args.Contains("--start-browser"))
                {
                    StartBrowser = true;
                }

                AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

                Console.CancelKeyPress += Console_CancelKeyPress;

                var hostBuilder = Host.CreateDefaultBuilder(args);
                hostBuilder.ConfigureLogging((hostContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddCustomFileLogger();
                });

                hostBuilder.ConfigureServices((hostContext, services) =>
                {
                    // Dependency Injection
                    services.AddHostedService<ServiceWorker>();
                });


                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    hostBuilder.UseSystemd();
                }

                if (Environment.OSVersion.Platform != PlatformID.Unix &&
                    Environment.OSVersion.Platform != PlatformID.MacOSX)
                {
                    hostBuilder.UseWindowsService(options => { options.ServiceName = ServiceName; });
                }

                _serviceHost = hostBuilder.Build();

                var loggerFactory = _serviceHost.Services.GetRequiredService<ILoggerFactory>();
                LogManager.Initialize(loggerFactory);

                var startupLogger = LogManager.GetLogger("HomeGenie");
                startupLogger.Info("Host configured. Starting service...");

                await _serviceHost.RunAsync();
            }
        }

        internal static void Quit(bool restartService)
        {
            ServiceWorker.Restart = restartService;
            Console.Write("HomeGenie is now exiting...\n");
            _serviceHost
                .StopAsync()
                .Wait(60000);
            Environment.Exit(restartService ? 1 : 0);
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            // create a copy of config file before shutting down the app
            try
            {
                File.Copy("systemconfig.xml", "systemconfig.bak.xml",true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.WriteLine("\n\nProgram interrupted!\n");
            Quit(false);
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            try
            {
                while (ServiceWorker.HomeGenie != null)
                {
                    Thread.Sleep(1000);
                }
            } catch { /* ignored */ }
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            var logEntry = new MigEvent(
                Domains.HomeAutomation_HomeGenie,
                "Trapper",
                "Unhandled Exception",
                "Error.Exception",
                e.ExceptionObject.ToString()
            );
            try
            {
                // try broadcast first
                ServiceWorker.HomeGenie?.RaiseEvent(Domains.HomeGenie_System, logEntry);
            }
            catch
            {
                HomeGenieService.LogError(logEntry);
            }
        }

        internal static void LaunchPwa(string url)
        {
            var startInfo = new ProcessStartInfo
            {
                //Arguments = $"--app={url} --incognito",
                Arguments = $"--app={url}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: Chrome or Edge
                string[] paths = {
                    @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\Application\chrome.exe"),
                    @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
                };

                foreach (var path in paths)
                {
                    if (File.Exists(path))
                    {
                        startInfo.FileName = path;
                        new Process { StartInfo = startInfo }.Start();
                        return;
                    }
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: google-chrome or chromium
                try {
                    startInfo.FileName = "google-chrome";
                    new Process { StartInfo = startInfo }.Start();
                    return;
                } catch {
                    try
                    {
                        startInfo.FileName = "chromium";
                        new Process { StartInfo = startInfo }.Start();
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS
                string chromePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
                if (File.Exists(chromePath))
                {
                    startInfo.FileName = chromePath;
                    new Process { StartInfo = startInfo }.Start();
                    return;
                }
            }
            // Fallback
            OpenStandardBrowser(url);
        }

        private static void OpenStandardBrowser(string url)
        {
            try {
                Process.Start(url);
            } catch {
                // workaround on Linux/Mac (.NET Core/5+)
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    Process.Start("xdg-open", url);
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    Process.Start("open", url);
            }
        }
    }
}
