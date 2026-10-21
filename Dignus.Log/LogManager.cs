// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Dignus.Log
{
    public static class LogManager
    {
        private static readonly ConcurrentDictionary<string, ILogger> _loggerContainer = new ConcurrentDictionary<string, ILogger>();

        public static ILogger GetLogger(string name)
        {
            if (_loggerContainer.TryGetValue(name, out ILogger logger) == false)
            {
                throw new Exception($"The logger '{name}' has not been registered.");
            }
            return logger;
        }

        private static Logger CreateLogger(string loggerName)
        {
            var logger = new Logger(loggerName);
            ConfigureTargets(logger.Name, logger._logTargetsByLevel);
            return logger;
        }

        private static AsyncLogger CreateAsyncLogger(string loggerName)
        {
            var logger = new AsyncLogger(loggerName);
            ConfigureTargets(logger.Name, logger._logTargetsByLevel);
            return logger;
        }

        private static void ConfigureTargets(string loggerName, TargetsByLevelDispatcher[] logTargetsByLevel)
        {
            foreach (var loggerRule in LogBuilder._builder.GetAllRules())
            {
                if (loggerRule.LoggerNames.Contains(loggerName))
                {
                    for (LogLevel logLevel = loggerRule.MinLogLevel; logLevel <= LogLevel.Fatal; ++logLevel)
                    {
                        logTargetsByLevel[(int)logLevel].AddTarget(loggerRule.LogTarget);
                    }
                }
            }
        }

        public static void RegisterLogger(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (_loggerContainer.TryAdd(logger.Name, logger) == false)
            {
                throw new InvalidOperationException($"The logger '{logger.Name}' has already been registered.");
            }
        }

        internal static void RegisterDefaultLogger(bool useAsyncLogger)
        {
            string defaultLoggerName = DefaultLoggerName();
            if (_loggerContainer.ContainsKey(defaultLoggerName))
            {
                return;
            }

            ILogger logger;
            if (useAsyncLogger)
            {
                logger = CreateAsyncLogger(defaultLoggerName);
            }
            else
            {
                logger = CreateLogger(defaultLoggerName);
            }

            RegisterLogger(logger);
        }

        public static ILogger GetDefaultLogger()
        {
            if (_loggerContainer.TryGetValue(DefaultLoggerName(), out ILogger logger))
            {
                return logger;
            }
            throw new InvalidOperationException("The default logger has not been registered. Build the log configuration first.");
        }

        internal static string DefaultLoggerName()
        {
            return Assembly.GetExecutingAssembly().GetName().Name;
        }

        public static void Dispose()
        {
            foreach (var logger in _loggerContainer.Values)
            {
                logger.Dispose();
            }
            _loggerContainer.Clear();
        }
    }
}
