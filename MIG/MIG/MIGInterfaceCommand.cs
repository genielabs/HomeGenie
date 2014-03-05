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

namespace MIG
{
    public class MIGInterfaceCommand
    {
        private string[] options = new string[0];

        public string Domain { get; set; }
        public string NodeId { get; set; }
        public string Command { get; set; }
        //public string option { get; set; }
        //public string option1 { get; set; }

        public string Response { get; set; }
        public string OriginalRequest { get; set; }

        public MIGInterfaceCommand(string request)
        {
            OriginalRequest = request;
            try
            {
                var requests = request.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (requests.Length > 0)
                {
                    Domain = requests[0];
                    if (Domain == "html")
                    {
                        return;
                    }

                    if (requests.Length > 2)
                    {
                        NodeId = requests[1];
                        Command = requests[2];
                    }
                    //                option = string.Empty;
                    //                option1 = string.Empty;
                    if (requests.Length > 3)
                    {
                        //                    option = requests[3];
                        options = new string[requests.Length - 3];
                        Array.Copy(requests, 3, options, 0, requests.Length - 3);
                    }
                    if (requests.Length > 4)
                    {
                        //                    option1 = requests[4];
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine("\n\nError parsing interface command request (" + request + ") : " + ex.Message + "\n" + ex.StackTrace);
            }
            Response = null;
        }

        public string GetOption(int index)
        {
            var option = "";
            if (index < options.Length)
            {
                option = Uri.UnescapeDataString(options[index]);
            }
            //Console.ForegroundColor = ConsoleColor.DarkMagenta;
            //Console.WriteLine("OPTION " + index + " = " + option);
            //Console.ForegroundColor = ConsoleColor.White;
            return option;
        }

        public string OptionsString
        {
            get
            {
                var options = "";
                for (var o = 0; o < options.Length; o++)
                {
                    options += options[o] + "/";
                }
                return options;
            }
        }

    }
}

