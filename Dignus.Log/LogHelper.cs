// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Model;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Dignus.Log
{
    public static class LogHelper
    {
        private static ILogger _logger;
        private static ILogger CurrentLogger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = LogManager.GetDefaultLogger();
                }
                return _logger;
            }
        }
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }
        public static void Debug(string logMessage, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            var logEvent = new LogEvent(LogLevel.Debug, logMessage, callerFilePath, callerLineNumber, CurrentLogger.Name);
            CurrentLogger.Write(logEvent);
        }
        public static void Info(string logMessage, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            var logEvent = new LogEvent(LogLevel.Info, logMessage, callerFilePath, callerLineNumber, CurrentLogger.Name);
            CurrentLogger.Write(logEvent);
        }

        public static void Error(string logMessage, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            var logEvent = new LogEvent(LogLevel.Error, logMessage, callerFilePath, callerLineNumber, CurrentLogger.Name);
            CurrentLogger.Write(logEvent);
        }
        public static void Fatal(string logMessage, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            var logEvent = new LogEvent(LogLevel.Fatal, logMessage, callerFilePath, callerLineNumber, CurrentLogger.Name);
            CurrentLogger.Write(logEvent);
        }
        public static void Debug(Exception ex, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            Write(LogLevel.Debug, ex, callerFilePath, callerLineNumber);
        }
        public static void Info(Exception ex, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            Write(LogLevel.Info, ex, callerFilePath, callerLineNumber);
        }
        public static void Error(Exception ex, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            Write(LogLevel.Error, ex, callerFilePath, callerLineNumber);
        }
        public static void Fatal(Exception ex, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            Write(LogLevel.Fatal, ex, callerFilePath, callerLineNumber);
        }
        private static void Write(LogLevel logLevel, Exception ex, string callerFilePath, int callerLineNumber)
        {
            if (ex == null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            var frames = new StackTrace(ex, true).GetFrames();
            if (frames != null)
            {
                foreach (var frame in frames)
                {
                    var filePath = frame.GetFileName();
                    if (string.IsNullOrEmpty(filePath) == false)
                    {
                        callerFilePath = filePath;
                        callerLineNumber = frame.GetFileLineNumber();
                        break;
                    }
                }
            }

            var logEvent = new LogEvent(logLevel, ex.ToString(), callerFilePath, callerLineNumber, CurrentLogger.Name);
            CurrentLogger.Write(logEvent);
        }
    }
}
