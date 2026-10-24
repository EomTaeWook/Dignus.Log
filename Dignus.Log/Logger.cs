// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Model;

namespace Dignus.Log
{
    public class Logger : ILogger
    {
        public string Name { get; private set; }

        internal TargetsByLevelDispatcher[] _logTargetsByLevel = new TargetsByLevelDispatcher[(int)LogLevel.Fatal + 1];
        private readonly object _syncObj = new object();
        private int _isDisposed;

        internal Logger(string name)
        {
            Name = name;
            for (LogLevel logLevel = LogLevel.Debug; logLevel <= LogLevel.Fatal; ++logLevel)
            {
                _logTargetsByLevel[(int)logLevel] = new TargetsByLevelDispatcher();
            }
        }
        public void Write(LogLevel logLevelType, string message, string callerFilePath, int callerLineNumber)
        {
            var logEvent = new LogEvent(logLevelType, message, callerFilePath, callerLineNumber, Name);
            Write(logEvent);
        }

        public void Write(LogEvent logEvent)
        {
            lock (_syncObj)
            {
                if (_isDisposed == 1)
                {
                    return;
                }

                _logTargetsByLevel[(int)logEvent.LogLevel].Write(logEvent);
            }
        }

        public void Dispose()
        {
            lock (_syncObj)
            {
                if (_isDisposed == 1)
                {
                    return;
                }
                _isDisposed = 1;
                TargetsByLevelDispatcher.DisposeTargets(_logTargetsByLevel);
            }
        }
    }
}
