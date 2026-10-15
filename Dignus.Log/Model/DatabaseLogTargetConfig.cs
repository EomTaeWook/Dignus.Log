// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Model.Interfaces;
using System;
using System.Collections.Generic;

namespace Dignus.Log.Model
{
    public class DatabaseLogTargetConfig
        : ILogTargetConfig
    {
        public string LoggerKey { get; private set; }
        public string DbConnection { get; set; }

        public Type DbProvider { get; set; }
        public string CommandTextRenderer { get; set; }
        public bool KeepConnection { get; set; }
        public Dictionary<string, string> DbParameters { get; set; } = new Dictionary<string, string>();

        public TargetType TargetType => TargetType.Database;

        public DatabaseLogTargetConfig(string loggerKey)
        {
            LoggerKey = loggerKey;
        }
    }
}
