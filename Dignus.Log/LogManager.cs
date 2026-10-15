// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using System.Collections.Concurrent;
using System.Reflection;

namespace Dignus.Log
{
    public static class LogManager
    {
        private static readonly ConcurrentDictionary<string, Logger> _loggerContainer = new ConcurrentDictionary<string, Logger>();
        public static Logger GetLogger(string name)
        {
            return _loggerContainer.GetOrAdd(name, Factory);
        }
        private static Logger Factory(string loggerName)
        {
            var logger = Logger.MakeLogger(loggerName);
            foreach (var item in LogBuilder._builder.GetAllRules())
            {
                if (item.LoggerNames.Contains(loggerName))
                {
                    for (LogLevel logLevel = item.MinLogLevel; logLevel <= LogLevel.Fatal; ++logLevel)
                    {
                        logger._logTargetsByLevel[(int)logLevel].AddTarget(item.LogTarget);
                    }
                }
            }
            return logger;
        }
        public static Logger GetCurrentLogger()
        {
            return GetLogger(DefaultLoggerName());
        }
        internal static string DefaultLoggerName()
        {
            return Assembly.GetExecutingAssembly().GetName().Name;
        }
        public static void Dispose()
        {
            var loggers = _loggerContainer.Values;
            foreach (var item in loggers)
            {
                item.Dispose();
            }
            _loggerContainer.Clear();
        }
    }
}
