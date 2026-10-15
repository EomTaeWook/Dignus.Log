// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log;
using Dignus.Log.Model;

namespace Dignus.Extensions.Logging.Model
{
    internal class CategorizedLogEvent : LogEvent
    {
        public string Category { get; private set; }
        public CategorizedLogEvent(in LogLevel logLevel, in string category, in string message, in string callerFilePath, in int callerLineNumber, in string loggerName) : base(logLevel, message, callerFilePath, callerLineNumber, loggerName)
        {
            Category = category;
        }
    }
}
