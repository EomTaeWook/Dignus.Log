// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

namespace Dignus.Log.Model
{
    public class LoggerConfig
    {
        public string LoggerName { get; set; }
        public string ConfigKey { get; set; }
        public LogLevel MinLogLevel { get; set; } = LogLevel.Debug;
    }
}
