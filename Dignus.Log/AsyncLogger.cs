// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Collections;
using Dignus.Log.Model;
using System.Threading;

namespace Dignus.Log
{
    public class AsyncLogger : ILogger
    {
        public string Name { get; private set; }

        internal TargetsByLevelDispatcher[] _logTargetsByLevel = new TargetsByLevelDispatcher[(int)LogLevel.Fatal + 1];
        private readonly MpscUnboundedQueue<LogEvent> _buffer = new MpscUnboundedQueue<LogEvent>();
        private readonly SemaphoreSlim _pendingEvents = new SemaphoreSlim(0);
        private readonly Thread _processingThread;
        private int _processingState;
        private int _isDisposed;

        public AsyncLogger() : this(null)
        {
        }
        internal AsyncLogger(string name)
        {
            Name = name;
            for (LogLevel logLevel = LogLevel.Debug; logLevel <= LogLevel.Fatal; ++logLevel)
            {
                _logTargetsByLevel[(int)logLevel] = new TargetsByLevelDispatcher();
            }
            _processingThread = new Thread(ProcessPendingEvents)
            {
                IsBackground = true,
                Name = "Dignus.Log.AsyncLogger"
            };
            _processingThread.Start();
        }
        public void Debug(string message, string callerFilePath, int callerLineNumber)
        {
            Write(LogLevel.Debug, message, callerFilePath, callerLineNumber);
        }
        public void Info(string message, string callerFilePath, int callerLineNumber)
        {
            Write(LogLevel.Info, message, callerFilePath, callerLineNumber);
        }

        public void Error(string message, string callerFilePath, int callerLineNumber)
        {
            Write(LogLevel.Error, message, callerFilePath, callerLineNumber);
        }
        public void Fatal(string message, string callerFilePath, int callerLineNumber)
        {
            Write(LogLevel.Fatal, message, callerFilePath, callerLineNumber);
        }
        public void Write(LogLevel logLevelType, string message, string callerFilePath, int callerLineNumber)
        {
            var logEvent = new LogEvent(logLevelType, message, callerFilePath, callerLineNumber, Name);
            Write(logEvent);
        }
        public void Write(LogEvent logEvent)
        {
            if (Volatile.Read(ref _isDisposed) == 1)
            {
                return;
            }

            _buffer.TryEnqueue(logEvent);

            if (Interlocked.CompareExchange(ref _processingState, 1, 0) != 0)
            {
                return;
            }

            _pendingEvents.Release();
        }
        private void ProcessPendingEvents()
        {
            while (true)
            {
                _pendingEvents.Wait();
                while (true)
                {
                    while (_buffer.TryDequeue(out LogEvent logEvent))
                    {
                        _logTargetsByLevel[(int)logEvent.LogLevel].Write(logEvent);
                    }

                    Interlocked.Exchange(ref _processingState, 0);

                    if (_buffer.TryPeek(out _) == false)
                    {
                        break;
                    }

                    if (Interlocked.CompareExchange(ref _processingState, 1, 0) != 0)
                    {
                        break;
                    }
                }
                if (Volatile.Read(ref _isDisposed) == 1)
                {
                    if (_buffer.TryPeek(out _) == false)
                    {
                        return;
                    }
                }
            }
        }
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
            {
                return;
            }
            _pendingEvents.Release();
            _processingThread.Join();

            try
            {
                for (int i = 0; i < _logTargetsByLevel.Length; ++i)
                {
                    _logTargetsByLevel[i].Dispose();
                }
            }
            finally
            {
                _pendingEvents.Dispose();
            }
        }
    }
}
