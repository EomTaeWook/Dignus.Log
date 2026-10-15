// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Internals;
using Dignus.Log.Model;
using Dignus.Log.Model.Interfaces;
using System;

namespace Dignus.Log
{
    internal class LogConfigFactory
    {
        public LogConfigFactory()
        {
        }
        public void CreateLogTargetConfigModel(LogTargetConfigModel logtargetConfig, LogConfiguration configuration)
        {
            ILogTargetConfig config = null;

            if (logtargetConfig.LogTarget == TargetType.Console)
            {
                config = CreateConsoleLogConfig(logtargetConfig);
            }
            else if (logtargetConfig.LogTarget == TargetType.File)
            {
                config = CreateFileLogConfig(logtargetConfig);
            }
            else if (logtargetConfig.LogTarget == TargetType.Database)
            {
                config = CreateDatabaseLogConfig(logtargetConfig as DatabaseLogTargetConfigModel);
            }
            configuration.AddLogTargetConfig(config);
        }
        private ConsoleLogTargetConfig CreateConsoleLogConfig(LogTargetConfigModel logTargetModel)
        {
            var config = new ConsoleLogTargetConfig(logTargetModel.Id);

            foreach (var item in logTargetModel.Options)
            {
                var category = EnumHelper.GetEnumType<OptionCategoryType>(item.Category);

                if (category == OptionCategoryType.Format)
                {
                    config.LogFormatRenderer = item.Value;
                }
                else if (category == OptionCategoryType.Color)
                {
                    var logLevel = EnumHelper.GetEnumType<LogLevel>(item.Id);
                    if (logLevel == LogLevel.Debug)
                    {
                        config.DebugColor = EnumHelper.GetEnumType<ConsoleColor>(item.Value);
                    }
                    else if (logLevel == LogLevel.Error)
                    {
                        config.ErrorColor = EnumHelper.GetEnumType<ConsoleColor>(item.Value);
                    }
                    else if (logLevel == LogLevel.Fatal)
                    {
                        config.FatalColor = EnumHelper.GetEnumType<ConsoleColor>(item.Value);
                    }
                    else if (logLevel == LogLevel.Info)
                    {
                        config.InfoColor = EnumHelper.GetEnumType<ConsoleColor>(item.Value);
                    }
                }
            }
            return config;
        }
        private FileLogTargetConfig CreateFileLogConfig(LogTargetConfigModel logTargetConfigModel)
        {
            var config = new FileLogTargetConfig(logTargetConfigModel.Id);

            foreach (var item in logTargetConfigModel.Options)
            {
                var category = EnumHelper.GetEnumType<OptionCategoryType>(item.Category);
                if (category == OptionCategoryType.Format)
                {
                    config.LogFormatRenderer = item.Value;
                }
                else if (category == OptionCategoryType.FileName)
                {
                    config.LogFileName = item.Value;
                }
                else if (category == OptionCategoryType.ArchiveFileName)
                {
                    config.ArchiveFileName = item.Value;
                }
                else if (category == OptionCategoryType.ArchiveRolling)
                {
                    config.ArchiveRollingType = EnumHelper.GetEnumType<FileRollingType>(item.Value);
                }
                else if (category == OptionCategoryType.MaxArchiveFiles)
                {
                    config.MaxArchiveFile = uint.Parse(item.Value);
                }
                else if (category == OptionCategoryType.KeepOpenFile)
                {
                    config.KeepOpenFile = bool.Parse(item.Value);
                }
                else if (category == OptionCategoryType.AutoFlush)
                {
                    config.AutoFlush = bool.Parse(item.Value);
                }
            }
            return config;
        }
        private DatabaseLogTargetConfig CreateDatabaseLogConfig(DatabaseLogTargetConfigModel logTargetConfigModel)
        {
            var config = new DatabaseLogTargetConfig(logTargetConfigModel.Id);

            foreach (var item in logTargetConfigModel.DbAttributes)
            {
                var category = EnumHelper.GetEnumType<DatabaseAttributeType>(item.Category);

                if (category == DatabaseAttributeType.DbProvider)
                {
                    config.DbProvider = Type.GetType(item.Value);
                }
                else if (category == DatabaseAttributeType.DbConnection)
                {
                    config.DbConnection = item.Value;
                }
                else if (category == DatabaseAttributeType.DbCommand)
                {
                    config.CommandTextRenderer = item.Value;
                }
                else if (category == DatabaseAttributeType.KeepConnectionOpen)
                {
                    config.KeepConnection = bool.Parse(item.Value);
                }
            }

            foreach (var item in logTargetConfigModel.Options)
            {
                var category = EnumHelper.GetEnumType<OptionCategoryType>(item.Category);

                if (category == OptionCategoryType.Parameter)
                {
                    config.DbParameters.Add(item.Id, item.Value);
                }
            }
            return config;
        }
    }
}
