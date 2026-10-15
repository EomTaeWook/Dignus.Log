// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using System;

namespace Dignus.Log.Model
{
    public class LogEvent
    {
        public readonly LogLevel LogLevel;
        public readonly string Message;
        public readonly DateTime DateTime;
        public readonly string CallerFilePath;
        public readonly int CallerLineNumber;
        public readonly string LoggerName;

        public LogEvent(in LogLevel logLevel, in string message, in string callerFilePath, in int callerLineNumber, in string loggerName)
        {
            LogLevel = logLevel;
            Message = message;
            DateTime = DateTime.Now;
            CallerFilePath = callerFilePath;
            CallerLineNumber = callerLineNumber;
            LoggerName = loggerName;
        }
    }
}
