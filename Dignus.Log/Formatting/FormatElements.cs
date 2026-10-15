// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Log.Attributes;
using Dignus.Log.Elements.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Dignus.Log.Formatting
{
    internal class FormatElements
    {
        private static readonly Dictionary<string, Func<IFormatElement>> _registry = new Dictionary<string, Func<IFormatElement>>();

        static FormatElements()
        {
            RegisterFromAssembly(typeof(FormatElements).Assembly);
        }

        internal static void RegisterFromAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                var logElementAttr = type.GetCustomAttribute<LogElementAttribute>();
                if (logElementAttr != null)
                {
                    Register(logElementAttr.ElementName, CreateInstanceDelegate(type));
                }
            }
        }
        public static void Register(string symbol, Func<IFormatElement> creator)
        {
            _registry[symbol] = creator;
        }

        internal static IFormatElement Create(string symbol)
        {
            if (_registry.TryGetValue(symbol, out Func<IFormatElement> creator) == true)
            {
                return creator();
            }
            return null;
        }

        internal static Func<IFormatElement> CreateInstanceDelegate(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
            {
                throw new InvalidOperationException($"type {type.FullName} does not have a parameterless constructor.");
            }
            var newExpression = Expression.New(constructor);
            var lambda = Expression.Lambda<Func<IFormatElement>>(newExpression);
            return lambda.Compile();
        }
    }
}
