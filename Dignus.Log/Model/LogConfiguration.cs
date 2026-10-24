// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.LogTarget.Interfaces;
using System;
using System.Collections.Generic;

namespace Dignus.Log.Model
{
    public class LogConfiguration
    {
        internal readonly Dictionary<string, LoggerConfig> _loggerConfigs = new Dictionary<string, LoggerConfig>();
        public LoggerConfig AddLogger(string loggerName, LoggerType loggerType)
        {
            loggerName = ToLoggerName(loggerName);
            if (_loggerConfigs.ContainsKey(loggerName) == true)
            {
                throw new InvalidOperationException($"The logger '{loggerName}' has already been added.");
            }
            var loggerConfig = new LoggerConfig(loggerName, loggerType);
            _loggerConfigs.Add(loggerName, loggerConfig);
            return loggerConfig;
        }
        public void AddLogTarget(string loggerName, LogLevel minLogLevel, ILogTarget logTarget)
        {
            loggerName = ToLoggerName(loggerName);
            if (_loggerConfigs.TryGetValue(loggerName, out LoggerConfig loggerConfig) == false)
            {
                loggerConfig = new LoggerConfig(loggerName, LoggerType.Async);
                _loggerConfigs.Add(loggerName, loggerConfig);
            }
            loggerConfig.AddLogTarget(minLogLevel, logTarget);
        }
        internal static string ToLoggerName(string loggerName)
        {
            if (string.IsNullOrEmpty(loggerName) == true)
            {
                return LogManager.DefaultLoggerName();
            }
            return loggerName;
        }
    }
}
