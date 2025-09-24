/*
   Copyright 2012-2025 G-Labs (https://github.com/genielabs)

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: https://homegenie.it
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HomeGenie.Service;
using Iot.Device.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MIG;

namespace HomeGenie
{
    public class ServiceWorker : BackgroundService
    {
        private readonly ILogger logger;
        internal static HomeGenieService HomeGenie;
        internal static volatile bool Restart;

        public ServiceWorker()
        {
            logger = MigService.Log.GetCurrentClassLogger();
        }

        public ServiceWorker(ILogger<ServiceWorker> logger)
        {
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(ShutDown);
            logger.LogInformation("HomeGenie service running at: {time}", DateTimeOffset.UtcNow);
            bool rebuildPrograms = PostInstallCheck();
            HomeGenie = new HomeGenieService(rebuildPrograms);
            while (!stoppingToken.IsCancellationRequested)
            {
                // service is running in the background
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            Exit();
        }

        private void ShutDown()
        {
            // Cleanup here
            if (HomeGenie != null)
            {
                logger.LogInformation("HomeGenie service stopping at: {time}", DateTimeOffset.UtcNow);
                HomeGenie.Stop();
                HomeGenie = null;
                logger.LogInformation("HomeGenie service stopped at: {time}", DateTimeOffset.UtcNow);
            }
        }

        private void Exit()
        {
            // set exit code to -1 if restart was requested
            int exitCode = 0;
            if (Restart)
            {
                exitCode = 1;
                Console.Write("\n\n...RESTART!\n\n");
            }
            else
            {
                Console.Write("\n\n...QUIT!\n\n");
            }
            Environment.Exit(exitCode);
        }

        private static bool PostInstallCheck()
        {
            bool firstTimeInstall = false;
            string postInstallLock = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "postinstall.lock");
            if (File.Exists(postInstallLock))
            {
                firstTimeInstall = true;
                //
                // NOTE: place any other post-install stuff here
                //
                try
                {
                    File.Delete(postInstallLock);
                }
                catch (Exception e)
                {
                    Console.WriteLine("{0}\n{1}\n", e.Message, e.StackTrace);
                }
            }
            return firstTimeInstall;
        }
    }

    class LocalServiceHost : IHost
    {
        private readonly BackgroundService service;
        private readonly LocalServiceProvider localServiceProvider = new LocalServiceProvider();
        public LocalServiceHost(BackgroundService backgroundService)
        {
            service = backgroundService;
        }

        public void Dispose()
        {
        }

        public Task StartAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return service.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return service.StopAsync(cancellationToken);
        }

        public IServiceProvider Services
        {
            get
            {
                return localServiceProvider;
            }
        }
    }

    class LocalServiceProvider : IServiceProvider
    {
        private static readonly LocalServiceLifetime DummyServiceProvider = new LocalServiceLifetime();

        public object GetService(Type serviceType)
        {
            return DummyServiceProvider;
        }
    }

    class LocalServiceLifetime : IHostApplicationLifetime
    {
        public void StopApplication()
        {
            throw new NotImplementedException();
        }

        public CancellationToken ApplicationStarted { get; }
        public CancellationToken ApplicationStopping { get; }
        public CancellationToken ApplicationStopped { get; }
    }
}
