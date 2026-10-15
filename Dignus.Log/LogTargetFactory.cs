// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Formatting;
using Dignus.Log.LogTarget;
using Dignus.Log.Model;
using System.Collections.Generic;

namespace Dignus.Log
{
    internal class LogTargetFactory
    {
        internal DatabaseLogTarget CreateDatabaseLogTarget(DatabaseLogTargetConfig config)
        {
            var databaseLogTarget = LogBuilder.GetService<DatabaseLogTarget>();
            databaseLogTarget.DbConnection = config.DbConnection;
            databaseLogTarget.DbProvider = config.DbProvider;
            databaseLogTarget.CommandTextRenderer = config.CommandTextRenderer;
            databaseLogTarget.KeepConnection = config.KeepConnection;

            foreach (var param in config.DbParameters)
            {
                databaseLogTarget._dbParameters.Add(param.Key, param.Value);
            }
            return databaseLogTarget;
        }
        internal FileLogTarget CreateFileLogTarget(FileLogTargetConfig config)
        {
            var fileLogTarget = LogBuilder.GetService<FileLogTarget>();
            fileLogTarget.ArchiveFileName = config.ArchiveFileName;
            fileLogTarget.ArchiveRollingType = config.ArchiveRollingType;
            fileLogTarget.AutoFlush = config.AutoFlush;
            fileLogTarget.KeepOpenFile = config.KeepOpenFile;
            fileLogTarget.LogFileName = config.LogFileName;
            fileLogTarget.LogFormatRenderer = config.LogFormatRenderer;
            fileLogTarget.MaxArchiveFile = config.MaxArchiveFile;

            return fileLogTarget;
        }
        internal ConsoleLogTarget CreateConsoleLogTarget(ConsoleLogTargetConfig config)
        {
            var consoleLogTarget = LogBuilder.GetService<ConsoleLogTarget>();
            consoleLogTarget.DebugColor = config.DebugColor;
            consoleLogTarget.InfoColor = config.InfoColor;
            consoleLogTarget.ErrorColor = config.ErrorColor;
            consoleLogTarget.FatalColor = config.FatalColor;
            consoleLogTarget.LogFormatRenderer = config.LogFormatRenderer;

            return consoleLogTarget;
        }
    }
}

