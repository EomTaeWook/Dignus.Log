using Dignus.Log;
using Dignus.Log.LogTarget.Interfaces;
using Dignus.Log.Model;
using Serilog;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;

internal static class Program
{
    private static int Main(string[] arguments)
    {
        string libraryVariant = arguments[0];
        int producerCount = int.Parse(arguments[1], CultureInfo.InvariantCulture);
        bool verifyDelivery = arguments[2] == "verify";
        string outputPath = arguments[3];
        int messageCount = verifyDelivery ? 262144 : 1048576;
        int messagesPerProducer = messageCount / producerCount;
        Dignus.Log.ILogger dignusLogger = null;
        NLog.LogFactory nlogFactory = null;
        NLog.Logger nlogLogger = null;
        Serilog.Core.Logger serilogLogger = null;
        object benchmarkResult;
        int exitCode = 0;
        bool loggersDisposed = false;

        try
        {
            string[][] messages = null;
            int[][] messageDeliveryCounts = null;
            int[] nextMessageIndices = null;
            int validationErrors = 0;
            int activeWriters = 0;
            Action<string> onLogWrite = null;
            if (verifyDelivery)
            {
                messages = new string[producerCount][];
                messageDeliveryCounts = new int[producerCount][];
                nextMessageIndices = new int[producerCount];
                for (int producerIndex = 0; producerIndex < producerCount; ++producerIndex)
                {
                    messages[producerIndex] = new string[messagesPerProducer];
                    messageDeliveryCounts[producerIndex] = new int[messagesPerProducer];
                    for (int messageIndex = 0; messageIndex < messagesPerProducer; ++messageIndex)
                    {
                        messages[producerIndex][messageIndex] = producerIndex.ToString(CultureInfo.InvariantCulture) + ":" + messageIndex.ToString(CultureInfo.InvariantCulture);
                    }
                }
                onLogWrite = message =>
                {
                    if (Interlocked.Increment(ref activeWriters) != 1)
                    {
                        Interlocked.Increment(ref validationErrors);
                    }
                    try
                    {
                        int separatorIndex = message.IndexOf(':');
                        if (separatorIndex <= 0)
                        {
                            Interlocked.Increment(ref validationErrors);
                            return;
                        }
                        if (int.TryParse(message.AsSpan(0, separatorIndex), CultureInfo.InvariantCulture, out int producerIndex) == false)
                        {
                            Interlocked.Increment(ref validationErrors);
                            return;
                        }
                        if (int.TryParse(message.AsSpan(separatorIndex + 1), CultureInfo.InvariantCulture, out int messageIndex) == false)
                        {
                            Interlocked.Increment(ref validationErrors);
                            return;
                        }
                        if ((uint)producerIndex >= producerCount)
                        {
                            Interlocked.Increment(ref validationErrors);
                            return;
                        }
                        if ((uint)messageIndex >= messagesPerProducer)
                        {
                            Interlocked.Increment(ref validationErrors);
                            return;
                        }
                        if (Interlocked.Increment(ref messageDeliveryCounts[producerIndex][messageIndex]) != 1)
                        {
                            Interlocked.Increment(ref validationErrors);
                        }
                        if (nextMessageIndices[producerIndex] != messageIndex)
                        {
                            Interlocked.Increment(ref validationErrors);
                        }
                        nextMessageIndices[producerIndex] = messageIndex + 1;
                    }
                    finally
                    {
                        Interlocked.Decrement(ref activeWriters);
                    }
                };
            }

            switch (libraryVariant)
            {
                case "dignus":
                case "dignus-async":
                    var logConfiguration = new LogConfiguration();
                    logConfiguration.AddLogger(string.Empty, LoggerType.Sync);
                    foreach (string loggerName in new[] { "warmup", "benchmark" })
                    {
                        logConfiguration.AddLogger(loggerName, libraryVariant == "dignus-async" ? LoggerType.Async : LoggerType.Sync).AddLogTarget(Dignus.Log.LogLevel.Info, new DignusTarget(onLogWrite));
                    }
                    LogBuilder.Configuration(logConfiguration).Build();
                    break;
            }

            void CreateLoggers(string loggerName)
            {
                switch (libraryVariant)
                {
                    case "dignus":
                    case "dignus-async":
                        dignusLogger = LogManager.GetLogger(loggerName);
                        break;
                    case "nlog":
                    case "nlog-concurrent":
                    case "nlog-async":
                        nlogFactory = new NLog.LogFactory { ThrowExceptions = verifyDelivery };
                        NLogTarget nlogTarget = libraryVariant == "nlog-concurrent" ? new ConcurrentNLogTarget(onLogWrite) : new NLogTarget(onLogWrite);
                        nlogTarget.Name = "benchmark";
                        var nlogConfiguration = new NLog.Config.LoggingConfiguration();
                        NLog.Targets.Target configuredNlogTarget = nlogTarget;
                        if (libraryVariant == "nlog-async")
                        {
                            configuredNlogTarget = new NLog.Targets.Wrappers.AsyncTargetWrapper(nlogTarget, 10000, NLog.Targets.Wrappers.AsyncTargetWrapperOverflowAction.Grow) { Name = "benchmark-async" };
                        }
                        nlogConfiguration.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, configuredNlogTarget);
                        nlogFactory.Configuration = nlogConfiguration;
                        nlogLogger = nlogFactory.GetLogger("benchmark");
                        Assert(nlogLogger.IsInfoEnabled, "NLog Info is disabled.");
                        break;
                    case "serilog":
                    case "serilog-async":
                        var serilogConfiguration = new Serilog.LoggerConfiguration().MinimumLevel.Information();
                        if (libraryVariant == "serilog-async")
                        {
                            serilogConfiguration.WriteTo.Async(sinkConfiguration => sinkConfiguration.Sink(new SerilogSink(onLogWrite)), bufferSize: int.MaxValue, blockWhenFull: false);
                        }
                        else
                        {
                            serilogConfiguration.WriteTo.Sink(new SerilogSink(onLogWrite));
                        }
                        serilogLogger = serilogConfiguration.CreateLogger();
                        Assert(serilogLogger.IsEnabled(Serilog.Events.LogEventLevel.Information), "Serilog Information is disabled.");
                        break;
                    default:
                        throw new ArgumentException("Unknown variant.");
                }
            }
            CreateLoggers(verifyDelivery ? "benchmark" : "warmup");
            Action closeLoggers = () =>
            {
                dignusLogger?.Dispose();
                nlogFactory?.Flush(TimeSpan.FromSeconds(30));
                nlogFactory?.Dispose();
                serilogLogger?.Dispose();
                loggersDisposed = true;
            };

            if (verifyDelivery == false)
            {
                Measure(dignusLogger, nlogLogger, serilogLogger, producerCount, 65536, null, closeLoggers);
                CreateLoggers("benchmark");
                loggersDisposed = false;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            var measurement = Measure(dignusLogger, nlogLogger, serilogLogger, producerCount, messageCount, messages, closeLoggers);
            if (verifyDelivery)
            {
                Assert(validationErrors == 0, "Invalid, duplicated, or reordered message.");
                for (int producerIndex = 0; producerIndex < producerCount; ++producerIndex)
                {
                    Assert(nextMessageIndices[producerIndex] == messagesPerProducer, "Delivered message count differs.");
                    foreach (int deliveryCount in messageDeliveryCounts[producerIndex])
                    {
                        Assert(deliveryCount == 1, "Missing or duplicated message.");
                    }
                }
            }
            benchmarkResult = new
            {
                Variant = libraryVariant,
                Producers = producerCount,
                Mode = verifyDelivery ? "verify" : "benchmark",
                Status = "success",
                MessageCount = messageCount,
                WarmupCount = verifyDelivery ? 0 : 65536,
                TimingIncludesFinalDispose = true,
                NanosecondsPerLog = measurement.NanosecondsPerLog,
                AllocatedBytes = measurement.AllocatedBytes,
                BytesPerLog = measurement.AllocatedBytes / (double)messageCount,
                Gen0Collections = measurement.Gen0Collections,
                Gen1Collections = measurement.Gen1Collections,
                Gen2Collections = measurement.Gen2Collections,
                GcPauseMilliseconds = measurement.GcPauseMilliseconds,
                Runtime = RuntimeInformation.FrameworkDescription,
                ProcessorCount = Environment.ProcessorCount,
                TieredCompilation = Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"),
                ServerGc = System.Runtime.GCSettings.IsServerGC,
                OutputSerialization = libraryVariant.EndsWith("-async", StringComparison.Ordinal) ? "Asynchronous queue; library-specific target synchronization" : "Synchronous writes; library-specific target synchronization",
                QueuePolicy = libraryVariant switch
                {
                    "dignus-async" => "Unbounded",
                    "nlog-async" => "Grow",
                    "serilog-async" => "Capacity=int.MaxValue",
                    _ => "No asynchronous queue"
                },
                NLogAssembly = typeof(NLog.Logger).Assembly.FullName,
                SerilogAssembly = typeof(Serilog.Core.Logger).Assembly.FullName,
                TimeUtc = DateTime.UtcNow
            };
        }
        catch (Exception exception)
        {
            benchmarkResult = new { Variant = libraryVariant, Producers = producerCount, Status = "failed", Error = exception.ToString() };
            exitCode = 1;
        }
        finally
        {
            if (loggersDisposed == false)
            {
                dignusLogger?.Dispose();
                nlogFactory?.Dispose();
                serilogLogger?.Dispose();
            }
        }

        string resultJson = JsonSerializer.Serialize(benchmarkResult, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, resultJson);
        Console.WriteLine(resultJson);
        return exitCode;
    }

    private static (double NanosecondsPerLog, long AllocatedBytes, int Gen0Collections, int Gen1Collections, int Gen2Collections, double GcPauseMilliseconds) Measure(Dignus.Log.ILogger dignusLogger, NLog.Logger nlogLogger, Serilog.Core.Logger serilogLogger, int producerCount, int totalMessageCount, string[][] messages, Action closeLoggers)
    {
        int messagesPerProducer = totalMessageCount / producerCount;
        using var startGate = new ManualResetEventSlim(false);
        using var producersReady = new CountdownEvent(producerCount);
        var producerThreads = new Thread[producerCount];
        var producerErrors = new Exception[producerCount];
        for (int producerIndex = 0; producerIndex < producerCount; ++producerIndex)
        {
            int currentProducerIndex = producerIndex;
            producerThreads[producerIndex] = new Thread(() =>
            {
                producersReady.Signal();
                startGate.Wait();
                try
                {
                    if (dignusLogger != null)
                    {
                        for (int messageIndex = 0; messageIndex < messagesPerProducer; ++messageIndex)
                        {
                            string message = messages == null ? "benchmark" : messages[currentProducerIndex][messageIndex];
                            dignusLogger.Write(new Dignus.Log.Model.LogEvent(Dignus.Log.LogLevel.Info, message, "benchmark", 0, dignusLogger.Name));
                        }
                    }
                    else if (nlogLogger != null)
                    {
                        for (int messageIndex = 0; messageIndex < messagesPerProducer; ++messageIndex)
                        {
                            string message = messages == null ? "benchmark" : messages[currentProducerIndex][messageIndex];
                            nlogLogger.Info(message);
                        }
                    }
                    else
                    {
                        for (int messageIndex = 0; messageIndex < messagesPerProducer; ++messageIndex)
                        {
                            string message = messages == null ? "benchmark" : messages[currentProducerIndex][messageIndex];
                            serilogLogger.Information(message);
                        }
                    }
                }
                catch (Exception exception)
                {
                    producerErrors[currentProducerIndex] = exception;
                }
            });
            producerThreads[producerIndex].IsBackground = true;
            producerThreads[producerIndex].Start();
        }

        Assert(producersReady.Wait(10000), "Benchmark threads were not ready.");
        int gen0CollectionsBefore = GC.CollectionCount(0);
        int gen1CollectionsBefore = GC.CollectionCount(1);
        int gen2CollectionsBefore = GC.CollectionCount(2);
        TimeSpan gcPauseBefore = GC.GetTotalPauseDuration();
        long allocatedBefore = GC.GetTotalAllocatedBytes(true);
        long startTimestamp = Stopwatch.GetTimestamp();
        startGate.Set();
        foreach (Thread producerThread in producerThreads)
        {
            Assert(producerThread.Join(30000), "Producer timed out.");
        }
        foreach (Exception producerError in producerErrors)
        {
            Assert(producerError == null, "Producer failed: " + producerError);
        }
        closeLoggers?.Invoke();
        long elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
        double gcPauseMilliseconds = (GC.GetTotalPauseDuration() - gcPauseBefore).TotalMilliseconds;
        int gen0Collections = GC.CollectionCount(0) - gen0CollectionsBefore;
        int gen1Collections = GC.CollectionCount(1) - gen1CollectionsBefore;
        int gen2Collections = GC.CollectionCount(2) - gen2CollectionsBefore;
        long allocatedBytes = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
        return (elapsedTicks * (1000000000.0 / Stopwatch.Frequency) / totalMessageCount, allocatedBytes, gen0Collections, gen1Collections, gen2Collections, gcPauseMilliseconds);
    }

    private static void Assert(bool condition, string message)
    {
        if (condition == false)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class DignusTarget : ILogTarget
    {
        private readonly Action<string> _onWrite;
        public DignusTarget(Action<string> onWrite)
        {
            _onWrite = onWrite;
        }
        public void Write(Dignus.Log.Model.LogEvent logEvent)
        {
            if (_onWrite != null)
            {
                _onWrite(logEvent.Message);
            }
        }
        public void Dispose() { }
    }

    private class NLogTarget : NLog.Targets.Target
    {
        private readonly Action<string> _onWrite;
        public NLogTarget(Action<string> onWrite)
        {
            _onWrite = onWrite;
        }
        protected override void Write(NLog.LogEventInfo logEvent)
        {
            if (_onWrite != null)
            {
                _onWrite(logEvent.FormattedMessage);
            }
        }
    }

    private sealed class ConcurrentNLogTarget : NLogTarget
    {
        private readonly object _outputLock = new object();
        public ConcurrentNLogTarget(Action<string> onWrite) : base(onWrite)
        {
        }
        protected override void WriteAsyncThreadSafe(NLog.Common.AsyncLogEventInfo logEvent)
        {
            lock (_outputLock)
            {
                base.Write(logEvent);
            }
        }
    }

    private sealed class SerilogSink : Serilog.Core.ILogEventSink
    {
        private readonly Action<string> _onWrite;
        private readonly object _outputLock = new object();
        public SerilogSink(Action<string> onWrite)
        {
            _onWrite = onWrite;
        }
        public void Emit(Serilog.Events.LogEvent logEvent)
        {
            lock (_outputLock)
            {
                if (_onWrite != null)
                {
                    _onWrite(logEvent.MessageTemplate.Text);
                }
            }
        }
    }
}


