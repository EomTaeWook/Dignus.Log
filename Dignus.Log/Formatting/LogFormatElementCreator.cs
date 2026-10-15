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
        public static List<IFormatElement> Parse(string logFormPattern)
        {
            StringBuilder stringBuilder = new StringBuilder();

            ArrayQueue<char> reader = new ArrayQueue<char>();
            reader.AddRange(logFormPattern.ToCharArray());

            var logFormatElements = new List<IFormatElement>();
            int ch;
            while (reader.Count > 0)
            {
                ch = reader.Read();
                if (ch == '$' && reader.Peek() == '{')
                {
                    logFormatElements.Add(AddLitererFormat(stringBuilder));
                    logFormatElements.Add(ParseSymbol(reader));
                    continue;
                }
                stringBuilder.Append((char)ch);
            }
            if (stringBuilder.Length > 0)
            {
                logFormatElements.Add(AddLitererFormat(stringBuilder));
            }

            reader.Clear();
            return logFormatElements;

        }
        internal static LogFormatRenderer CreateDefaultRenderer()
        {
            return new LogFormatRenderer("${datetime} | ${level} | ${message}");
        }
        private static Literal AddLitererFormat(StringBuilder sb)
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
            int ch;
            while ((ch = reader.Read()) > 0)
            {
                stringBuilder.Append((char)ch);
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
            return null;
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
            }

            return symbol;
        }
    }
}
