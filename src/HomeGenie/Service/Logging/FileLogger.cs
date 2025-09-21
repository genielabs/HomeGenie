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

using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Options;

namespace HomeGenie.Service.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly FileLogProcessor _processor;
        private readonly IOptionsMonitor<FileLoggerOptions> _options;

        // Il costruttore ora accetta anche le opzioni
        public FileLogger(string categoryName, FileLogProcessor processor, IOptionsMonitor<FileLoggerOptions> options)
        {
            _categoryName = categoryName;
            _processor = processor;
            _options = options;
        }

        public IDisposable BeginScope<TState>(TState state) => default;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
    
            var timestamp = DateTime.Now.ToString(_options.CurrentValue.TimestampFormat);
            var logLevelString = logLevel.ToString().ToUpper();
    
            var fullMessage = $"{timestamp}\t[{logLevelString,-11}]\t{_categoryName}\t{message}";
            if (exception != null)
            {
                fullMessage +=  "\t" + Environment.NewLine + exception.ToString();
            }
    
            _processor.EnqueueMessage(fullMessage);
        }
    }
}
