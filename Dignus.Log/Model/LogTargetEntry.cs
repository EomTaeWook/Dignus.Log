// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.LogTarget.Interfaces;

namespace Dignus.Log.Model
{
    internal class LogTargetEntry
    {
        public LogLevel MinLogLevel { get; private set; }
        public ILogTarget LogTarget { get; private set; }

        public LogTargetEntry(LogLevel minLogLevel, ILogTarget logTarget)
        {
            MinLogLevel = minLogLevel;
            LogTarget = logTarget;
        }
    }
}
