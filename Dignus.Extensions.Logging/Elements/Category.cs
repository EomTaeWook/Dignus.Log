// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Extensions.Logging.Model;
using Dignus.Log.Attributes;
using Dignus.Log.Elements.Interfaces;
using Dignus.Log.Model;
using System.Text;

namespace Dignus.Extensions.Logging.Elements
{
    [LogElement("category")]
    internal class Category : IFormatElement
    {
        public void Write(StringBuilder stringBuilder, LogEvent logMessageModel)
        {
            if (logMessageModel is CategorizedLogEvent logMessage)
            {
                stringBuilder.Append(logMessage.Category);
            }
        }

        public void SetFormat(string format)
        {
        }
    }
}
