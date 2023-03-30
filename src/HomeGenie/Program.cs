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
using System.Threading.Tasks;
using HomeGenie.Service;
using HomeGenie.Service.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MIG;

namespace HomeGenie
{
    class Program
    {
        private static IHost _serviceHost;
        private const string ServiceName = "HomeGenie";

        static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
#if !NETCOREAPP
            AppDomain.CurrentDomain.SetupInformation.ShadowCopyFiles = "true";
#endif
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);


#if NETCOREAPP
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                _serviceHost = Host.CreateDefaultBuilder(args)
                    .UseSystemd()
                    .ConfigureServices((hostContext, services) => { services.AddHostedService<ServiceWorker>(); })
                    .Build();
            }
#endif
            if (Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX)
            {
                _serviceHost = Host.CreateDefaultBuilder(args)
                    .UseWindowsService(options =>
                    {
                        options.ServiceName = ServiceName;
                    })
                    .ConfigureServices(services =>
                    {
                        services.AddHostedService<ServiceWorker>();
                    })
                    .Build();
            }
            
            if (_serviceHost == null)
            {
                _serviceHost = new LocalServiceHost(new ServiceWorker());
            }
            await _serviceHost
                .RunAsync();
        }

        internal static void Quit(bool restartService)
        {
            ServiceWorker._restart = restartService;
            Console.Write("HomeGenie is now exiting...\n");
            _serviceHost
                .StopAsync()
                .Wait();
            Environment.Exit(restartService ? 1 : 0);
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("\n\nProgram interrupted!\n");
            Quit(false);
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
                ServiceWorker._homegenie?.RaiseEvent(Domains.HomeGenie_System, logEntry);
            }
            catch
            {
                HomeGenieService.LogError(logEntry);
            }
        }
    }
}
