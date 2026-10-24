// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Dignus.Log
{
    public static class LogManager
    {
        private static readonly ConcurrentDictionary<string, ILogger> _loggerContainer = new ConcurrentDictionary<string, ILogger>();
        private static bool _isDisposed;

        public static ILogger GetLogger(string name)
        {
            if (string.IsNullOrEmpty(name) == true)
            {
                name = DefaultLoggerName();
            }
            if (_loggerContainer.TryGetValue(name, out ILogger logger))
            {
                return logger;
            }

            lock (_loggerContainer)
            {
                if (_isDisposed == true)
                {
                    throw new ObjectDisposedException(nameof(LogManager), $"The logger '{name}' cannot be created after LogManager.Dispose().");
                }
                if (LogBuilder._builder.IsBuilt == false)
                {
                    throw new InvalidOperationException($"The logger '{name}' cannot be created before the log configuration is built. Call LogBuilder.Build() first.");
                }

                return _loggerContainer.GetOrAdd(name, CreateConfiguredLogger);
            }
        }

        private static ILogger CreateConfiguredLogger(string loggerName)
        {
            if (LogBuilder._builder.GetLoggerType(loggerName) == LoggerType.Async)
            {
                return CreateAsyncLogger(loggerName);
            }
            return CreateLogger(loggerName);
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
            foreach (var logTargetEntry in LogBuilder._builder.GetLogTargetEntries(loggerName))
            {
                for (LogLevel logLevel = logTargetEntry.MinLogLevel; logLevel <= LogLevel.Fatal; ++logLevel)
                {
                    logTargetsByLevel[(int)logLevel].AddTarget(logTargetEntry.LogTarget);
                }
            }
        }

        public static void RegisterLogger(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (string.IsNullOrEmpty(logger.Name) == true)
            {
                throw new ArgumentException("Logger name cannot be null or empty.", nameof(logger));
            }

            lock (_loggerContainer)
            {
                if (_isDisposed == true)
                {
                    throw new ObjectDisposedException(nameof(LogManager), $"The logger '{logger.Name}' cannot be registered after LogManager.Dispose().");
                }
                if (_loggerContainer.TryAdd(logger.Name, logger) == false)
                {
                    throw new InvalidOperationException($"The logger '{logger.Name}' has already been registered.");
                }
            }
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
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                return entryAssembly.GetName().Name;
            }
            return Assembly.GetExecutingAssembly().GetName().Name;
        }

        public static void Dispose()
        {
            lock (_loggerContainer)
            {
                _isDisposed = true;
                foreach (var logger in _loggerContainer.Values)
                {
                    logger.Dispose();
                }
                _loggerContainer.Clear();
            }
        }
    }
}
