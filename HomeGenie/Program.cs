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
using System.IO;

using HomeGenie.Service;
using HomeGenie.Service.Constants;
using MIG;

namespace HomeGenie
{
    class Program
    {
        private static HomeGenieService _homegenie = null;
        private static bool _isrunning = true;
        private static bool _restart = false;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            AppDomain.CurrentDomain.SetupInformation.ShadowCopyFiles = "true";

            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);

            PostInstallCheck();
            
            _homegenie = new HomeGenieService();
            do { System.Threading.Thread.Sleep(2000); } while (_isrunning);
        }

        private static void PostInstallCheck()
        {
            string postInstallLock = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "postinstall.lock");
            if (File.Exists(postInstallLock))
            {
                // Move MIG interface plugins from root folder to lib/mig
                // TODO: find a better solution to this
                string[] migFiles =
                {
                    "MIG.HomeAutomation.dll",
                    "MIG.Protocols.dll",
                    "LibUsb.Common.dll",
                    "LibUsbDotNet.LibUsbDotNet.dll",
                    "XTenLib.dll",
                    "CM19Lib.dll",
                    "ZWaveLib.dll"
                };
                foreach (var f in migFiles)
                {
                    string source = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, f);
                    if (!File.Exists(source)) continue;
                    try
                    {
                        string dest = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lib", "mig", f);
                        if (File.Exists(dest)) File.Delete(dest);
                        File.Move(source, dest);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("{0}\n{1}\n", e.Message, e.StackTrace);
                    }
                }                
                // TODO: place any other post-install stuff here
                try
                {
                    File.Delete(postInstallLock);
                }
                catch (Exception e)
                {
                    Console.WriteLine("{0}\n{1}\n", e.Message, e.StackTrace);
                }
            }
        }

        internal static void Quit(bool restartService)
        {
            _restart = restartService;
            ShutDown();
            _isrunning = false;
        }

        private static void ShutDown()
        {
            Console.Write("HomeGenie is now exiting...\n");
            //
            if (_homegenie != null)
            {
                _homegenie.Stop();
                _homegenie = null;
            }
            //
            int exitCode = 0;
            if (_restart)
            {
                exitCode = 1;
                Console.Write("\n\n...RESTART!\n\n");
            }
            else
            {
                Console.Write("\n\n...QUIT!\n\n");
            }
            //
            Environment.Exit(exitCode);
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
                // try broadcast first (we don't want homegenie object to be passed, so use the domain string)
                _homegenie.RaiseEvent(Domains.HomeGenie_System, logEntry);
            }
            catch 
            {
                HomeGenieService.LogError(logEntry);
            }
        }

    }

}


