// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Collections;
using Dignus.Log.Model;
using System.Threading;

namespace Dignus.Log
{
    public class Logger
    {
        public string Name { get; private set; }

        internal TargetsByLevelDispatcher[] _logTargetsByLevel = new TargetsByLevelDispatcher[(int)LogLevel.Fatal + 1];
        private readonly MpscUnboundedQueue<LogEvent> _buffer = new MpscUnboundedQueue<LogEvent>();
        private int _processingState;
        private int _isDisposed;
        internal static Logger MakeLogger(string name)
        {
            return new Logger()
            {
                Name = name
            };
        }
        public Logger()
        {
            for (LogLevel e = LogLevel.Debug; e <= LogLevel.Fatal; ++e)
            {
                _logTargetsByLevel[(int)e] = new TargetsByLevelDispatcher();
            }
        }
        public void Info(in string message, in string callerFilePath, in int callerLineNumber)
        {
            Enqueue(in message, in callerFilePath, in callerLineNumber, LogLevel.Info);
        }
        public void Debug(in string message, in string callerFilePath, in int callerLineNumber)
        {
            Enqueue(in message, in callerFilePath, in callerLineNumber, LogLevel.Debug);
        }
        public void Error(in string message, in string callerFilePath, in int callerLineNumber)
        {
            Enqueue(in message, in callerFilePath, in callerLineNumber, LogLevel.Error);
        }
        public void Fatal(in string message, in string callerFilePath, in int callerLineNumber)
        {
            Enqueue(in message, in callerFilePath, in callerLineNumber, LogLevel.Fatal);
        }

        internal void Enqueue(LogEvent logEvent)
        {
            if (_isDisposed == 1)
            {
                return;
            }

            _buffer.TryEnqueue(logEvent);

            if (Interlocked.CompareExchange(ref _processingState, 1, 0) != 0)
            {
                return;
            }

            while (true)
            {
                try
                {
                    while (_buffer.TryDequeue(out LogEvent item))
                    {
                        _logTargetsByLevel[(int)item.LogLevel].Write(item);
                    }
                }
                catch
                {
                    Interlocked.Exchange(ref _processingState, 0);
                    throw;
                }

                Interlocked.Exchange(ref _processingState, 0);
                if (_buffer.TryPeek(out _) == false)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref _processingState, 1, 0) != 0)
                {
                    return;
                }
            }
        }
        private void Enqueue(in string message, in string callerFilePath, in int callerLineNumber, in LogLevel logLevelType)
        {
            var logEvent = new LogEvent(in logLevelType, in message, in callerFilePath, in callerLineNumber, Name);
            Enqueue(logEvent);
        }
        internal void Dispose()
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
            {
                return;
            }
            while (Interlocked.CompareExchange(ref _processingState, 1, 0) != 0)
            {
                Thread.Yield();
            }

            try
            {
                while (_buffer.TryDequeue(out LogEvent item))
                {
                    _logTargetsByLevel[(int)item.LogLevel].Write(item);
                }

                for (int i = 0; i < _logTargetsByLevel.Length; ++i)
                {
                    _logTargetsByLevel[i].Dispose();
                }
            }
            finally
            {
                Interlocked.Exchange(ref _processingState, 0);
            }
        }
    }
}
