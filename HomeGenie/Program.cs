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

using System.IO;
using System.Globalization;

using HomeGenie.Service;
using Raspberry;

using Newtonsoft.Json;
using System.Diagnostics;

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

            Console.OutputEncoding = Encoding.UTF8;
            /* Change current culture
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            */
            var os = Environment.OSVersion;
            var platform = os.Platform;

            // TODO: run "uname" to determine OS type
            if (platform == PlatformID.Unix)
            {

                var libusblink = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libusb-1.0.so");

                // RaspBerry Pi armel dependency check and needed symlink
                // TODO: check for armhf version
                if (File.Exists("/lib/arm-linux-gnueabi/libusb-1.0.so.0.1.0") && !File.Exists(libusblink))
                {
                    ShellCommand("ln", " -s \"/lib/arm-linux-gnueabi/libusb-1.0.so.0.1.0\" \"" + libusblink + "\"");
                }

                // Debian/Ubuntu 64bit dependency and needed symlink check
                if (File.Exists("/lib/x86_64-linux-gnu/libusb-1.0.so.0") && !File.Exists(libusblink))
                {
                    ShellCommand("ln", " -s \"/lib/x86_64-linux-gnu/libusb-1.0.so.0\" \"" + libusblink + "\"");
                }

                // lirc
                var liblirclink = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "liblirc_client.so");
                if (File.Exists("/usr/lib/liblirc_client.so") && !File.Exists(liblirclink))
                {
                    ShellCommand("ln", " -s \"/usr/lib/liblirc_client.so\" \"" + liblirclink + "\"");
                }
                else if (File.Exists("/usr/lib/liblirc_client.so.0") && !File.Exists(liblirclink))
                {
                    ShellCommand("ln", " -s \"/usr/lib/liblirc_client.so.0\" \"" + liblirclink + "\"");
                }

                // video 4 linux interop
                if (Raspberry.Board.Current.IsRaspberryPi)
                {
                    ShellCommand("cp", " -f \"" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "v4l/raspbian_libCameraCaptureV4L.so") + "\" \"" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libCameraCaptureV4L.so") + "\"");
                    if (!File.Exists("/root/.lircrc"))
                    {
                        var lircrc = "begin\n" +
                                        "        prog = homegenie\n" +
                                        "        button = KEY_1\n" +
                                        "        repeat = 3\n" +
                                        "        config = KEY_1\n" +
                                        "end\n";
                        try
                        {
                            File.WriteAllText("/root/.lircrc", lircrc);
                        }
                        catch { }
                    }
                    //
                    //if (File.Exists("/usr/lib/libgdiplus.so") && !File.Exists("/usr/local/lib/libgdiplus.so"))
                    //{
                    //    ShellCommand("ln", " -s \"/usr/lib/libgdiplus.so\" \"/usr/local/lib/libgdiplus.so\"");
                    //}
                }
                else // fallback (ubuntu and other 64bit debians)
                {
                    string v4lfile = "v4l/debian64_libCameraCaptureV4L.so.gd3";
                    if (!File.Exists("/usr/lib/x86_64-linux-gnu/libgd.so.3"))
                    {
                        v4lfile = "v4l/debian64_libCameraCaptureV4L.so";
                    }
                    ShellCommand(
                        "cp",
                        " -f \"" + Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            v4lfile
                        ) + "\" \"" + Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            "libCameraCaptureV4L.so"
                        ) + "\""
                    );
                }
            }
            //
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
            //
            AppDomain.CurrentDomain.SetupInformation.ShadowCopyFiles = "true";
            //
            _homegenie = new HomeGenieService();
            //
            do { System.Threading.Thread.Sleep(2000); } while (_isrunning);
            //
            System.Threading.Thread.Sleep(2000);
            //
            ShutDown();
        }

        internal static void Quit(bool restartService)
        {
            _restart = restartService;
            _isrunning = false;
            ShutDown();
        }

        private static void ShutDown()
        {
            Console.Write("HomeGenie is now exiting...");
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
            ShutDown();
            _isrunning = false;
        }

        private static void ShellCommand(string command, string args)
        {
            try
            {
                var processInfo = new ProcessStartInfo(command, args);
                processInfo.RedirectStandardOutput = false;
                processInfo.UseShellExecute = false;
                processInfo.CreateNoWindow = true;
                var process = new Process();
                process.StartInfo = processInfo;
                process.Start();
            }
            catch { }
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e) {
            HomeGenieService.LogEvent(new HomeGenie.Data.LogEntry() {
                Domain = "HomeAutomation.HomeGenie",
                Source = "UnhandledExceptionTrapper",
                Description = e.ExceptionObject.ToString(),
                Property = "HomeGenie.UnhandledException",
                Value = e.ExceptionObject.ToString()
            });
        }

    }
}


