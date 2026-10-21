// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Model.Interfaces;
using Dignus.Log.Rule;
using System.Collections.Generic;

namespace Dignus.Log.Model
{
    public class LogConfiguration
    {
        public bool UseAsyncLogger { get; set; }

        internal readonly Dictionary<string, ILogTargetConfig> _logTargetConfigs = new Dictionary<string, ILogTargetConfig>();
        internal readonly Dictionary<string, LoggerConfig> _loggerConfigs = new Dictionary<string, LoggerConfig>();

        internal readonly Dictionary<string, LoggerRule> _loggerRules = new Dictionary<string, LoggerRule>();
        public void AddLogTargetConfig(ILogTargetConfig logTargetConfig)
        {
            _logTargetConfigs.Add(logTargetConfig.LoggerKey, logTargetConfig);
        }
        public IEnumerable<ILogTargetConfig> GetLogTargetConfigs()
        {
            return _logTargetConfigs.Values;
        }

        public void AddLoggerConfig(LoggerConfig loggerConfig)
        {
            _loggerConfigs.Add(loggerConfig.ConfigKey, loggerConfig);
        }
        public IEnumerable<LoggerConfig> GetLoggerConfigs()
        {
            return _loggerConfigs.Values;
        }
        public void AddLogRule(string loggerName, LoggerRule loggerRule)
        {
            _loggerRules.Add(loggerName, loggerRule);
        }
    }
}
