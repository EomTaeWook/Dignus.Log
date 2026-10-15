// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Attributes;
using Dignus.Log.Elements.Interfaces;
using Dignus.Log.Model;
using System.Text;

namespace Dignus.Log.Elements
{
    [LogElement("literal")]
    internal class Literal : IFormatElement
    {
        public string LiteralValue { get; set; }

        public void Write(StringBuilder stringBuilder, LogEvent logMessage)
        {
            stringBuilder.Append(LiteralValue);
        }
        public void SetFormat(string format)
        {
        }
    }
}
