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

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace HomeGenie.Service.Logging
{
    public class FileLogProcessor : IDisposable
    {
        private const int MaxQueueCapacity = 10000;

        private readonly BlockingCollection<string> _messageQueue;
        private readonly Task _outputTask;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private int _messagesDroppedCount;

        private string _logPath;
        private FileStream _logStream;
        private StreamWriter _logWriter;
        private DateTime _lastRotated = DateTime.Now;
        private readonly int _maxLogAgeSeconds = (60 * 60 * 24) * 1;

        // --- Static control fields for runtime enable/disable ---
        private static volatile bool _isEnabled = true;
        private volatile bool _isDisposing = false;
        private static FileLogProcessor _instance; // A static reference to the single instance

        public FileLogProcessor()
        {
            _messageQueue = new BlockingCollection<string>(new ConcurrentQueue<string>(), MaxQueueCapacity);
            _cancellationTokenSource = new CancellationTokenSource();
            _instance = this; // Store the singleton instance for static access

            if (_isEnabled)
            {
                OpenLog();
            }

            _outputTask = Task.Run(ProcessLogQueueAsync);
        }

        /// <summary>
        /// Globally enables or disables the file logger at runtime.
        /// </summary>
        public static void SetEnabled(bool isEnabled)
        {
            _isEnabled = isEnabled;
            Console.WriteLine($"[INFO] File logging has been {(isEnabled ? "ENABLED" : "DISABLED")}.");

            // Use the static instance reference to open/close the log file
            if (_instance != null)
            {
                if (isEnabled && !_instance.IsLogEnabled)
                {
                    _instance.OpenLog();
                }
                else if (!isEnabled && _instance.IsLogEnabled)
                {
                    _instance.CloseLog();
                }
            }
        }

        /// <summary>
        /// Flushes the log queue to the file.
        /// Blocks until the current queue is empty.
        /// </summary>
        public static void FlushLog()
        {
            // Use the static instance reference
            _instance?.FlushAndWait();
        }

        /// <summary>
        /// Gets the full path to the current log file.
        /// </summary>
        public static string GetLogPath()
        {
            return _instance?._logPath;
        }

        /// <summary>
        /// A blocking method that waits for the queue to empty.
        /// </summary>
        private void FlushAndWait()
        {
            while (_messageQueue.Count > 0)
            {
                Thread.Sleep(50);
            }
            Flush();
        }

        public void EnqueueMessage(string message)
        {
            // Check the static enabled flag first
            if (!_isEnabled || _isDisposing)
            {
                return;
            }

            if (!_messageQueue.TryAdd(message))
            {
                Interlocked.Increment(ref _messagesDroppedCount);
            }
        }

        private async Task ProcessLogQueueAsync()
        {
            try
            {
                foreach (var message in _messageQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
                {
                    // Although we check in EnqueueMessage, an extra check here prevents
                    // writing if logging was disabled while the message was in the queue.
                    if (!_isEnabled || !IsLogEnabled) continue;

                    CheckForRotation();

                    int droppedCount = Interlocked.Exchange(ref _messagesDroppedCount, 0);
                    if (droppedCount > 0)
                    {
                        await WriteMessageAsync($"[{DateTime.Now:s}] [WARN] [FileLogProcessor] Log queue capacity reached. {droppedCount} message(s) were dropped.");
                    }

                    await WriteMessageAsync(message);
                }
            }
            catch (OperationCanceledException) { /* Expected */ }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL] FileLogProcessor failed: {ex}");
            }
            finally
            {
                Flush();
            }
        }

        private async Task WriteMessageAsync(string message)
        {
            if (IsLogEnabled)
            {
                await _logWriter.WriteLineAsync(message);
            }
        }

        private void Flush()
        {
            if (IsLogEnabled)
            {
                try { _logWriter?.Flush(); }
                catch (Exception ex) { Console.WriteLine($"[ERROR] LogWriter could not flush log: {ex.Message}"); }
            }
        }

        #region Log File Handling

        private void CheckForRotation()
        {
            var logAge = DateTime.Now - _lastRotated;
            if (logAge.TotalSeconds >= _maxLogAgeSeconds)
            {
                _lastRotated = DateTime.Now;
                string logFileBackup = _logPath + ".bak";

                CloseLog();

                try
                {
                    if (File.Exists(logFileBackup)) File.Delete(logFileBackup);
                    if (File.Exists(_logPath)) File.Move(_logPath, logFileBackup);
                }
                catch (Exception e) { Console.WriteLine($"[ERROR] LogWriter could not move old log: {e.Message}"); }

                OpenLog();
            }
        }

        public void OpenLog()
        {
            CloseLog();

            var assembly = Assembly.GetExecutingAssembly();
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
            string logFile = (assembly.ManifestModule.Name ?? "app")
                .ToLower()
                .Replace(".exe", ".log")
                .Replace(".dll", ".log");
            _logPath = Path.Combine(logDir, logFile);

            try
            {
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                _logStream = new FileStream(_logPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                _logWriter = new StreamWriter(_logStream) { AutoFlush = true };
                _logWriter.WriteLine("#Start-Date: " + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz"));
                _logWriter.WriteLine("#Fields: datetime\tlevel\tcategory\tsource-domain\tsource-id\tdescription\tproperty\tvalue\n");
            }
            catch (Exception ex) { Console.WriteLine($"[FATAL] Could not open log file at {_logPath}: {ex}"); }
        }

        public void CloseLog()
        {
            if (IsLogEnabled)
            {
                try
                {
                    _logWriter?.Dispose();
                    _logStream?.Dispose();
                }
                catch { /* Ignore */ }
                finally
                {
                    _logWriter = null;
                    _logStream = null;
                }
            }
        }

        public bool IsLogEnabled => _logWriter != null;

        #endregion

        public void Dispose()
        {
            _isDisposing = true;
            _messageQueue.CompleteAdding();
            try
            {
                _outputTask.Wait(2000);
            }
            catch (Exception) { /* Ignore exceptions during shutdown */ }
            CloseLog();
            _cancellationTokenSource.Dispose();
            _messageQueue.Dispose();
            _instance = null;
        }
    }
}
