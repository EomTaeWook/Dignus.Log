// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Elements.Interfaces;
using Dignus.Log.Formatting.Interfaces;
using Dignus.Log.Model;
using System.Text;

namespace Dignus.Log.Formatting
{
    public class LogFormatRenderer : ILogFormatRenderer
    {
        public static implicit operator LogFormatRenderer(string logFormatPattern)
        {
            var renderer = new LogFormatRenderer();
            renderer.SetLogFormat(logFormatPattern);
            return renderer;
        }

        private string _logFormatPattern = LogFormatElementCreator.DefaultLogFormat;
        private IFormatElement[] _logFormatElements;

        public LogFormatRenderer()
        {
        }
        public LogFormatRenderer(string logFormatPattern)
        {
            SetLogFormat(logFormatPattern);
        }
        public void SetLogFormat(string logFormatPattern)
        {
            if (string.IsNullOrEmpty(logFormatPattern) == true)
            {
                logFormatPattern = LogFormatElementCreator.DefaultLogFormat;
            }
            _logFormatPattern = logFormatPattern;
            _logFormatElements = null;
        }
        public void RenderTo(StringBuilder stringBuilder, LogEvent logEvent)
        {
            var logFormatElements = _logFormatElements;
            if (logFormatElements == null)
            {
                logFormatElements = LogFormatElementCreator.Parse(_logFormatPattern).ToArray();
                _logFormatElements = logFormatElements;
            }
            for (int index = 0; index < logFormatElements.Length; ++index)
            {
                logFormatElements[index].Write(stringBuilder, logEvent);
            }
        }
    }
}
