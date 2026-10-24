// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Collections;
using Dignus.Log.LogTarget.Interfaces;
using Dignus.Log.Model;
using System.Collections.Generic;

namespace Dignus.Log
{
    internal class TargetsByLevelDispatcher
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
        internal static void DisposeTargets(TargetsByLevelDispatcher[] logTargetsByLevel)
        {
            var logTargets = new UniqueSet<ILogTarget>();
            for (int i = 0; i < logTargetsByLevel.Length; ++i)
            {
                var dispatcher = logTargetsByLevel[i];
                for (int ii = 0; ii < dispatcher._logTargets.Count; ++ii)
                {
                    if (logTargets.Add(dispatcher._logTargets[ii]) == true)
                    {
                        dispatcher._logTargets[ii].Dispose();
                    }
                }
            }
        }
    }
}
