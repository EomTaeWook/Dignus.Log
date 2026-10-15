// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using System;

namespace Dignus.Log.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class LogTargetAttribute : Attribute
    {
        private string _logTarget;
        public LogTargetAttribute(string logTarget)
        {
            _logTarget = logTarget;
        }
        public string GetTagetName()
        {
            return _logTarget;
        }
    }
}
