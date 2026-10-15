// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

namespace Dignus.Log
{
    public enum TargetType
    {
        Console,
        File,
        Database,
    }
    public enum LogLevel
    {
        Debug = 0,
        Info,
        Error,
        Fatal
    }
    public enum FileRollingType
    {
        Hour,
        Day,
        Week,
        Month
    }
}
