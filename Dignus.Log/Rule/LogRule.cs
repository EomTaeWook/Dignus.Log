// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Collections;
using Dignus.Log.LogTarget.Interfaces;
using System.Collections.Generic;

namespace Dignus.Log.Rule
{
    public class LoggerRule
    {
        public ArrayQueue<string> LoggerNames { get; private set; }
        public LogLevel MinLogLevel { get; private set; }
        public ILogTarget LogTarget { get; private set; }

        public LoggerRule(ILogTarget logTarget) : this(string.Empty, LogLevel.Debug, logTarget)
        {
        }

        public LoggerRule(LogLevel minLogLevel, ILogTarget logTarget) : this(string.Empty, minLogLevel, logTarget)
        {
        }
        public LoggerRule(string loggerName, LogLevel minLogLevel, ILogTarget logTarget)
        {
            if (string.IsNullOrEmpty(loggerName) == true)
            {
                loggerName = logTarget.GetType().Name;
            }
            InitInternal(new ArrayQueue<string>() { loggerName }, minLogLevel, logTarget);
        }
        public LoggerRule(ICollection<string> loggerNames, LogLevel minLogLevel, ILogTarget logTarget)
        {
            if (loggerNames == null)
            {
                loggerNames = new List<string>();
            }
            if (loggerNames.Count == 0)
            {
                loggerNames.Add(logTarget.GetType().Name);
            }
            var list = new ArrayQueue<string>();
            list.AddRange(loggerNames);
            InitInternal(list, minLogLevel, logTarget);
        }
        private void InitInternal(ArrayQueue<string> loggerNames, LogLevel minLogLevel, ILogTarget logTarget)
        {
            LoggerNames = loggerNames;
            MinLogLevel = minLogLevel;
            LogTarget = logTarget;
        }
    }
}
