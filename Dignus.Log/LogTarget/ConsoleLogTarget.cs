// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Formatting;
using Dignus.Log.Formatting.Interfaces;
using Dignus.Log.LogTarget.Interfaces;
using Dignus.Log.Model;
using System;
using System.Text;

namespace Dignus.Log.LogTarget
{
    public class ConsoleLogTarget : ILogTarget
    {
        public ConsoleColor DebugColor { get; set; } = ConsoleColor.White;
        public ConsoleColor InfoColor { get; set; } = ConsoleColor.White;
        public ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;
        public ConsoleColor FatalColor { get; set; } = ConsoleColor.Red;
        public LogFormatRenderer LogFormatRenderer
        {
            get => _logFormatRenderer;
            set
            {
                if (value is ILogFormatRenderer)
                {
                    _logFormatRenderer = value;
                }
            }
        }

        private LogFormatRenderer _logFormatRenderer;
        private readonly StringBuilder _logStringBuilder = new StringBuilder();
        public ConsoleLogTarget()
        {
            _logFormatRenderer = LogFormatElementCreator.CreateDefaultRenderer();
        }
        private ConsoleColor GetLogColor(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                    return DebugColor;
                case LogLevel.Info:
                    return InfoColor;
                case LogLevel.Error:
                    return ErrorColor;
                case LogLevel.Fatal:
                    return FatalColor;
                default:
                    return ConsoleColor.White;
            }
        }
        public void Write(LogEvent logEvent)
        {
            var textWriter = Console.Out;
            lock (textWriter)
            {
                var previousColor = Console.ForegroundColor;
                Console.ForegroundColor = GetLogColor(logEvent.LogLevel);
                try
                {
                    _logFormatRenderer.RenderTo(_logStringBuilder, logEvent);
                    textWriter.WriteLine(_logStringBuilder.ToString());
                }
                finally
                {
                    _logStringBuilder.Clear();
                    Console.ForegroundColor = previousColor;
                }
            }
        }
        public void Dispose()
        {
        }
    }
}
