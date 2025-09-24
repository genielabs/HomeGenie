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
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace HomeGenie.Service.Logging
{
    public class FileLoggerProvider : ILoggerProvider
    {
        // Crea una sola istanza del tuo processore e la condivide tra tutti i logger
        private readonly FileLogProcessor _processor = new FileLogProcessor();
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new ConcurrentDictionary<string, FileLogger>();
        private readonly IOptionsMonitor<FileLoggerOptions> _options;

        public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> options)
        {
            _options = options;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _processor, _options));
        }

        public void Dispose()
        {
            _loggers.Clear();
            _processor.Dispose();
        }
    }
}
