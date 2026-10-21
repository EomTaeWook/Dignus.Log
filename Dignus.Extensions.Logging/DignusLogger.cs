// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Extensions.Logging.Model;
using Dignus.Log;
using Microsoft.Extensions.Logging;
using System;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Dignus.Extensions.Logging
{
    internal class DignusLogger : Microsoft.Extensions.Logging.ILogger
    {
        private readonly DignusLoggerProvider _provider;
        private readonly Log.ILogger _logger;
        private readonly string _category;
        public DignusLogger(Log.ILogger logger, DignusLoggerProvider provider, string category)
        {
            _provider = provider;
            _logger = logger;
            _category = category;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (state == null)
            {
                return null;
            }
            return _provider.ScopeProvider.Push(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel) == false)
            {
                return;
            }
            string logMessage;
            if (formatter != null)
            {
                logMessage = formatter(state, exception);
            }
            else
            {
                logMessage = state.ToString();
            }

            if (string.IsNullOrEmpty(logMessage))
            {
                return;
            }
            string callerFilePath = _category;
            int callerLineNumber = 0;
            _provider.ScopeProvider.ForEachScope((scope, _) =>
            {
                if (scope is CallerInfo callerInfo)
                {
                    callerFilePath = callerInfo.FilePath;
                    callerLineNumber = callerInfo.LineNumber;
                }
            }, state);

            var logEvent = new CategorizedLogEvent(ToDignusLogLevel(logLevel), _category, logMessage, callerFilePath, callerLineNumber, _logger.Name);

            _logger.Write(logEvent);
        }

        private static Log.LogLevel ToDignusLogLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    return Dignus.Log.LogLevel.Debug;
                case LogLevel.Information:
                    return Dignus.Log.LogLevel.Info;
                case LogLevel.Warning:
                case LogLevel.Error:
                    return Dignus.Log.LogLevel.Error;
                case LogLevel.Critical:
                    return Dignus.Log.LogLevel.Fatal;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, $"unsupported LogLevel: {logLevel}");

            }
        }
    }
}
