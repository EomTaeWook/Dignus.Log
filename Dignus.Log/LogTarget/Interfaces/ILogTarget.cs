// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Model;

namespace Dignus.Log.LogTarget.Interfaces
{
    public interface ILogTarget
    {
        void Write(LogEvent logMessage);
        void Dispose();
    }
}
