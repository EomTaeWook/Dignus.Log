// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Collections;
using Dignus.Log.Internals;
using Dignus.Log.Model;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Dignus.Log
{

    public class LogConfigXmlReader
    {
        private readonly Dictionary<OptionCategoryType, string> _optionStringToMap = new Dictionary<OptionCategoryType, string>();
        private readonly Dictionary<AttibuteType, string> _attibuteStringToMap = new Dictionary<AttibuteType, string>();
        private readonly Dictionary<ElementType, string> _elementStringToMap = new Dictionary<ElementType, string>();
        private readonly Dictionary<DatabaseAttributeType, string> _databaseAttributeStringToMap = new Dictionary<DatabaseAttributeType, string>();

        private readonly Dictionary<string, LogTargetConfigModel> _logTargetConfigToMap = new Dictionary<string, LogTargetConfigModel>();

        private readonly LogConfiguration _logConfiguration = new LogConfiguration();
        private readonly LogConfigFactory _logConfigCreator = new LogConfigFactory();
        public static LogConfiguration Load(string path)
        {
            return new LogConfigXmlReader().LoadXmlFile(path);
        }
        public LogConfigXmlReader()
        {
            for (var e = OptionCategoryType.Color; e < OptionCategoryType.Max; ++e)
            {
                _optionStringToMap[e] = ConvertEnumToString(e);
            }

            for (var e = AttibuteType.Id; e < AttibuteType.Max; ++e)
            {
                _attibuteStringToMap[e] = ConvertEnumToString(e);
            }

            for (var e = ElementType.Targets; e < ElementType.Max; ++e)
            {
                _elementStringToMap[e] = ConvertEnumToString(e);
            }
            for (var e = DatabaseAttributeType.DbProvider; e < DatabaseAttributeType.Max; ++e)
            {
                _databaseAttributeStringToMap[e] = ConvertEnumToString(e);
            }
        }
        private string ConvertEnumToString(Enum enumValue)
        {
            var enumString = enumValue.ToString();
            return $"{char.ToLower(enumString[0])}{enumString.Substring(1)}";
        }

        private LogConfiguration LoadXmlFile(string path)
        {
            using (var reader = XmlReader.Create(path))
            {
                while (reader.Read() == true)
                {
                    if (reader.IsStartElement() == false)
                    {
                        continue;
                    }

                    if (reader.Name.Equals(ElementType.Targets.ToString(), StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var subReader = reader.ReadSubtree();

                        CreateConfigModels(subReader);
                    }
                    else if (reader.Name.Equals(ElementType.Loggers.ToString(), StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var subReader = reader.ReadSubtree();

                        CreateLoggerTargetConfigModels(subReader);
                    }
                }
            }

            foreach (var item in _logConfiguration.GetLoggerConfigs())
            {
                var logConfigModel = _logTargetConfigToMap[item.ConfigKey];
                _logConfigCreator.CreateLogTargetConfigModel(logConfigModel, _logConfiguration);
            }

            return _logConfiguration;
        }
        private void CreateLoggerTargetConfigModels(XmlReader reader)
        {
            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == false)
                {
                    continue;
                }
                if (reader.Name.Equals(ElementType.Logger.ToString(), StringComparison.OrdinalIgnoreCase) == true)
                {
                    _logConfiguration.AddLoggerConfig(ParseLoggerConfigModel(reader));
                }
            }
        }
        private void CreateConfigModels(XmlReader reader)
        {
            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == false)
                {
                    continue;
                }

                if (reader.Name.Equals(ElementType.Target.ToString(), StringComparison.OrdinalIgnoreCase) == true)
                {
                    var logTarget = EnumHelper.GetEnumType<TargetType>(reader.GetAttribute(_attibuteStringToMap[AttibuteType.LogTarget]));

                    LogTargetConfigModel logTargetConfigModel;
                    if (logTarget == TargetType.Database)
                    {
                        logTargetConfigModel = ParseDatabaseConfigModel(reader);
                    }
                    else
                    {
                        logTargetConfigModel = ParseConfigModel(reader, logTarget);
                    }
                    _logTargetConfigToMap.Add(logTargetConfigModel.Id, logTargetConfigModel);
                }
            }
        }
        private DatabaseLogTargetConfigModel ParseDatabaseConfigModel(XmlReader reader)
        {
            var databaseLogTargetConfigModel = new DatabaseLogTargetConfigModel()
            {
                LogTarget = TargetType.Database,
                Id = reader.GetAttribute(_attibuteStringToMap[AttibuteType.Id]) ?? string.Empty
            };

            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == false)
                {
                    continue;
                }
                if (reader.Name.Equals(_elementStringToMap[ElementType.Database]))
                {
                    databaseLogTargetConfigModel.DbAttributes.Add(new OptionModel()
                    {
                        Category = DatabaseAttributeType.DbConnection.ToString(),
                        Value = reader.GetAttribute(_databaseAttributeStringToMap[DatabaseAttributeType.DbConnection]) ?? string.Empty
                    });

                    databaseLogTargetConfigModel.DbAttributes.Add(new OptionModel()
                    {
                        Category = DatabaseAttributeType.DbProvider.ToString(),
                        Value = reader.GetAttribute(_databaseAttributeStringToMap[DatabaseAttributeType.DbProvider]) ?? string.Empty
                    });
                    databaseLogTargetConfigModel.DbAttributes.Add(new OptionModel()
                    {
                        Category = DatabaseAttributeType.DbCommand.ToString(),
                        Value = reader.GetAttribute(_databaseAttributeStringToMap[DatabaseAttributeType.DbCommand]) ?? string.Empty
                    });

                    var subReader = reader.ReadSubtree();

                    while (subReader.ReadToFollowing("option"))
                    {
                        var option = new OptionModel()
                        {
                            Category = subReader.GetAttribute(_attibuteStringToMap[AttibuteType.Category]),
                            Id = subReader.GetAttribute(_attibuteStringToMap[AttibuteType.Id]) ?? string.Empty,
                            Value = subReader.GetAttribute(_attibuteStringToMap[AttibuteType.Value]) ?? string.Empty
                        };
                        databaseLogTargetConfigModel.Options.Add(option);
                    }
                }
            }

            return databaseLogTargetConfigModel;
        }
        private ArrayQueue<OptionModel> ParseOptionModels(XmlReader reader)
        {
            var optionDatas = new ArrayQueue<OptionModel>();
            while (reader.Read() == true)
            {
                if (reader.IsStartElement() == false)
                {
                    continue;
                }
                if (reader.Name.Equals(_elementStringToMap[ElementType.Option]))
                {
                    var option = new OptionModel()
                    {
                        Category = reader.GetAttribute(_attibuteStringToMap[AttibuteType.Category]),
                        Id = reader.GetAttribute(_attibuteStringToMap[AttibuteType.Id]) ?? string.Empty,
                        Value = reader.GetAttribute(_attibuteStringToMap[AttibuteType.Value]) ?? string.Empty
                    };

                    optionDatas.Add(option);
                }
                else if (reader.Name.Equals(_elementStringToMap[ElementType.Database]))
                {
                    optionDatas.Add(new OptionModel()
                    {
                        Category = DatabaseAttributeType.DbConnection.ToString(),
                        Value = reader.GetAttribute(_databaseAttributeStringToMap[DatabaseAttributeType.DbConnection]) ?? string.Empty
                    });

                    optionDatas.Add(new OptionModel()
                    {
                        Category = DatabaseAttributeType.DbProvider.ToString(),
                        Value = reader.GetAttribute(_databaseAttributeStringToMap[DatabaseAttributeType.DbProvider]) ?? string.Empty
                    });
                    optionDatas.Add(new OptionModel()
                    {
                        Category = DatabaseAttributeType.DbCommand.ToString(),
                        Value = reader.GetAttribute(_databaseAttributeStringToMap[DatabaseAttributeType.DbCommand]) ?? string.Empty
                    });

                    var subReader = reader.ReadSubtree();

                    while (subReader.ReadToFollowing("option"))
                    {
                        var option = new OptionModel()
                        {
                            Category = subReader.GetAttribute(_attibuteStringToMap[AttibuteType.Category]),
                            Id = subReader.GetAttribute(_attibuteStringToMap[AttibuteType.Id]) ?? string.Empty,
                            Value = subReader.GetAttribute(_attibuteStringToMap[AttibuteType.Value]) ?? string.Empty
                        };
                        optionDatas.Add(option);
                    }
                }
            }
            return optionDatas;
        }

        private LogTargetConfigModel ParseConfigModel(XmlReader reader, TargetType targetType)
        {

            var logTargetConfigModel = new LogTargetConfigModel()
            {
                Id = reader.GetAttribute(_attibuteStringToMap[AttibuteType.Id]) ?? string.Empty,
                LogTarget = targetType
            };

            var logFormatOption = new OptionModel()
            {
                Category = AttibuteType.Format.ToString(),
                Value = reader.GetAttribute(_attibuteStringToMap[AttibuteType.Format]) ?? string.Empty,
            };

            logTargetConfigModel.Options.Add(logFormatOption);

            logTargetConfigModel.Options.AddRange(ParseOptionModels(reader.ReadSubtree()));

            return logTargetConfigModel;
        }

        private LoggerConfig ParseLoggerConfigModel(XmlReader reader)
        {
            var logger = new LoggerConfig();

            var key = reader.GetAttribute(_attibuteStringToMap[AttibuteType.Id]);

            logger.ConfigKey = key;

            var minLogLevel = reader.GetAttribute(_attibuteStringToMap[AttibuteType.MinLogLevel]);

            if (string.IsNullOrEmpty(minLogLevel) == false)
            {
                logger.MinLogLevel = EnumHelper.GetEnumType<LogLevel>(minLogLevel);
            }

            var name = reader.GetAttribute(_attibuteStringToMap[AttibuteType.LoggerName]);

            if (string.IsNullOrEmpty(name))
            {
                name = LogManager.DefaultLoggerName();
            }

            logger.LoggerName = name;

            return logger;
        }
    }
}
