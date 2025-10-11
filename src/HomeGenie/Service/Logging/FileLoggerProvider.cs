/*
   Copyright 2012-2025 G-Labs (https://github.com/genielabs)

   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU Affero General Public License as
   published by the Free Software Foundation, either version 3 of the
   License, or (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU Affero General Public License for more details.

   You should have received a copy of the GNU Affero General Public License
   along with this program.  If not, see <https://www.gnu.org/licenses/>.
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
