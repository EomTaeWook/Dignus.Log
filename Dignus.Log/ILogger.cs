// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Model;

namespace Dignus.Log
{
    public interface ILogger
    {
        string Name { get; }
        void Write(LogLevel logLevel, string message, string callerFilePath, int callerLineNumber);
        void Write(LogEvent logEvent);
        void Dispose();
    }
}
