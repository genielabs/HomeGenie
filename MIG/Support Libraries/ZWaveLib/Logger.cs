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
using System.Text;

namespace ZWaveLib
{
    public enum LogLevel
    {
        INFO,
        WARNING,
        ERROR,
        DEBUG_IN,
        DEBUG_OUT,
        REPORT
    }

    public class Logger
    {
        public delegate void LogEventReceived(LogLevel level, string message);
        public static LogEventReceived LogEventReceivedCallback;

        static public void Log(LogLevel level, string message)
        {
            /*
            switch (level)
            {
                case LogLevel.INFO:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                case LogLevel.DEBUG_OUT:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                case LogLevel.DEBUG_IN:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case LogLevel.WARNING:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case LogLevel.ERROR:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogLevel.REPORT:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
            }
            */
            //Console.WriteLine(DateTime.Now.ToLongTimeString() + " " + level.ToString() + " " + message);

            //
            if (LogEventReceivedCallback != null) LogEventReceivedCallback(level, message);
        }

    }
}
