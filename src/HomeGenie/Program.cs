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

        static void DeployFiles(string inputFolder, string outputFolder)
        {
            //var destinationFolder = AppDomain.CurrentDomain.BaseDirectory;
            if (Directory.Exists(inputFolder))
            {
                //LogMessage("= Copying new files...");
                foreach (string file in Directory.EnumerateFiles(inputFolder, "*", SearchOption.AllDirectories))
                {
                    string destinationFolder = Path.Combine(outputFolder, Path.GetDirectoryName(file).Replace(inputFolder, "").TrimStart('/').TrimStart('\\'));
                    string destinationFile = Path.Combine(destinationFolder, Path.GetFileName(file));
                    if (!String.IsNullOrWhiteSpace(destinationFolder) && !Directory.Exists(destinationFolder))
                    {
                        Directory.CreateDirectory(destinationFolder);
                    }
                    var sourceFile = new FileInfo(file);
                    var destFile = new FileInfo(destinationFile);
                    if (destFile.Exists)
                    {
                        if (sourceFile.LastWriteTime > destFile.LastWriteTime)
                        {
                            Console.WriteLine("Updating {0}", destinationFile);
                            // now you can safely overwrite it
                            sourceFile.CopyTo(destFile.FullName, true);
                        }
                        else
                        {
                            //Console.WriteLine("Skipping {0}", destinationFile);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Copying {0}", destinationFile);
                        File.Copy(file, destinationFile);
                    }
                }
            }

        }

        static void Main(string[] args)
        {

            // TODO: check CLI args
            // TODO: if first argument is "deploy" then copy common files to the build output directory

            if (args != null && args.Length == 1 && args[0] == "--post-build")
            {

#if NETCOREAPP
                var assetsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "assets", "build");
#else
                var assetsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "assets", "build");
#endif
                DeployFiles(Path.Combine(assetsFolder, "all"), AppDomain.CurrentDomain.BaseDirectory);
#if !NETCOREAPP
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    DeployFiles(Path.Combine(assetsFolder, "linux"), AppDomain.CurrentDomain.BaseDirectory);
                }
                else
                {
                    DeployFiles(Path.Combine(assetsFolder, "windows"), AppDomain.CurrentDomain.BaseDirectory);
                }
#endif
            }

            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
#if !NETCOREAPP
            AppDomain.CurrentDomain.SetupInformation.ShadowCopyFiles = "true";
#endif
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);

            bool rebuildPrograms = PostInstallCheck();

            _homegenie = new HomeGenieService(rebuildPrograms);
            do { System.Threading.Thread.Sleep(2000); } while (_isrunning);
        }

        private static bool PostInstallCheck()
        {
            bool firstTimeInstall = false;
            string postInstallLock = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "postinstall.lock");
            if (File.Exists(postInstallLock))
            {
                firstTimeInstall = true;
                // Move MIG interface plugins from root folder to lib/mig
                // TODO: find a better solution to this
                string[] migFiles =
                {
                    "MIG.HomeAutomation.dll",
                    "MIG.Protocols.dll",
                    "libusb-1.0.so",
                    "LibUsbDotNet.dll",
                    "XTenLib.dll",
                    "CM19Lib.dll",
                    "ZWaveLib.dll"
                };
                string migFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lib", "mig");
                if (!Directory.Exists(migFolder))
                {
                    Directory.CreateDirectory(migFolder);
                }
                foreach (var f in migFiles)
                {
                    string source = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, f);
                    if (!File.Exists(source)) continue;
                    try
                    {
                        string dest = Path.Combine(migFolder, f);
                        if (File.Exists(dest)) File.Delete(dest);
#if NETCOREAPP
                        File.Copy(source, dest);
#else
                        File.Move(source, dest);
#endif
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("{0}\n{1}\n", e.Message, e.StackTrace);
                    }
                }
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
