// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Attributes;
using Dignus.Log.Elements.Interfaces;
using Dignus.Log.Model;
using System.Text;

namespace Dignus.Log.Elements
{
    [LogElement("level")]
    internal class LogLevelElement : IFormatElement
    {
        public void Write(StringBuilder stringBuilder, LogEvent logMessageModel)
        {
            switch (logMessageModel.LogLevel)
            {
                case Log.LogLevel.Debug:
                    stringBuilder.Append(nameof(Log.LogLevel.Debug));
                    break;
                case Log.LogLevel.Info:
                    stringBuilder.Append(nameof(Log.LogLevel.Info));
                    break;
                case Log.LogLevel.Error:
                    stringBuilder.Append(nameof(Log.LogLevel.Error));
                    break;
                case Log.LogLevel.Fatal:
                    stringBuilder.Append(nameof(Log.LogLevel.Fatal));
                    break;
                default:
                    stringBuilder.Append(logMessageModel.LogLevel);
                    break;
            }
        }

        public void SetFormat(string format)
        {

        }
    }
}
