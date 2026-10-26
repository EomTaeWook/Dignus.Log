// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using System.Runtime.CompilerServices;

namespace Dignus.Log
{
    public static class LoggerExtensions
    {
        public static void Debug(this ILogger logger, string message, [CallerFilePath] string callerFilePath ="", [CallerLineNumber] int callerLineNumber =0)
        {
            logger.Write(LogLevel.Debug, message, callerFilePath, callerLineNumber);
        }
        public static void Info(this ILogger logger, string message, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            logger.Write(LogLevel.Info, message, callerFilePath, callerLineNumber);
        }
        public static void Error(this ILogger logger, string message, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            logger.Write(LogLevel.Error, message, callerFilePath, callerLineNumber);
        }
        public static void Fatal(this ILogger logger, string message, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            logger.Write(LogLevel.Fatal, message, callerFilePath, callerLineNumber);
        }
    }
}
