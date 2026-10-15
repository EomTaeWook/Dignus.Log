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

    private static int Main(string[] arguments)
    {
        string libraryVariant = arguments[0];
        int producerCount = int.Parse(arguments[1], CultureInfo.InvariantCulture);
        int messageCount = int.Parse(arguments[2], CultureInfo.InvariantCulture);
        string resultPath = Path.GetFullPath(arguments[3]);
        string logPath = Path.GetFullPath(arguments[4]);
        Dignus.Log.Logger dignusLogger = null;
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
            switch (libraryVariant)
            {
                case "dignus":
                    var dignusTarget = new FileLogTarget
                    {
                        LogFileName = logPath,
                        KeepOpenFile = true,
                        AutoFlush = true,
                        ArchiveRollingType = Dignus.Log.FileRollingType.Day,
                        ArchiveFileName = Path.Combine(Path.GetDirectoryName(logPath), "archive-{#}.log"),
                        LogFormatRenderer = new LogFormatRenderer("${message}")
                    };
                    dignusLogger = new Dignus.Log.Logger();
                    dignusLogger._logTargetsByLevel[(int)Dignus.Log.LogLevel.Info].AddTarget(dignusTarget);
                    break;
                case "nlog":
                    var nlogTarget = new NLog.Targets.FileTarget("benchmark")
                    {
                        FileName = logPath,
                        Layout = "${message}",
                        Encoding = utf8Encoding,
                        WriteBom = false,
                        KeepFileOpen = true,
                        AutoFlush = true,
                        BufferSize = 4096,
                        ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                        ArchiveAboveSize = 0
                    };
                    nlogFactory = new NLog.LogFactory();
                    var nlogConfiguration = new NLog.Config.LoggingConfiguration();
                    nlogConfiguration.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, nlogTarget);
                    nlogFactory.Configuration = nlogConfiguration;
                    nlogLogger = nlogFactory.GetLogger("benchmark");
                    Assert(nlogLogger.IsInfoEnabled, "NLog Info is disabled.");
                    break;
                case "serilog":
                    Serilog.Debugging.SelfLog.Enable(message => loggingErrors.Enqueue(message));
                    string serilogPath = Path.Combine(Path.GetDirectoryName(logPath), "output-.log");
                    serilogLogger = new Serilog.LoggerConfiguration()
                        .MinimumLevel.Information()
                        .WriteTo.File(serilogPath, outputTemplate: "{Message:lj}{NewLine}", formatProvider: CultureInfo.InvariantCulture, fileSizeLimitBytes: null, buffered: false, shared: false, rollingInterval: RollingInterval.Day, retainedFileCountLimit: null, encoding: utf8Encoding)
                        .CreateLogger();
                    Assert(serilogLogger.IsEnabled(Serilog.Events.LogEventLevel.Information), "Serilog Information is disabled.");
                    break;
                default:
                    throw new ArgumentException("Unknown variant.");
            }

            string warmupMessage = new string('w', MessageLength);
            var warmupMessages = new string[producerCount][];
            for (int producerIndex = 0; producerIndex < producerCount; ++producerIndex)
            {
                warmupMessages[producerIndex] = new string[WarmupCount / producerCount];
                Array.Fill(warmupMessages[producerIndex], warmupMessage);
            }
            Run(dignusLogger, nlogLogger, serilogLogger, warmupMessages, null);
            Assert(new FileInfo(logPath).Length == (long)WarmupCount * 64, "Warmup file length differs; flush or encoding does not match.");
            string probeMessage = new string('v', MessageLength);
            Run(dignusLogger, nlogLogger, serilogLogger, new[] { new[] { probeMessage } }, null);
            Assert(new FileInfo(logPath).Length == (long)(WarmupCount + 1) * 64, "Single-message flush was not visible before disposal.");

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
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Action closeLoggers = () =>
            {
                dignusLogger?.Dispose();
                nlogFactory?.Dispose();
                serilogLogger?.Dispose();
                loggersDisposed = true;
            };
            var measurement = Run(dignusLogger, nlogLogger, serilogLogger, messages, closeLoggers);
            Assert(loggingErrors.IsEmpty, "Logging error: " + string.Join("; ", loggingErrors));
            long fileLength = new FileInfo(logPath).Length;
            Assert(fileLength == (long)(WarmupCount + 1 + messageCount) * 64, "Final file length differs.");
            ValidateFile(logPath, warmupMessage, probeMessage, messages);

            benchmarkResult = new
            {
                Variant = libraryVariant,
                Producers = producerCount,
                Status = "success",
                MessageCount = messageCount,
                WarmupCount = WarmupCount + 1,
                BytesPerLine = 64,
                FileLength = fileLength,
                NanosecondsPerLog = measurement.NanosecondsPerLog,
                ElapsedMilliseconds = measurement.NanosecondsPerLog * messageCount / 1000000.0,
                AllocatedBytes = measurement.AllocatedBytes,
                BytesPerLog = measurement.AllocatedBytes / (double)messageCount,
                AutoFlush = true,
                KeepOpenFile = true,
                ArchiveInterval = "Day; no rollover during measurement",
                Format = "message + CRLF; UTF-8 without BOM",
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

    private static (double NanosecondsPerLog, long AllocatedBytes) Run(Dignus.Log.Logger dignusLogger, NLog.Logger nlogLogger, Serilog.Core.Logger serilogLogger, string[][] messages, Action closeLoggers)
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
                        foreach (string message in messages[currentProducerIndex])
                        {
                            dignusLogger.Info(message, "benchmark", 0);
                        }
                    }
                    else if (nlogLogger != null)
                    {
                        foreach (string message in messages[currentProducerIndex])
                        {
                            nlogLogger.Info(message);
                        }
                    }
                    else
                    {
                        foreach (string message in messages[currentProducerIndex])
                        {
                            serilogLogger.Information("{LogMessage:l}", message);
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
        long allocatedBytes = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
        return (elapsedTicks * (1000000000.0 / Stopwatch.Frequency) / totalMessageCount, allocatedBytes);
    }

    private static void ValidateFile(string logPath, string warmupMessage, string probeMessage, string[][] messages)
    {
        using var logReader = new StreamReader(logPath, new UTF8Encoding(false, true), false);
        for (int warmupIndex = 0; warmupIndex < WarmupCount; ++warmupIndex)
        {
            Assert(logReader.ReadLine() == warmupMessage, "Warmup output differs.");
        }
        Assert(logReader.ReadLine() == probeMessage, "Flush probe output differs.");
        var nextMessageIndices = new int[messages.Length];
        int totalLines = 0;
        string logLine;
        while ((logLine = logReader.ReadLine()) != null)
        {
            Assert(logLine.Length == MessageLength, "Partial or merged line.");
            Assert(logLine[2] == ':', "Producer delimiter differs.");
            Assert(logLine[11] == ':', "Sequence delimiter differs.");
            int producerIndex = int.Parse(logLine.AsSpan(0, 2), CultureInfo.InvariantCulture);
            int messageIndex = int.Parse(logLine.AsSpan(3, 8), CultureInfo.InvariantCulture);
            Assert((uint)producerIndex < messages.Length, "Invalid producer index.");
            Assert((uint)messageIndex < messages[producerIndex].Length, "Invalid message index.");
            Assert(messageIndex == nextMessageIndices[producerIndex], "Missing, duplicated, or reordered message.");
            Assert(string.Equals(logLine, messages[producerIndex][messageIndex], StringComparison.Ordinal), "File content differs.");
            ++nextMessageIndices[producerIndex];
            ++totalLines;
        }
        Assert(totalLines == messages.Length * messages[0].Length, "Line count differs.");
        for (int producerIndex = 0; producerIndex < messages.Length; ++producerIndex)
        {
            Assert(nextMessageIndices[producerIndex] == messages[producerIndex].Length, "Producer message count differs.");
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
