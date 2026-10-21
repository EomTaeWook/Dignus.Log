using Dignus.Log.Formatting;
using Dignus.Log.LogTarget;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;

internal static class Program
{
    private const int WarmupCount = 4096;
    private const int MessageLength = 62;
    private const int FormattedPrefixLength = 33;

    private static int Main(string[] arguments)
    {
        string libraryVariant = arguments[0];
        int producerCount = int.Parse(arguments[1], CultureInfo.InvariantCulture);
        int messageCount = int.Parse(arguments[2], CultureInfo.InvariantCulture);
        string resultPath = Path.GetFullPath(arguments[3]);
        string logPath = Path.GetFullPath(arguments[4]);
        bool includeFormatting = false;
        bool bufferedOutput = false;
        if (arguments.Length > 5)
        {
            if (arguments[5] == "formatted")
            {
                includeFormatting = true;
            }
            else if (arguments[5] == "buffered")
            {
                bufferedOutput = true;
            }
            else if (arguments[5] == "formatted-buffered")
            {
                includeFormatting = true;
                bufferedOutput = true;
            }
            else
            {
                throw new ArgumentException("Unknown format variant.");
            }
        }
        int bytesPerLine = MessageLength + Environment.NewLine.Length;
        if (includeFormatting)
        {
            bytesPerLine += FormattedPrefixLength;
        }
        Dignus.Log.ILogger dignusLogger = null;
        NLog.LogFactory nlogFactory = null;
        NLog.Logger nlogLogger = null;
        Serilog.Core.Logger serilogLogger = null;
        bool loggersDisposed = false;
        var loggingErrors = new ConcurrentQueue<string>();
        object benchmarkResult;
        int exitCode = 0;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            File.WriteAllBytes(logPath, Array.Empty<byte>());
            var utf8Encoding = new UTF8Encoding(false);
            void CreateLoggers()
            {
                switch (libraryVariant)
                {
                    case "dignus":
                    case "dignus-async":
                        var dignusTarget = new FileLogTarget
                        {
                            LogFileName = logPath,
                            KeepOpenFile = true,
                            AutoFlush = bufferedOutput == false,
                            ArchiveRollingType = Dignus.Log.FileRollingType.Day,
                            ArchiveFileName = Path.Combine(Path.GetDirectoryName(logPath), "archive-{#}.log"),
                            LogFormatRenderer = new LogFormatRenderer(includeFormatting ? "${datetime} | ${level} | ${message}" : "${message}")
                        };
                        if (libraryVariant == "dignus-async")
                        {
                            var asynchronousLogger = new Dignus.Log.AsyncLogger();
                            asynchronousLogger._logTargetsByLevel[(int)Dignus.Log.LogLevel.Info].AddTarget(dignusTarget);
                            dignusLogger = asynchronousLogger;
                        }
                        else
                        {
                            var synchronousLogger = new Dignus.Log.Logger();
                            synchronousLogger._logTargetsByLevel[(int)Dignus.Log.LogLevel.Info].AddTarget(dignusTarget);
                            dignusLogger = synchronousLogger;
                        }
                        break;
                    case "nlog":
                    case "nlog-async":
                        var nlogTarget = new NLog.Targets.FileTarget("benchmark")
                        {
                            FileName = logPath,
                            Layout = includeFormatting ? @"${date:format=yyyy-MM-dd HH\:mm\:ss.fff} | ${level} | ${message}" : "${message}",
                            Encoding = utf8Encoding,
                            WriteBom = false,
                            KeepFileOpen = true,
                            AutoFlush = bufferedOutput == false,
                            BufferSize = 4096,
                            ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                            ArchiveAboveSize = 0
                        };
                        nlogFactory = new NLog.LogFactory();
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
                        Serilog.Debugging.SelfLog.Enable(message => loggingErrors.Enqueue(message));
                        string serilogPath = Path.Combine(Path.GetDirectoryName(logPath), "output-.log");
                        var serilogConfiguration = new Serilog.LoggerConfiguration().MinimumLevel.Information();
                        if (libraryVariant == "serilog-async")
                        {
                            serilogConfiguration.WriteTo.Async(sinkConfiguration => sinkConfiguration.File(serilogPath, outputTemplate: includeFormatting ? "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} | {Level:w4} | {Message:lj}{NewLine}" : "{Message:lj}{NewLine}", formatProvider: CultureInfo.InvariantCulture, fileSizeLimitBytes: null, buffered: bufferedOutput, shared: false, rollingInterval: RollingInterval.Day, retainedFileCountLimit: null, encoding: utf8Encoding), bufferSize: int.MaxValue, blockWhenFull: false);
                        }
                        else
                        {
                            serilogConfiguration.WriteTo.File(serilogPath, outputTemplate: includeFormatting ? "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} | {Level:w4} | {Message:lj}{NewLine}" : "{Message:lj}{NewLine}", formatProvider: CultureInfo.InvariantCulture, fileSizeLimitBytes: null, buffered: bufferedOutput, shared: false, rollingInterval: RollingInterval.Day, retainedFileCountLimit: null, encoding: utf8Encoding);
                        }
                        serilogLogger = serilogConfiguration.CreateLogger();
                        Assert(serilogLogger.IsEnabled(Serilog.Events.LogEventLevel.Information), "Serilog Information is disabled.");
                        break;
                    default:
                        throw new ArgumentException("Unknown variant.");
                }
            }
            CreateLoggers();
            Action closeLoggers = () =>
            {
                dignusLogger?.Dispose();
                nlogFactory?.Flush(TimeSpan.FromSeconds(30));
                nlogFactory?.Dispose();
                serilogLogger?.Dispose();
                loggersDisposed = true;
            };

            string warmupMessage = new string('w', MessageLength);
            var warmupMessages = new string[producerCount][];
            for (int producerIndex = 0; producerIndex < producerCount; ++producerIndex)
            {
                warmupMessages[producerIndex] = new string[WarmupCount / producerCount];
                Array.Fill(warmupMessages[producerIndex], warmupMessage);
            }
            Run(dignusLogger, nlogLogger, serilogLogger, warmupMessages, null);
            if (bufferedOutput == false)
            {
                AssertFileLength(logPath, (long)WarmupCount * bytesPerLine, "Warmup file length differs; flush or encoding does not match.");
            }
            string probeMessage = new string('v', MessageLength);
            Run(dignusLogger, nlogLogger, serilogLogger, new[] { new[] { probeMessage } }, null);
            if (bufferedOutput == false)
            {
                AssertFileLength(logPath, (long)(WarmupCount + 1) * bytesPerLine, "Single-message flush was not visible before disposal.");
            }
            closeLoggers();
            AssertFileLength(logPath, (long)(WarmupCount + 1) * bytesPerLine, "Warmup output was not fully flushed.");
            CreateLoggers();
            loggersDisposed = false;

            int messagesPerProducer = messageCount / producerCount;
            var messages = new string[producerCount][];
            string messageBody = new string('x', 50);
            for (int producerIndex = 0; producerIndex < producerCount; ++producerIndex)
            {
                messages[producerIndex] = new string[messagesPerProducer];
                for (int messageIndex = 0; messageIndex < messagesPerProducer; ++messageIndex)
                {
                    messages[producerIndex][messageIndex] = producerIndex.ToString("D2", CultureInfo.InvariantCulture) + ":" + messageIndex.ToString("D8", CultureInfo.InvariantCulture) + ":" + messageBody;
                }
            }
            long[][] callDurationTicks = null;
            if (bufferedOutput)
            {
                callDurationTicks = new long[producerCount][];
                for (int producerIndex = 0; producerIndex < producerCount; ++producerIndex)
                {
                    callDurationTicks[producerIndex] = new long[messagesPerProducer];
                }
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var measurement = Run(dignusLogger, nlogLogger, serilogLogger, messages, closeLoggers, callDurationTicks);
            Assert(loggingErrors.IsEmpty, "Logging error: " + string.Join("; ", loggingErrors));
            long fileLength = new FileInfo(logPath).Length;
            Assert(fileLength == (long)(WarmupCount + 1 + messageCount) * bytesPerLine, "Final file length differs.");
            ValidateFile(logPath, warmupMessage, probeMessage, messages, includeFormatting);
            double? callLatencyP99Nanoseconds = null;
            double? callLatencyP999Nanoseconds = null;
            double? callLatencyMaxNanoseconds = null;
            if (callDurationTicks != null)
            {
                var sortedDurationTicks = new long[messageCount];
                for (int producerIndex = 0; producerIndex < producerCount; ++producerIndex)
                {
                    Array.Copy(callDurationTicks[producerIndex], 0, sortedDurationTicks, producerIndex * messagesPerProducer, messagesPerProducer);
                }
                Array.Sort(sortedDurationTicks);
                double nanosecondsPerTick = 1000000000.0 / Stopwatch.Frequency;
                callLatencyP99Nanoseconds = sortedDurationTicks[(int)Math.Ceiling(messageCount * 0.99) - 1] * nanosecondsPerTick;
                callLatencyP999Nanoseconds = sortedDurationTicks[(int)Math.Ceiling(messageCount * 0.999) - 1] * nanosecondsPerTick;
                callLatencyMaxNanoseconds = sortedDurationTicks[messageCount - 1] * nanosecondsPerTick;
            }

            benchmarkResult = new
            {
                Variant = libraryVariant,
                Producers = producerCount,
                Status = "success",
                MessageCount = messageCount,
                WarmupCount = WarmupCount + 1,
                BytesPerLine = bytesPerLine,
                FileLength = fileLength,
                NanosecondsPerLog = measurement.NanosecondsPerLog,
                CallLatencyP99Nanoseconds = callLatencyP99Nanoseconds,
                CallLatencyP999Nanoseconds = callLatencyP999Nanoseconds,
                CallLatencyMaxNanoseconds = callLatencyMaxNanoseconds,
                ElapsedMilliseconds = measurement.NanosecondsPerLog * messageCount / 1000000.0,
                AllocatedBytes = measurement.AllocatedBytes,
                BytesPerLog = measurement.AllocatedBytes / (double)messageCount,
                Gen0Collections = measurement.Gen0Collections,
                Gen1Collections = measurement.Gen1Collections,
                Gen2Collections = measurement.Gen2Collections,
                GcPauseMilliseconds = measurement.GcPauseMilliseconds,
                AutoFlush = bufferedOutput == false,
                BufferedOutput = bufferedOutput,
                QueuePolicy = libraryVariant switch
                {
                    "dignus-async" => "Unbounded",
                    "nlog-async" => "Grow",
                    "serilog-async" => "Capacity=int.MaxValue",
                    _ => "No asynchronous queue"
                },
                KeepOpenFile = true,
                ArchiveInterval = "Day; no rollover during measurement",
                Format = includeFormatting ? "yyyy-MM-dd HH:mm:ss.fff | level | message + CRLF; UTF-8 without BOM" : "message + CRLF; UTF-8 without BOM",
                TimingIncludesFinalDispose = true,
                Runtime = RuntimeInformation.FrameworkDescription,
                ProcessorCount = Environment.ProcessorCount,
                ServerGc = System.Runtime.GCSettings.IsServerGC,
                TieredCompilation = Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"),
                LogPath = logPath,
                TimeUtc = DateTime.UtcNow
            };
        }
        catch (Exception exception)
        {
            benchmarkResult = new { Variant = libraryVariant, Producers = producerCount, Status = "failed", Error = exception.ToString(), TimeUtc = DateTime.UtcNow };
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
            Serilog.Debugging.SelfLog.Disable();
        }

        string resultJson = JsonSerializer.Serialize(benchmarkResult, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(resultPath, resultJson);
        Console.WriteLine(resultJson);
        return exitCode;
    }

    private static (double NanosecondsPerLog, long AllocatedBytes, int Gen0Collections, int Gen1Collections, int Gen2Collections, double GcPauseMilliseconds) Run(Dignus.Log.ILogger dignusLogger, NLog.Logger nlogLogger, Serilog.Core.Logger serilogLogger, string[][] messages, Action closeLoggers, long[][] callDurationTicks = null)
    {
        int producerCount = messages.Length;
        int totalMessageCount = producerCount * messages[0].Length;
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
                        if (callDurationTicks == null)
                        {
                            foreach (string message in messages[currentProducerIndex])
                            {
                                dignusLogger.Write(new Dignus.Log.Model.LogEvent(Dignus.Log.LogLevel.Info, message, "benchmark", 0, dignusLogger.Name));
                            }
                        }
                        else
                        {
                            for (int messageIndex = 0; messageIndex < messages[currentProducerIndex].Length; ++messageIndex)
                            {
                                long callStartTimestamp = Stopwatch.GetTimestamp();
                                dignusLogger.Write(new Dignus.Log.Model.LogEvent(Dignus.Log.LogLevel.Info, messages[currentProducerIndex][messageIndex], "benchmark", 0, dignusLogger.Name));
                                callDurationTicks[currentProducerIndex][messageIndex] = Stopwatch.GetTimestamp() - callStartTimestamp;
                            }
                        }
                    }
                    else if (nlogLogger != null)
                    {
                        if (callDurationTicks == null)
                        {
                            foreach (string message in messages[currentProducerIndex])
                            {
                                nlogLogger.Info(message);
                            }
                        }
                        else
                        {
                            for (int messageIndex = 0; messageIndex < messages[currentProducerIndex].Length; ++messageIndex)
                            {
                                long callStartTimestamp = Stopwatch.GetTimestamp();
                                nlogLogger.Info(messages[currentProducerIndex][messageIndex]);
                                callDurationTicks[currentProducerIndex][messageIndex] = Stopwatch.GetTimestamp() - callStartTimestamp;
                            }
                        }
                    }
                    else
                    {
                        if (callDurationTicks == null)
                        {
                            foreach (string message in messages[currentProducerIndex])
                            {
                                serilogLogger.Information("{LogMessage:l}", message);
                            }
                        }
                        else
                        {
                            for (int messageIndex = 0; messageIndex < messages[currentProducerIndex].Length; ++messageIndex)
                            {
                                long callStartTimestamp = Stopwatch.GetTimestamp();
                                serilogLogger.Information("{LogMessage:l}", messages[currentProducerIndex][messageIndex]);
                                callDurationTicks[currentProducerIndex][messageIndex] = Stopwatch.GetTimestamp() - callStartTimestamp;
                            }
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
        Assert(producersReady.Wait(10000), "Producer threads were not ready.");
        int gen0CollectionsBefore = GC.CollectionCount(0);
        int gen1CollectionsBefore = GC.CollectionCount(1);
        int gen2CollectionsBefore = GC.CollectionCount(2);
        TimeSpan gcPauseBefore = GC.GetTotalPauseDuration();
        long allocatedBefore = GC.GetTotalAllocatedBytes(true);
        long startTimestamp = Stopwatch.GetTimestamp();
        startGate.Set();
        foreach (Thread producerThread in producerThreads)
        {
            Assert(producerThread.Join(60000), "Producer timed out.");
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

    private static void ValidateFile(string logPath, string warmupMessage, string probeMessage, string[][] messages, bool includeFormatting)
    {
        using var logReader = new StreamReader(logPath, new UTF8Encoding(false, true), false);
        for (int warmupIndex = 0; warmupIndex < WarmupCount; ++warmupIndex)
        {
            Assert(ValidateLine(logReader.ReadLine(), includeFormatting) == warmupMessage, "Warmup output differs.");
        }
        Assert(ValidateLine(logReader.ReadLine(), includeFormatting) == probeMessage, "Flush probe output differs.");
        var nextMessageIndices = new int[messages.Length];
        int totalLines = 0;
        string logLine;
        while ((logLine = logReader.ReadLine()) != null)
        {
            string message = ValidateLine(logLine, includeFormatting);
            Assert(message.Length == MessageLength, "Partial or merged line.");
            Assert(message[2] == ':', "Producer delimiter differs.");
            Assert(message[11] == ':', "Sequence delimiter differs.");
            int producerIndex = int.Parse(message.AsSpan(0, 2), CultureInfo.InvariantCulture);
            int messageIndex = int.Parse(message.AsSpan(3, 8), CultureInfo.InvariantCulture);
            Assert((uint)producerIndex < messages.Length, "Invalid producer index.");
            Assert((uint)messageIndex < messages[producerIndex].Length, "Invalid message index.");
            Assert(messageIndex == nextMessageIndices[producerIndex], "Missing, duplicated, or reordered message.");
            Assert(string.Equals(message, messages[producerIndex][messageIndex], StringComparison.Ordinal), "File content differs.");
            ++nextMessageIndices[producerIndex];
            ++totalLines;
        }
        Assert(totalLines == messages.Length * messages[0].Length, "Line count differs.");
        for (int producerIndex = 0; producerIndex < messages.Length; ++producerIndex)
        {
            Assert(nextMessageIndices[producerIndex] == messages[producerIndex].Length, "Producer message count differs.");
        }
    }

    private static string ValidateLine(string logLine, bool includeFormatting)
    {
        Assert(logLine != null, "Missing log line.");
        if (includeFormatting == false)
        {
            return logLine;
        }

        Assert(logLine.Length == FormattedPrefixLength + MessageLength, "Formatted line length differs.");
        Assert(DateTime.TryParseExact(logLine.AsSpan(0, 23), "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out _), "Timestamp format differs.");
        string levelSegment = logLine.Substring(23, 10);
        if (string.Equals(levelSegment, " | Info | ", StringComparison.Ordinal) == false)
        {
            Assert(string.Equals(levelSegment, " | info | ", StringComparison.Ordinal), "Level format differs.");
        }
        return logLine.Substring(FormattedPrefixLength);
    }

    private static void AssertFileLength(string logPath, long expectedLength, string message)
    {
        var timeout = Stopwatch.StartNew();
        var fileInfo = new FileInfo(logPath);
        while (true)
        {
            fileInfo.Refresh();
            if (fileInfo.Length == expectedLength)
            {
                return;
            }
            if (timeout.Elapsed >= TimeSpan.FromSeconds(10))
            {
                throw new InvalidOperationException(message);
            }
            Thread.Sleep(1);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (condition == false)
        {
            throw new InvalidOperationException(message);
        }
    }
}
