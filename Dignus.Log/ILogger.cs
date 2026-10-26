// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Model;
using System.Runtime.CompilerServices;

namespace Dignus.Log
{
    public interface ILogger
    {
        string Name { get; }
        void Write(LogLevel logLevel, string message, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0);
        void Write(LogEvent logEvent);
        void Dispose();
    }
}
