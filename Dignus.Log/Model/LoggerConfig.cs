// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.LogTarget.Interfaces;
using System.Collections.Generic;

namespace Dignus.Log.Model
{
    public class LoggerConfig
    {
        public string LoggerName { get; private set; }
        public LoggerType LoggerType { get; private set; } = LoggerType.Async;

        internal readonly List<LogTargetEntry> _logTargetEntries = new List<LogTargetEntry>();

        internal LoggerConfig(string loggerName, LoggerType loggerType)
        {
            LoggerName = loggerName;
            LoggerType = loggerType;
        }
        public LoggerConfig AddLogTarget(LogLevel minLogLevel, ILogTarget logTarget)
        {
            _logTargetEntries.Add(new LogTargetEntry(minLogLevel, logTarget));
            return this;
        }
        internal LoggerConfig Copy()
        {
            var loggerConfig = new LoggerConfig(LoggerName, LoggerType);
            loggerConfig._logTargetEntries.AddRange(_logTargetEntries);
            return loggerConfig;
        }
    }
}
