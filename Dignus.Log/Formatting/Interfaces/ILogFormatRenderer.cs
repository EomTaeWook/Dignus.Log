// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Model;
using System.Text;

namespace Dignus.Log.Formatting.Interfaces
{
    public interface ILogFormatRenderer
    {
        void RenderTo(StringBuilder stringBuilder, LogEvent logEvent);
    }
}
