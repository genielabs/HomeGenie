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

namespace HomeGenie.Automation
{
    public static class ArduinoAppFactory
    {

        public static List<ProgramError>  CompileSketch(string sketchFileName, string sketchMakefile)
        {
            List<ProgramError> errors = new List<ProgramError>();

            string fileIno = Path.GetFileName(sketchFileName);
            // run make
            var processInfo = new ProcessStartInfo("make", "");
            processInfo.WorkingDirectory = Path.GetDirectoryName(sketchFileName);
            processInfo.RedirectStandardOutput = false;
            processInfo.RedirectStandardError = true;
            processInfo.UseShellExecute = false;
            processInfo.CreateNoWindow = true;
            using (Process process = Process.Start(processInfo))
            {
                using (StreamReader reader = process.StandardError)
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] lineParts = line.Split(':');
                        // TODO: here should parse errors and warnings
                        if (line.StartsWith(fileIno + ":") && lineParts.Length > 4)
                        {
                            int errorRow = 0;
                            int errorColumn = 0;
                            if (lineParts[3].Trim() == "error" && int.TryParse(lineParts[1], out errorRow) && int.TryParse(lineParts[2], out errorColumn))
                            {
                                errors.Add(new ProgramError() {
                                    Line = errorRow,
                                    Column = errorColumn,
                                    ErrorMessage = lineParts[3]+": "+lineParts[4],
                                    ErrorNumber = "1",
                                    CodeBlock = "CR"
                                });
                            }
                        }
                        else if (line.StartsWith("Makefile:") && lineParts.Length > 2)
                        {
                            int errorRow = 0;
                            if (int.TryParse(lineParts[1], out errorRow))
                            {
                                errors.Add(new ProgramError() {
                                    Line = errorRow,
                                    Column = 0,
                                    ErrorMessage = line,
                                    ErrorNumber = "1",
                                    CodeBlock = "TC"
                                });
                            }
                        }
                        Console.WriteLine(line);
                    }
                }
            }

            // TODO: Possibly add support for rt debugging and arduino output logging
            // TODO: Implement an "arduino-hg-interop" C library
            // NOTE: Issue "apt-get install arduino-mk" on the hosting platform to get needed tools for this task

            return errors;
        }
        
        public static void UploadSketch(string sketchDirectory)
        {
            string errorOutput = "";
            var processInfo = new ProcessStartInfo("make", "upload");
            processInfo.WorkingDirectory = sketchDirectory;
            processInfo.RedirectStandardOutput = false;
            processInfo.RedirectStandardError = true;
            processInfo.UseShellExecute = false;
            processInfo.CreateNoWindow = true;
            using (Process process = Process.Start(processInfo))
            {
                using (StreamReader reader = process.StandardError)
                {
                    while (!reader.EndOfStream)
                    {
                        errorOutput += reader.ReadLine();
                    }
                    Console.WriteLine(errorOutput);
                }
            }
            if (!String.IsNullOrEmpty(errorOutput))
            {
                throw(new IOException(errorOutput));
            }
        }

    }
}

