// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Attributes;
using Dignus.Log.Elements.Interfaces;
using Dignus.Log.Model;
using System.IO;
using System.Text;

namespace Dignus.Log.Elements
{
    [LogElement("callerFileName")]
    internal class CallerFileName : IFormatElement
    {
        public void Write(StringBuilder stringBuilder, LogEvent logMessageModel)
        {
            stringBuilder.Append(Path.GetFileName(logMessageModel.CallerFilePath));
        }

        public void SetFormat(string format)
        {
        }
    }
}
