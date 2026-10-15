// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Model;
using System.Text;

namespace Dignus.Log.Elements.Interfaces
{
    public interface IFormatElement
    {
        void SetFormat(string format);
        void Write(StringBuilder stringBuilder, LogEvent logMessageModel);
    }
}
