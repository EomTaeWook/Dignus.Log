// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace Dignus.Extensions.Logging
{
    public class DignusLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();
        internal IExternalScopeProvider ScopeProvider { get => _scopes; }
        private readonly Logger _logger = LogManager.GetCurrentLogger();
        private readonly ConcurrentDictionary<string, DignusLogger> _cache = new ConcurrentDictionary<string, DignusLogger>();


        public ILogger CreateLogger(string categoryName)
        {
            return _cache.GetOrAdd(categoryName, (name) =>
            {
                return new DignusLogger(_logger, this, name);
            });
        }
        public void Dispose()
        {
            _logger.Dispose();
            _cache.Clear();
            GC.SuppressFinalize(this);
        }
        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopes = scopeProvider;
            if (_scopes == null)
            {
                _scopes = new LoggerExternalScopeProvider();
            }
        }
    }
}
