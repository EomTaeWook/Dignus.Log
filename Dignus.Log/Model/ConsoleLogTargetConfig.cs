// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Model.Interfaces;
using System;

namespace Dignus.Log.Model
{
    public class ConsoleLogTargetConfig
        : ILogTargetConfig
    {
        public string LoggerKey { get; private set; }
        public string LogFormatRenderer { get; set; }
        public LogLevel MinLogLevel { get; set; } = LogLevel.Debug;
        public ConsoleColor DebugColor { get; set; } = ConsoleColor.White;
        public ConsoleColor InfoColor { get; set; } = ConsoleColor.White;
        public ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;
        public ConsoleColor FatalColor { get; set; } = ConsoleColor.Red;

        public TargetType TargetType => TargetType.Console;

        public ConsoleLogTargetConfig(string loggerKey)
        {
            LoggerKey = loggerKey;
        }
    }
}
