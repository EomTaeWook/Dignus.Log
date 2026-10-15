// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Attributes;
using Dignus.Log.Elements.Interfaces;
using Dignus.Log.Model;
using System.Text;

namespace Dignus.Log.Elements
{
    [LogElement("callerLineNumber")]
    internal class CallerLineNumber : IFormatElement
    {
        public string _format;
        public void Write(StringBuilder stringBuilder, LogEvent logMessageModel)
        {
            stringBuilder.Append(logMessageModel.CallerLineNumber.ToString(_format));
        }

        public void SetFormat(string format)
        {
            _format = string.IsNullOrEmpty(format) ? "G" : format;
        }
    }
}
