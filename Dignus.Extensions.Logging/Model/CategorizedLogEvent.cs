// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log;
using Dignus.Log.Model;

namespace Dignus.Extensions.Logging.Model
{
    internal class CategorizedLogEvent : LogEvent
    {
        public string Category { get; }
        public CategorizedLogEvent(LogLevel logLevel, string category, string message, string callerFilePath, int callerLineNumber, string loggerName) : base(logLevel, message, callerFilePath, callerLineNumber, loggerName)
        {
            Category = category;
        }
    }
}
