// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Formatting;
using Dignus.Log.Model;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Dignus.Log
{
    public class Builder
    {
        private bool _isBuilt = false;
        internal bool IsBuilt => _isBuilt;
        private readonly Dictionary<string, LoggerConfig> _loggerConfigs = new Dictionary<string, LoggerConfig>();
        internal Builder()
        {
        }
        internal Builder Configure(LogConfiguration logConfiguration)
        {
            if (logConfiguration == null)
            {
                throw new ArgumentNullException(nameof(logConfiguration));
            }
            if (_isBuilt == true)
            {
                throw new InvalidOperationException("log builder has already been built.");
            }

            _loggerConfigs.Clear();
            foreach (var loggerConfig in logConfiguration._loggerConfigs.Values)
            {
                _loggerConfigs.Add(loggerConfig.LoggerName, loggerConfig.Copy());
            }

            return this;
        }
        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            LogManager.Dispose();
        }

        public void Build()
        {
            if (_isBuilt == true)
            {
                throw new InvalidOperationException("log builder has already been built.");
            }

            _isBuilt = true;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            CreateDefaultLogger();
        }
        private void CreateDefaultLogger()
        {
            LogManager.GetLogger(LogManager.DefaultLoggerName());
        }
        public void RegisterAssembly(Assembly assembly)
        {
            if (_isBuilt == true)
            {
                throw new InvalidOperationException("RegisterAssembly must be called before the log builder is built.");
            }
            FormatElements.RegisterFromAssembly(assembly);
        }

        internal IEnumerable<LogTargetEntry> GetLogTargetEntries(string loggerName)
        {
            if (_loggerConfigs.TryGetValue(loggerName, out LoggerConfig loggerConfig) == true)
            {
                return loggerConfig._logTargetEntries;
            }
            return Array.Empty<LogTargetEntry>();
        }
        internal LoggerType GetLoggerType(string loggerName)
        {
            if (_loggerConfigs.TryGetValue(loggerName, out LoggerConfig loggerConfig) == true)
            {
                return loggerConfig.LoggerType;
            }
            if (loggerName == LogManager.DefaultLoggerName())
            {
                return LoggerType.Async;
            }
            throw new InvalidOperationException($"The logger '{loggerName}' is not configured.");
        }
    }

    public static class LogBuilder
    {
        internal static readonly Builder _builder = new Builder();

        public static Builder Configuration(LogConfiguration logConfiguration)
        {
            return _builder.Configure(logConfiguration);
        }
        public static void Build()
        {
            _builder.Build();
        }
    }
}
