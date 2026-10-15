// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Dignus.Log
{
    public class LogHelper
    {
        private static Logger _logger = LogManager.GetCurrentLogger();
        public static void SetLogger(Logger logger)
        {
            _logger = logger;
        }
        public static void Debug(in string logMessage, [CallerFilePath] in string callerFilePath = "", [CallerLineNumber] in int callerLineNumber = 0)
        {
            _logger.Debug(in logMessage, callerFilePath, in callerLineNumber);
        }
        public static void Info(in string logMessage, [CallerFilePath] in string callerFilePath = "", [CallerLineNumber] in int callerLineNumber = 0)
        {
            _logger.Info(in logMessage, callerFilePath, in callerLineNumber);
        }

        public static void Error(in string logMessage, [CallerFilePath] in string callerFilePath = "", [CallerLineNumber] in int callerLineNumber = 0)
        {
            _logger.Error(in logMessage, callerFilePath, in callerLineNumber);
        }
        public static void Fatal(in string logMessage, [CallerFilePath] in string callerFilePath = "", [CallerLineNumber] in int callerLineNumber = 0)
        {
            _logger.Fatal(in logMessage, callerFilePath, in callerLineNumber);
        }
        public static void Debug(in Exception ex, [CallerFilePath] in string callerFilePath = "", [CallerLineNumber] in int callerLineNumber = 0)
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
        public static void Info(in Exception ex, [CallerFilePath] in string callerFilePath = "", [CallerLineNumber] in int callerLineNumber = 0)
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
        public static void Error(in Exception ex, [CallerFilePath] in string callerFilePath = "", [CallerLineNumber] in int callerLineNumber = 0)
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
        public static void Fatal(in Exception ex, [CallerFilePath] in string callerFilePath = "", [CallerLineNumber] in int callerLineNumber = 0)
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
