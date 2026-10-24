// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Collections;
using Dignus.Log.Elements;
using Dignus.Log.Elements.Interfaces;
using System.Collections.Generic;
using System.Text;

namespace Dignus.Log.Formatting
{
    internal class LogFormatElementCreator
    {
        internal const string DefaultLogFormat = "${datetime} | ${level} | ${message}";

        public static List<IFormatElement> Parse(string logFormPattern)
        {
            StringBuilder stringBuilder = new StringBuilder();

            ArrayQueue<char> reader = new ArrayQueue<char>();
            reader.AddRange(logFormPattern.ToCharArray());

            var logFormatElements = new List<IFormatElement>();
            while (reader.TryRead(out char ch) == true)
            {
                if (ch == '$' && reader.Count > 0 && reader.Peek() == '{')
                {
                    logFormatElements.Add(AddLiteralFormat(stringBuilder));
                    logFormatElements.Add(ParseSymbol(reader));
                    continue;
                }
                stringBuilder.Append(ch);
            }
            if (stringBuilder.Length > 0)
            {
                logFormatElements.Add(AddLiteralFormat(stringBuilder));
            }

            reader.Clear();
            return logFormatElements;
        }
        internal static LogFormatRenderer CreateDefaultRenderer()
        {
            return new LogFormatRenderer(DefaultLogFormat);
        }
        private static Literal AddLiteralFormat(StringBuilder sb)
        {
            var logForm = new Literal
            {
                LiteralValue = sb.ToString()
            };
            sb.Clear();
            return logForm;
        }
        private static IFormatElement ParseSymbol(ArrayQueue<char> reader)
        {
            StringBuilder stringBuilder = new StringBuilder();
            while (reader.TryRead(out char ch) == true)
            {
                stringBuilder.Append(ch);
                if (ch == '}')
                {
                    var symbol = GetSymbol(stringBuilder.ToString(), out string symbolFormat);
                    IFormatElement formatElement = FormatElements.Create(symbol);
                    if (formatElement == null)
                    {
                        formatElement = new Literal();
                    }
                    formatElement.SetFormat(symbolFormat);
                    stringBuilder.Clear();
                    return formatElement;
                }
            }
            return new Literal
            {
                LiteralValue = $"${stringBuilder}"
            };
        }
        private static string GetSymbol(string text, out string symbolFormat)
        {
            var startIndex = text.IndexOf('{') + 1;

            var endIndex = text.IndexOf('}') - 1;

            string symbol = text.Substring(startIndex, endIndex);

            symbolFormat = string.Empty;

            if ((startIndex = symbol.IndexOf(':')) > -1)
            {
                symbolFormat = symbol.Substring(startIndex + 1, symbol.Length - startIndex - 1);
                symbol = symbol.Substring(0, startIndex);
            }

            return symbol;
        }
    }
}
