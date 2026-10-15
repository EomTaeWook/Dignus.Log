// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Collections;
using Dignus.Log.LogTarget.Interfaces;
using Dignus.Log.Model;
using System;

namespace Dignus.Log
{
    internal class TargetsByLevelDispatcher : IDisposable
    {
        private readonly ArrayQueue<ILogTarget> _logTargets = new ArrayQueue<ILogTarget>();

        public void Write(LogEvent model)
        {
            for (int i = 0; i < _logTargets.Count; ++i)
            {
                _logTargets[i].Write(model);
            }
        }
        public void AddTarget(ILogTarget logTarget)
        {
            _logTargets.Add(logTarget);
        }
        public void Dispose()
        {
            for (int i = 0; i < _logTargets.Count; ++i)
            {
                _logTargets[i].Dispose();
            }
        }
    }
}
