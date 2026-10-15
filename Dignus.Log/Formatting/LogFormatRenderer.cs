// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Collections;
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

        private readonly ArrayQueue<IFormatElement> _logFormatElements = new ArrayQueue<IFormatElement>();

        public LogFormatRenderer()
        {
        }
        public LogFormatRenderer(string logFormatPattern)
        {
            SetLogFormat(logFormatPattern);
        }
        public void SetLogFormat(string logFormatPattern)
        {
            var logFormatElements = LogFormatElementCreator.Parse(logFormatPattern);
            _logFormatElements.Clear();
            _logFormatElements.AddRange(logFormatElements);
        }
        public void RenderTo(StringBuilder stringBuilder, LogEvent logEvent)
        {
            for (int index = 0; index < _logFormatElements.Count; ++index)
            {
                _logFormatElements[index].Write(stringBuilder, logEvent);
            }
        }
    }
}
