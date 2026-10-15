// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Collections;

namespace Dignus.Log.Model
{
    internal class OptionModel
    {
        public string Category { get; set; }
        public string Value { get; set; }
        public string Id { get; set; }
    }

    internal class LogTargetConfigModel
    {
        public TargetType LogTarget { get; set; }
        public string Id { get; set; }
        public string LoggerName { get; set; }
        public ArrayQueue<OptionModel> Options { get; set; } = new ArrayQueue<OptionModel>();
    }

    internal class DatabaseLogTargetConfigModel : LogTargetConfigModel
    {
        public ArrayQueue<OptionModel> DbAttributes { get; set; } = new ArrayQueue<OptionModel>();
    }
}
