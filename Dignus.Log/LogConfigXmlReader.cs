// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Collections;
using Dignus.Log.Internals;
using Dignus.Log.LogTarget;
using Dignus.Log.LogTarget.Interfaces;
using Dignus.Log.Model;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Dignus.Log
{
    public class LogConfigXmlReader
    {
        private readonly Dictionary<string, ILogTarget> _logTargets = new Dictionary<string, ILogTarget>();
        private readonly UniqueSet<string> _loggerNames = new UniqueSet<string>();
        private readonly LogConfiguration _logConfiguration = new LogConfiguration();

        public static LogConfiguration Load(string path)
        {
            var document = new XmlDocument();
            document.Load(path);
            return new LogConfigXmlReader().Read(document);
        }
        private LogConfiguration Read(XmlDocument document)
        {
            var targetReferences = document.SelectNodes("//*[local-name()='loggers']/*[local-name()='logger']/*[local-name()='target']");
            foreach (XmlElement element in document.SelectNodes("//*[local-name()='targets']/*[local-name()='target']"))
            {
                ReadLogTarget(element, targetReferences);
            }
            foreach (XmlElement element in document.SelectNodes("//*[local-name()='loggers']/*[local-name()='logger']"))
            {
                ReadLogger(element);
            }
            return _logConfiguration;
        }
        private void ReadLogTarget(XmlElement element, XmlNodeList targetReferences)
        {
            var id = element.GetAttribute("id");
            if (string.IsNullOrEmpty(id) == true)
            {
                throw new InvalidOperationException("A target must have an id.");
            }
            if (_logTargets.ContainsKey(id) == true)
            {
                throw new InvalidOperationException($"The target '{id}' is declared more than once.");
            }

            var targetType = EnumHelper.GetEnumType<TargetType>(element.GetAttribute("logTarget"));
            _logTargets.Add(id, null);
            foreach (XmlElement targetReference in targetReferences)
            {
                if (targetReference.GetAttribute("id") != id)
                {
                    continue;
                }

                ILogTarget logTarget;
                if (targetType == TargetType.Console)
                {
                    logTarget = ReadConsoleLogTarget(id, element);
                }
                else if (targetType == TargetType.File)
                {
                    logTarget = ReadFileLogTarget(id, element);
                }
                else
                {
                    logTarget = ReadDatabaseLogTarget(id, element);
                }
                _logTargets[id] = logTarget;
                return;
            }
        }
        private ConsoleLogTarget ReadConsoleLogTarget(string id, XmlElement element)
        {
            var consoleLogTarget = new ConsoleLogTarget
            {
                LogFormatRenderer = element.GetAttribute("format")
            };
            foreach (XmlElement option in element.SelectNodes("*[local-name()='option']"))
            {
                var category = option.GetAttribute("category");
                if (IsCategory(category, "color") == false)
                {
                    throw UnknownOption(id, category);
                }
                var color = EnumHelper.GetEnumType<ConsoleColor>(option.GetAttribute("value"));
                var logLevel = EnumHelper.GetEnumType<LogLevel>(option.GetAttribute("id"));
                if (logLevel == LogLevel.Debug)
                {
                    consoleLogTarget.DebugColor = color;
                }
                else if (logLevel == LogLevel.Info)
                {
                    consoleLogTarget.InfoColor = color;
                }
                else if (logLevel == LogLevel.Error)
                {
                    consoleLogTarget.ErrorColor = color;
                }
                else
                {
                    consoleLogTarget.FatalColor = color;
                }
            }
            return consoleLogTarget;
        }
        private FileLogTarget ReadFileLogTarget(string id, XmlElement element)
        {
            var fileLogTarget = new FileLogTarget
            {
                LogFormatRenderer = element.GetAttribute("format")
            };
            foreach (XmlElement option in element.SelectNodes("*[local-name()='option']"))
            {
                var category = option.GetAttribute("category");
                var value = option.GetAttribute("value");
                if (IsCategory(category, "fileName") == true)
                {
                    fileLogTarget.LogFileName = value;
                }
                else if (IsCategory(category, "archiveFileName") == true)
                {
                    fileLogTarget.ArchiveFileName = value;
                }
                else if (IsCategory(category, "archiveRolling") == true)
                {
                    fileLogTarget.ArchiveRollingType = EnumHelper.GetEnumType<FileRollingType>(value);
                }
                else if (IsCategory(category, "maxArchiveFiles") == true)
                {
                    fileLogTarget.MaxArchiveFile = uint.Parse(value);
                }
                else if (IsCategory(category, "keepOpenFile") == true)
                {
                    fileLogTarget.KeepOpenFile = bool.Parse(value);
                }
                else if (IsCategory(category, "autoFlush") == true)
                {
                    fileLogTarget.AutoFlush = bool.Parse(value);
                }
                else
                {
                    throw UnknownOption(id, category);
                }
            }
            return fileLogTarget;
        }
        private DatabaseLogTarget ReadDatabaseLogTarget(string id, XmlElement element)
        {
            if (!(element.SelectSingleNode("*[local-name()='database']") is XmlElement databaseElement))
            {
                throw new InvalidOperationException($"The database target '{id}' must have a database element.");
            }

            var databaseLogTarget = new DatabaseLogTarget
            {
                DbConnection = databaseElement.GetAttribute("dbConnection"),
                DbProvider = Type.GetType(databaseElement.GetAttribute("dbProvider"), true),
                CommandTextRenderer = databaseElement.GetAttribute("dbCommand")
            };

            var keepConnectionOpen = databaseElement.GetAttribute("keepConnectionOpen");
            if (string.IsNullOrEmpty(keepConnectionOpen) == false)
            {
                databaseLogTarget.KeepConnection = bool.Parse(keepConnectionOpen);
            }

            foreach (XmlElement option in databaseElement.SelectNodes("*[local-name()='option']"))
            {
                var category = option.GetAttribute("category");
                if (IsCategory(category, "parameter") == false)
                {
                    throw UnknownOption(id, category);
                }
                databaseLogTarget.AddDbParameter(option.GetAttribute("id"), option.GetAttribute("value"));
            }
            return databaseLogTarget;
        }
        private void ReadLogger(XmlElement element)
        {
            var loggerName = LogConfiguration.ToLoggerName(element.GetAttribute("loggerName"));
            if (_loggerNames.Add(loggerName) == false)
            {
                throw new InvalidOperationException($"The logger '{loggerName}' is declared more than once.");
            }

            LoggerConfig loggerConfig = null;
            var loggerType = element.GetAttribute("loggerType");
            if (string.IsNullOrEmpty(loggerType) == false)
            {
                loggerConfig = _logConfiguration.AddLogger(loggerName, EnumHelper.GetEnumType<LoggerType>(loggerType));
            }

            foreach (XmlElement targetElement in element.SelectNodes("*[local-name()='target']"))
            {
                var id = targetElement.GetAttribute("id");
                if (_logTargets.TryGetValue(id, out ILogTarget logTarget) == false)
                {
                    throw new InvalidOperationException($"The logger '{loggerName}' refers to an unknown target '{id}'.");
                }

                var minLogLevel = LogLevel.Debug;
                var minLogLevelValue = targetElement.GetAttribute("minLogLevel");
                if (string.IsNullOrEmpty(minLogLevelValue) == false)
                {
                    minLogLevel = EnumHelper.GetEnumType<LogLevel>(minLogLevelValue);
                }

                if (loggerConfig != null)
                {
                    loggerConfig.AddLogTarget(minLogLevel, logTarget);
                }
                else
                {
                    _logConfiguration.AddLogTarget(loggerName, minLogLevel, logTarget);
                }
            }
        }
        private static bool IsCategory(string category, string name)
        {
            return string.Equals(category, name, StringComparison.OrdinalIgnoreCase);
        }
        private static InvalidOperationException UnknownOption(string id, string category)
        {
            return new InvalidOperationException($"The target '{id}' has an unknown option category '{category}'.");
        }
    }
}
