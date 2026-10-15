// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

namespace Dignus.Log.Model
{
    internal enum ElementType
    {
        Targets,
        Target,
        Option,
        Loggers,
        Logger,
        Database,

        Max,
    }
    internal enum AttibuteType
    {
        Id,
        LogTarget,
        Format,
        Value,
        MinLogLevel,
        Category,
        LoggerName,

        Max,
    }
    internal enum OptionCategoryType
    {
        Color,
        FileName,
        ArchiveFileName,
        ArchiveRolling,
        MaxArchiveFiles,
        KeepOpenFile,
        AutoFlush,
        Parameter,

        Format,

        Max
    }

    internal enum DatabaseAttributeType
    {
        DbProvider,
        DbConnection,
        DbCommand,
        KeepConnectionOpen,

        Max
    }
}
