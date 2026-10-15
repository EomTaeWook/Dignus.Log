// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Model.Interfaces;

namespace Dignus.Log.Model
{
    public class FileLogTargetConfig
        : ILogTargetConfig
    {
        public string LoggerKey { get; private set; }
        public string LogFormatRenderer { get; set; }
        public string LogFileName { get; set; }
        public string ArchiveFileName { get; set; }
        public FileRollingType ArchiveRollingType { get; set; }
        public uint MaxArchiveFile { get; set; }

        public bool KeepOpenFile { get; set; }
        public bool AutoFlush { get; set; }

        public TargetType TargetType => TargetType.File;

        public FileLogTargetConfig(string loggerKey)
        {
            LoggerKey = loggerKey;
        }
    }
}
