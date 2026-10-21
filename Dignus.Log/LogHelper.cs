// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Model;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Dignus.Log
{
    public class LogHelper
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
            var st = new StackTrace(ex, true);
            if (st.FrameCount > 0)
            {
                var frames = st.GetFrames().Select(f =>
                new
                {
                    FilePath = f.GetFileName(),
                    LineNumber = f.GetFileLineNumber(),
                });
                foreach (var frame in frames)
                {
                    if (string.IsNullOrEmpty(frame.FilePath))
                    {
                        continue;
                    }
                    Debug(ex.Message, frame.FilePath, frame.LineNumber);
                }
            }
            else
            {
                Debug(ex.Message, callerFilePath, callerLineNumber);
            }
        }
        public static void Info(Exception ex, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            var st = new StackTrace(ex, true);
            if (st.FrameCount > 0)
            {
                var frames = st.GetFrames().Select(f =>
                new
                {
                    FilePath = f.GetFileName(),
                    LineNumber = f.GetFileLineNumber(),
                });
                foreach (var frame in frames)
                {
                    if (string.IsNullOrEmpty(frame.FilePath))
                    {
                        continue;
                    }
                    Info(ex.Message, frame.FilePath, frame.LineNumber);
                }
            }
            else
            {
                Info(ex.Message, callerFilePath, callerLineNumber);
            }
        }
        public static void Error(Exception ex, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            var st = new StackTrace(ex, true);
            if (st.FrameCount > 0)
            {
                var frames = st.GetFrames().Select(f =>
                new
                {
                    FilePath = f.GetFileName(),
                    LineNumber = f.GetFileLineNumber(),
                });
                foreach (var frame in frames)
                {
                    if (string.IsNullOrEmpty(frame.FilePath))
                    {
                        continue;
                    }
                    Error(ex.Message, frame.FilePath, frame.LineNumber);
                }
            }
            else
            {
                Error(ex.Message, callerFilePath, callerLineNumber);
            }
        }
        public static void Fatal(Exception ex, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            var st = new StackTrace(ex, true);
            if (st.FrameCount > 0)
            {
                var frames = st.GetFrames().Select(f =>
                new
                {
                    FilePath = f.GetFileName(),
                    LineNumber = f.GetFileLineNumber(),
                });
                foreach (var frame in frames)
                {
                    if (string.IsNullOrEmpty(frame.FilePath))
                    {
                        continue;
                    }
                    Fatal(ex.Message, frame.FilePath, frame.LineNumber);
                }
            }
            else
            {
                Fatal(ex.Message, callerFilePath, callerLineNumber);
            }
        }
    }
}
