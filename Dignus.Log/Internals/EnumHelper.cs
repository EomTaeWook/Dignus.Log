// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using System;

namespace Dignus.Log.Internals
{
    internal class EnumHelper
    {
        public static T GetEnumType<T>(string value) where T : struct, Enum
        {
            if (Enum.TryParse<T>(value, true, out T enumValue) == true)
            {
                return enumValue;
            }
            throw new ArgumentException($"invalid value {typeof(T).Name}: {value}");
        }
    }
}
