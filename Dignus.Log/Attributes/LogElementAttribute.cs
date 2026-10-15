// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using System;

namespace Dignus.Log.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class LogElementAttribute : System.Attribute
    {
        public string ElementName { get; private set; }
        public LogElementAttribute(string elementName)
        {
            ElementName = elementName;
        }
    }
}
