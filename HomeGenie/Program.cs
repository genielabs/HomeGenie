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

namespace HomeGenie
{
    class Program
    {
        private static HomeGenieService _homegenie = null;
        private static bool _isrunning = true;
        private static bool _startupdater = false;

        static void Main (string[] args)
		{

            Console.OutputEncoding = Encoding.UTF8;
            /* Change current culture
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            */
			OperatingSystem os = Environment.OSVersion;
			PlatformID pid = os.Platform;

			// TODO: run "uname" to determine OS type
			if (pid == PlatformID.Unix) {

				string libusblink = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "libusb-1.0.so");

                // RaspBerry Pi armel dependency check and needed symlink
                // TODO: check for armhf version
				if (File.Exists ("/lib/arm-linux-gnueabi/libusb-1.0.so.0.1.0") && !File.Exists (libusblink)) {
					ShellCommand ("ln", " -s \"/lib/arm-linux-gnueabi/libusb-1.0.so.0.1.0\" \"" + libusblink + "\"");
				}

				// Debian/Ubuntu 64bit dependency and needed symlink check
				if (File.Exists ("/lib/x86_64-linux-gnu/libusb-1.0.so.0") && !File.Exists (libusblink)) {
					ShellCommand ("ln", " -s \"/lib/x86_64-linux-gnu/libusb-1.0.so.0\" \"" + libusblink + "\"");
				}

                // lirc
                string liblirclink = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "liblirc_client.so");
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
                        string lircrc = "begin\n" +
                                        "        prog = homegenie\n" +
                                        "        button = KEY_1\n" +
                                        "        repeat = 3\n" +
                                        "        config = KEY_1\n" +
                                        "end\n";
                        try
                        {
                            File.WriteAllText("/root/.lircrc", lircrc);
                        }
                        catch (Exception ex) { }
                    }
                    //
                    //if (File.Exists("/usr/lib/libgdiplus.so") && !File.Exists("/usr/local/lib/libgdiplus.so"))
                    //{
                    //    ShellCommand("ln", " -s \"/usr/lib/libgdiplus.so\" \"/usr/local/lib/libgdiplus.so\"");
                    //}
                }
                else // fallback (ubuntu and other 64bit debians)
                {
                    ShellCommand("cp", " -f \"" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "v4l/debian64_libCameraCaptureV4L.so") + "\" \"" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libCameraCaptureV4L.so") + "\"");
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
            _shutdown();
        }

        internal static void Quit(bool startupdater)
        {
            _startupdater = startupdater;
            _isrunning = false;
            _shutdown();
        }

        static void _shutdown()
        {
            Console.Write("HomeGenie is now exiting...");
            //
            if (_homegenie != null)
            {
                _homegenie.Stop();
                _homegenie = null;
            }
            //
            Console.Write(" QUIT!\n\n");
            //
            if (_startupdater)
            {
                Utility.StartUpdater(true);
            }
            //
            System.Environment.Exit(0);
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("\n\nProgram interrupted!\n");
            _shutdown();
            _isrunning = false;
        }

		static void ShellCommand(string command, string args)
		{
			try
			{
				System.Diagnostics.ProcessStartInfo psinfo = new System.Diagnostics.ProcessStartInfo(command, args);
				psinfo.RedirectStandardOutput = false;
				psinfo.UseShellExecute = false;
				psinfo.CreateNoWindow = true;
				System.Diagnostics.Process process = new System.Diagnostics.Process();
				process.StartInfo = psinfo;
				process.Start();
			}
			catch { }
		}

    }
}


