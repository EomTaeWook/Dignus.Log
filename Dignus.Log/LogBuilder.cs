// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.DependencyInjection;
using Dignus.Log.Attributes;
using Dignus.Log.Formatting;
using Dignus.Log.LogTarget.Interfaces;
using Dignus.Log.Model;
using Dignus.Log.Rule;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Dignus.Log
{
    public class Builder
    {
        internal LogConfiguration _logConfiguration = new LogConfiguration();

        internal readonly ServiceContainer _serviceContainer;
        private readonly LogTargetFactory _logTargetFactory = new LogTargetFactory();
        private bool _isBuilt = false;

        public Dictionary<string, LoggerRule> _loggerRules = new Dictionary<string, LoggerRule>();
        public Builder()
        {
            _serviceContainer = new ServiceContainer();
            _serviceContainer.RegisterType(this);
            _serviceContainer.RegisterType(_logConfiguration);

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }
        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            LogManager.Dispose();
        }
        public LogConfiguration GetLogConfiguration()
        {
            return _logConfiguration;
        }
        public IServiceProvider Build()
        {
            if (_isBuilt == true)
            {
                throw new InvalidOperationException("log builder has already been built.");
            }
            _isBuilt = true;

            var assembly = typeof(LogBuilder).Assembly;

            RegisterLogTargets(assembly);

            var serviceProvider = _serviceContainer.Build();

            BuildLogTargets();

            foreach (var rule in _logConfiguration._loggerRules)
            {
                _loggerRules.Add(rule.Key, rule.Value);
            }

            LogManager.RegisterDefaultLogger(_logConfiguration.UseAsyncLogger);

            return serviceProvider;
        }
        private void BuildLogTargets()
        {
            foreach (var item in _logConfiguration.GetLogTargetConfigs())
            {
                ILogTarget logTarget;
                if (item.TargetType == TargetType.Console)
                {
                    logTarget = _logTargetFactory.CreateConsoleLogTarget(item as ConsoleLogTargetConfig);
                }
                else if (item.TargetType == TargetType.File)
                {
                    logTarget = _logTargetFactory.CreateFileLogTarget(item as FileLogTargetConfig);
                }
                else if (item.TargetType == TargetType.Database)
                {
                    logTarget = _logTargetFactory.CreateDatabaseLogTarget(item as DatabaseLogTargetConfig);
                }
                else
                {
                    throw new Exception("unsupported log target type specified.");
                }

                if (_logConfiguration._loggerConfigs.TryGetValue(item.LoggerKey, out var loggerConfig) == false)
                {
                    throw new Exception("unsupported or missing logger key in log configuration");
                }

                var rule = new LoggerRule(loggerConfig.LoggerName, loggerConfig.MinLogLevel, logTarget);

                _loggerRules.Add(loggerConfig.ConfigKey, rule);
            }
        }
        public void RegisterAssembly(Assembly assembly)
        {
            FormatElements.RegisterFromAssembly(assembly);
            RegisterLogTargets(assembly);
        }
        internal void RegisterLogTargets(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                var logTargetAttr = type.GetCustomAttribute<LogTargetAttribute>();
                if (logTargetAttr != null)
                {
                    _serviceContainer.RegisterType(logTargetAttr.GetTagetName(), type, LifeScope.Transient);
                }
            }
        }

        internal IEnumerable<LoggerRule> GetAllRules()
        {
            return _loggerRules.Values;
        }
    }

    public class LogBuilder
    {
        internal static readonly Builder _builder = new Builder();

        public static Builder Configuration(LogConfiguration logConfiguration)
        {
            _builder._logConfiguration = logConfiguration;
            return _builder;
        }
        public static IServiceProvider Build()
        {
            return _builder.Build();
        }
        internal static LogConfiguration GetLogConfiguration()
        {
            return _builder.GetLogConfiguration();
        }
        internal static T GetService<T>(string typeName) where T : class
        {
            return _builder._serviceContainer.GetService<T>(typeName);
        }
        internal static T GetService<T>() where T : class
        {
            return _builder._serviceContainer.GetService<T>();
        }
    }
}
