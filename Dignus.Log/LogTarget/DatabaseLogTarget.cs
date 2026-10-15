// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Attributes;
using Dignus.Log.Formatting;
using Dignus.Log.Formatting.Interfaces;
using Dignus.Log.LogTarget.Interfaces;
using Dignus.Log.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;

namespace Dignus.Log.LogTarget
{
    [LogTarget("database")]
    public class DatabaseLogTarget : ILogTarget
    {
        public string DbConnection { get; internal set; }
        public Type DbProvider { get; internal set; }
        public bool KeepConnection { get; internal set; }

        public LogFormatRenderer CommandTextRenderer
        {
            get => _commandTextRenderer;
            set
            {
                if (value is ILogFormatRenderer)
                {
                    _commandTextRenderer = value;
                }
                else
                {
                    _commandTextRenderer = value;
                }
            }
        }

        internal readonly Dictionary<string, LogFormatRenderer> _dbParameters = new Dictionary<string, LogFormatRenderer>();

        private LogFormatRenderer _commandTextRenderer;
        private readonly StringBuilder _logStringBuilder = new StringBuilder();

        private IDbConnection _dbConnection;
        private readonly object _syncObj = new object();

        public DatabaseLogTarget()
        {
            _commandTextRenderer = LogFormatElementCreator.CreateDefaultRenderer();
        }
        private void CreateDbConnection()
        {
            var constructor = DbProvider.GetConstructor(Type.EmptyTypes);

            _dbConnection = constructor?.Invoke(new object[0]) as IDbConnection;

            _dbConnection.ConnectionString = DbConnection;
        }

        public void Write(LogEvent logMessage)
        {
            lock (_syncObj)
            {
                WriteCore(logMessage);
            }
        }
        private void WriteCore(LogEvent logMessage)
        {
            if (_dbConnection == null)
            {
                CreateDbConnection();
                _dbConnection.Open();
            }

            using (var command = _dbConnection.CreateCommand())
            {
                command.CommandText = RenderToString(CommandTextRenderer, logMessage);

                foreach (var keyValuePair in _dbParameters)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = keyValuePair.Key;
                    parameter.Value = RenderToString(keyValuePair.Value, logMessage);
                    command.Parameters.Add(parameter);
                }
                _ = command.ExecuteNonQuery();
            }

            if (KeepConnection == false)
            {
                DisposeCore();
            }
        }
        private string RenderToString(LogFormatRenderer logFormatRenderer, LogEvent logEvent)
        {
            logFormatRenderer.RenderTo(_logStringBuilder, logEvent);
            string logMessage = _logStringBuilder.ToString();
            _logStringBuilder.Clear();
            return logMessage;
        }

        public void Dispose()
        {
            lock (_syncObj)
            {
                DisposeCore();
            }
        }
        private void DisposeCore()
        {
            if (_dbConnection != null)
            {
                var temp = _dbConnection;
                _dbConnection.Dispose();
                Interlocked.CompareExchange(ref _dbConnection, null, temp);
            }
        }
    }
}
