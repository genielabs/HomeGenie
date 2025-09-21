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
 *     Project Homepage: https://homegenie.it
 */

using System;
using System.IO;
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
        private static IHost _serviceHost;
        private const string ServiceName = "HomeGenie";

        static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
#if NETFRAMEWORK
            // This is critical for runtime assembly loading and recompilation on Mono/.NET Framework.
            // It prevents file locks on dynamically loaded/compiled DLLs by loading them from a shadow copy.            
            AppDomain.CurrentDomain.SetupInformation.ShadowCopyFiles = "true";
#endif
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

#if !NETFRAMEWORK
    #if NET6_0_OR_GREATER
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                hostBuilder.UseSystemd();
            }
    #endif
            if (Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX)
            {
                hostBuilder.UseWindowsService(options =>
                {
                    options.ServiceName = ServiceName;
                });
            }
#endif

            _serviceHost = hostBuilder.Build();

            var loggerFactory = _serviceHost.Services.GetRequiredService<ILoggerFactory>();
            LogManager.Initialize(loggerFactory);

            var startupLogger = LogManager.GetLogger("HomeGenie");
            startupLogger.Info("Host configured. Starting service...");

            await _serviceHost.RunAsync();
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
    }
}
