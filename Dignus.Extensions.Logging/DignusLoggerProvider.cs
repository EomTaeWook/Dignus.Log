// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Dignus.Extensions.Logging
{
    public class DignusLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();
        internal IExternalScopeProvider ScopeProvider => _scopeProvider;
        private readonly Log.ILogger _logger;
        private readonly ConcurrentDictionary<string, DignusLogger> _loggersByCategory = new ConcurrentDictionary<string, DignusLogger>();

        public DignusLoggerProvider() : this(LogManager.GetDefaultLogger())
        {
        }

        public DignusLoggerProvider(Log.ILogger logger)
        {
            _logger = logger;
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
        {
            return _loggersByCategory.GetOrAdd(categoryName, registeredCategoryName => new DignusLogger(_logger, this, registeredCategoryName));
        }
        public void Dispose()
        {
            _loggersByCategory.Clear();
        }
        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider ?? new LoggerExternalScopeProvider();
        }
    }
}
