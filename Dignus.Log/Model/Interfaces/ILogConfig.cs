// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

namespace Dignus.Log.Model.Interfaces
{
    public interface ILogTargetConfig
    {
        string LoggerKey { get; }

        TargetType TargetType { get; }
    }
}
