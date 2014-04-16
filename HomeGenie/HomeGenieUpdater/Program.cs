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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HomeGenieUpdater
{
    class Program
    {
        static void Main(string[] args)
        {
            var os = Environment.OSVersion;
            var platformId = os.Platform;

            bool restart = false;
            if (args.Length > 1)
            {
                if (args[1] == "-r")
                {
                    restart = true;
                }
            }
            else if (args.Length > 0)
            {
                if (args[0] == "-r")
                {
                    restart = true;
                }
            }

            Console.WriteLine("HomeGenie Updater started.");
            Console.Write("Please wait");
            for (int i = 0; i < 5; i++)
            {
                Console.Write(".");
                Thread.Sleep(1000);
            }

            if (Directory.Exists(Path.Combine("_update", "files", "HomeGenie")))
            {
                Console.WriteLine("\nCopying new files...");
                foreach (string file in Directory.EnumerateFiles(Path.Combine("_update", "files", "HomeGenie"), "*", SearchOption.AllDirectories))
                {
                    string destdir = Path.GetDirectoryName(file).Replace(Path.Combine("_update", "files", "HomeGenie"), "");
                    if (destdir != "" && !Directory.Exists(destdir)) Directory.CreateDirectory(destdir);
                    string destfile = Path.Combine(destdir, Path.GetFileName(file)).TrimStart(Directory.GetDirectoryRoot(Directory.GetCurrentDirectory()).ToArray()).TrimStart('/');
                    // we cannot overwrite HomeGenieUpdate while it's running
                    if (destfile.EndsWith("HomeGenieUpdater.exe"))
                    {
                        destfile = destfile.Replace("HomeGenieUpdater.exe", "HomeGenieUpdaterNew.exe");
                    }
                    Console.WriteLine("+ " + destfile);
                    try
                    {
                        File.Copy(file, destfile, true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("! Error copying file '" + destfile + "'");
                    }
                }
            }

            for (int i = 0; i < 5; i++)
            {
                Console.Write(".");
                Thread.Sleep(1000);
            }
            Console.WriteLine("\nUpdate completed!");

            if (restart)
            {

                // TODO: run "uname" to determine OS type
                if (platformId == PlatformID.Win32NT)
                {
                    try
                    {
                        ServiceController service = new ServiceController("HomeGenieService");
                        service.Start();
                    }
                    catch { StartHomeGenie(); }
                }
                else if (platformId == PlatformID.Win32Windows)
                {
                    StartHomeGenie();
                }
                else
                {
                    StartMonoHomeGenie();
                }

            }

        }

        private static void StartHomeGenie()
        {
            var homegenie = new Process();
            homegenie.StartInfo.FileName = "HomeGenie.exe";
            homegenie.StartInfo.Arguments = "-u";
            //updater.StartInfo.UseShellExecute = true;
            homegenie.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            //updater.StartInfo.RedirectStandardOutput = true;
            homegenie.Start();
        }

        private static void StartMonoHomeGenie()
        {
            var homegenie = new Process();
            homegenie.StartInfo.FileName = "mono";
            homegenie.StartInfo.Arguments = "HomeGenie.exe -u";
            homegenie.StartInfo.UseShellExecute = false;
            homegenie.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            //updater.StartInfo.RedirectStandardOutput = true;
            homegenie.Start();
        }
    }
}
