// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Attributes;
using Dignus.Log.Elements.Interfaces;
using Dignus.Log.Model;
using System.Text;

namespace Dignus.Log.Elements
{
    [LogElement("callerFilePath")]
    internal class CallerFilePath : IFormatElement
    {
        public void Write(StringBuilder stringBuilder, LogEvent logMessageModel)
        {
            stringBuilder.Append(logMessageModel.CallerFilePath);
        }

        public void SetFormat(string format)
        {
        }
    }
}
