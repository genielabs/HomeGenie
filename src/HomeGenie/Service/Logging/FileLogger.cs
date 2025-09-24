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
