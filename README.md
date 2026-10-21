# Dignus logging

- [Dignus.Log](./publish/Dignus.Log.md)
- [Dignus.Extensions.Logging](./publish/Dignus.Extensions.Logging.md)

## Benchmarks (2026-10-22)

Measured from this working tree on .NET 10.0.12, Release, workstation GC, tiered compilation disabled, with 12 logical processors. Dependencies: Dignus 1.4.3 (local package), NLog 6.2.1, Serilog 4.4.0, Serilog.Sinks.File 7.0.0, and Serilog.Sinks.Async 2.1.0. One logger is shared by 1, 4, or 8 producer threads. Each metric is the median of five separate runs.

`Logger` writes synchronously under a logger-level lock. `AsyncLogger` uses an unbounded MPSC queue and a dedicated consumer thread. NLog async uses `AsyncTargetWrapper` with `OverflowAction.Grow`; other wrapper options retain their defaults. Serilog async uses `bufferSize: int.MaxValue, blockWhenFull: false`: this is a large finite limit, not a truly unbounded queue. No measured run reached that limit or lost events.

Timestamp acquisition also differs: Dignus reads `DateTime.Now` for every event, while NLog retains its default cached `FastLocalTimeSource`. The contribution of this difference to the timing results has not been isolated. [NLog time-source documentation](https://github.com/NLog/NLog/wiki/Time-Source).

For file output, each run uses a separate directory. Warmup output is fully drained, then fresh logger instances are created before measurement for every variant. Total time and allocation include final flush and disposal. GC counts and pause duration are deltas over the same interval; forced collections before measurement are excluded. Pause duration comes from `GC.GetTotalPauseDuration()`. Generation counts come from `GC.CollectionCount(0/1/2)`; these overlapping counters must not be added together. Individual log calls are not correlated with GC events, so maximum latency cannot be attributed solely to GC.

### Message-only buffered file output

400,000 messages per run, with message text only (64 UTF-8 bytes per line including CRLF). This still includes normal event creation, message rendering, encoding, and file output; it is not an isolated disk-write measurement. Files remain open, flushing after each write is disabled, and final flush and disposal are included. The call shape, per-call timer, and output validation match the formatted file test below. [Benchmark code](./Benchmarks/LoggerFileComparison/Program.cs), format argument: `buffered`.

**Total time per log (ns)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 283.96 | 217.29 | 322.49 | 288.50 | 442.90 | 453.23 |
| 4 | 374.18 | 228.56 | 486.70 | 292.98 | 477.25 | 752.50 |
| 8 | 381.99 | 242.85 | 490.21 | 319.37 | 499.77 | 845.44 |

**Allocation (B/log)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 80.01 | 86.21 | 120.03 | 170.25 | 360.04 | 361.35 |
| 4 | 80.01 | 92.10 | 120.03 | 177.60 | 360.04 | 381.02 |
| 8 | 80.01 | 92.17 | 120.03 | 174.41 | 360.05 | 381.03 |

**GC pause duration per run (ms)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.38 | 10.64 | 0.54 | 24.99 | 1.36 | 25.24 |
| 4 | 0.40 | 18.00 | 0.72 | 26.30 | 1.86 | 127.53 |
| 8 | 0.45 | 18.55 | 0.84 | 32.61 | 2.17 | 148.77 |

**GC collection counts (gen0 / gen1 / gen2)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 3 / 0 / 0 | 3 / 1 / 0 | 5 / 0 / 0 | 5 / 4 / 0 | 15 / 0 / 0 | 15 / 9 / 0 |
| 4 | 3 / 0 / 0 | 4 / 3 / 0 | 5 / 0 / 0 | 5 / 4 / 0 | 15 / 0 / 0 | 16 / 15 / 1 |
| 8 | 3 / 0 / 0 | 4 / 3 / 0 | 5 / 0 / 0 | 5 / 4 / 0 | 15 / 0 / 0 | 16 / 15 / 1 |

**Log-call p99 (µs)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 2.90 | 0.20 | 2.90 | 2.00 | 3.40 | 0.90 |
| 4 | 8.10 | 0.60 | 31.10 | 2.80 | 26.80 | 5.10 |
| 8 | 8.10 | 3.30 | 34.00 | 5.80 | 127.20 | 8.10 |

**Log-call maximum (ms; median of each run's maximum)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.38 | 5.92 | 2.52 | 9.60 | 4.13 | 3.46 |
| 4 | 0.63 | 9.68 | 2.71 | 11.31 | 3.15 | 16.35 |
| 8 | 1.47 | 10.12 | 2.86 | 11.85 | 3.58 | 19.74 |

### Formatted, buffered file output

400,000 messages per run, with `yyyy-MM-dd HH:mm:ss.fff | level | message` (97 UTF-8 bytes per line including CRLF). Message strings are prepared before measurement. Dignus creates a `LogEvent` and calls `ILogger.Write`; NLog receives the message string; Serilog receives `"{LogMessage:l}"` and an argument, so the call shapes and allocations differ. Dignus and NLog render `Info`; Serilog renders `info`.

Files remain open, and flushing after each write is disabled. Every file is checked for line count, timestamp format, level, message content, and per-producer order. Calls are individually timed with `Stopwatch`, which adds measurement overhead. [Benchmark code](./Benchmarks/LoggerFileComparison/Program.cs), format argument: `formatted-buffered`.

**Total time per log (ns)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 509.89 | 485.93 | 665.29 | 554.72 | 1,010.01 | 1,257.51 |
| 4 | 749.24 | 534.16 | 870.81 | 583.23 | 1,138.50 | 1,296.86 |
| 8 | 707.65 | 502.30 | 831.84 | 597.09 | 1,271.36 | 1,385.16 |

**Allocation (B/log)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 152.02 | 164.16 | 192.03 | 252.17 | 360.07 | 381.02 |
| 4 | 152.02 | 166.53 | 192.03 | 253.93 | 360.04 | 381.02 |
| 8 | 152.02 | 166.72 | 192.03 | 252.97 | 360.04 | 381.03 |

**GC pause duration per run (ms)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.52 | 24.19 | 1.02 | 37.15 | 1.26 | 123.71 |
| 4 | 0.71 | 23.79 | 1.23 | 38.86 | 1.70 | 134.87 |
| 8 | 0.80 | 23.58 | 1.38 | 40.31 | 2.14 | 135.76 |

**GC collection counts (gen0 / gen1 / gen2)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 6 / 0 / 0 | 7 / 3 / 0 | 8 / 0 / 0 | 9 / 6 / 1 | 15 / 0 / 0 | 16 / 15 / 1 |
| 4 | 6 / 0 / 0 | 7 / 4 / 0 | 8 / 0 / 0 | 9 / 6 / 1 | 15 / 0 / 0 | 16 / 15 / 1 |
| 8 | 6 / 0 / 0 | 7 / 4 / 0 | 8 / 0 / 0 | 9 / 6 / 1 | 15 / 0 / 0 | 16 / 15 / 1 |

**Log-call p99 (µs)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 3.30 | 0.20 | 3.90 | 0.80 | 4.00 | 3.20 |
| 4 | 56.10 | 0.60 | 88.20 | 2.60 | 54.60 | 5.10 |
| 8 | 23.80 | 1.80 | 257.70 | 5.60 | 287.80 | 8.30 |

**Log-call maximum (ms; median of each run's maximum)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.37 | 9.57 | 3.14 | 10.89 | 3.20 | 14.27 |
| 4 | 1.45 | 8.47 | 2.71 | 11.66 | 3.14 | 17.35 |
| 8 | 2.05 | 9.16 | 3.09 | 11.48 | 4.17 | 18.59 |

### No-I/O

1,048,576 calls per run with a pre-existing message, without rendering or physical output. Dignus uses a no-op target with no target-level lock. NLog default uses its standard target synchronization; NLog concurrent uses the benchmark's concurrent-target override. The Serilog benchmark sink protects its output callback with a lock. These results measure those configurations rather than universal library rankings. [Benchmark code](./Benchmarks/LoggerLibraryComparison/Program.cs).

**Total time per log (ns)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog default | NLog concurrent | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 56.80 | 118.45 | 43.59 | 42.84 | 163.70 | 101.44 | 321.46 |
| 4 | 102.04 | 83.64 | 101.84 | 106.81 | 151.35 | 84.18 | 408.53 |
| 8 | 147.42 | 86.38 | 140.01 | 144.51 | 170.21 | 136.64 | 426.41 |

The synchronous rankings for 4 and 8 producers are not stable conclusions from these five runs. For 4 producers, Dignus ranged from 93.81–106.03 ns/log and Serilog from 66.47–110.61. For 8 producers, Dignus ranged from 144.72–150.93, NLog default from 132.57–158.44, and Serilog from 132.49–167.57. These ranges overlap. The single-producer difference between Dignus (54.58–59.32) and NLog default (42.53–45.12) was consistent across the five runs. No-I/O is a supplementary pipeline-cost test, separate from the two file-output workloads above.

**Allocation (B/log)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog default | NLog concurrent | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 56.00 | 56.13 | 120.00 | 120.00 | 132.66 | 160.00 | 160.03 |
| 4 | 56.00 | 56.25 | 120.00 | 120.00 | 127.53 | 160.00 | 176.02 |
| 8 | 56.00 | 57.98 | 120.00 | 120.00 | 127.45 | 160.00 | 192.05 |

**GC pause duration per run (ms)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog default | NLog concurrent | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.43 | 0.70 | 0.72 | 0.69 | 48.66 | 0.88 | 1.66 |
| 4 | 0.68 | 0.87 | 1.34 | 1.59 | 37.48 | 1.57 | 126.57 |
| 8 | 0.96 | 13.45 | 1.69 | 1.67 | 31.06 | 2.14 | 123.87 |

**GC collection counts (gen0 / gen1 / gen2)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog default | NLog concurrent | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 6 / 0 / 0 | 6 / 0 / 0 | 13 / 0 / 0 | 13 / 0 / 0 | 17 / 16 / 4 | 17 / 0 / 0 | 17 / 0 / 0 |
| 4 | 6 / 0 / 0 | 6 / 0 / 0 | 13 / 0 / 0 | 13 / 0 / 0 | 15 / 13 / 2 | 17 / 0 / 0 | 21 / 20 / 5 |
| 8 | 6 / 0 / 0 | 6 / 3 / 1 | 13 / 0 / 0 | 13 / 0 / 0 | 14 / 12 / 1 | 17 / 0 / 0 | 20 / 19 / 3 |

Variant names in both programs: `dignus`, `dignus-async`, `nlog`, `nlog-async`, `serilog`, and `serilog-async`. The No-I/O program also accepts `nlog-concurrent`. Its `verify` mode checks delivery, ordering, and serialized target access. For the file program, `buffered` selects message-only output and `formatted-buffered` adds the timestamp and level. Pass a separate directory per configuration; Serilog's rolling sink writes `output-yyyyMMdd.log`, so use that day's dated filename as the log-path argument.

### Earlier file output with flushing after every write

These earlier measurements used the dedicated-thread implementation before the `Logger` / `AsyncLogger` split: 100,000 messages, medians of seven runs, including final disposal. Output was validated. They use different flushing and measurement conditions from the buffered results above and do not describe the new synchronous `Logger`.

**Message-only time (ns/log; 64 bytes per line)**

| Producers | Dignus dedicated thread | NLog | Serilog |
| :--- | ---: | ---: | ---: |
| 1 | 1,539.38 | 1,777.29 | 1,793.13 |
| 4 | 1,523.97 | 2,432.80 | 2,454.82 |
| 8 | 1,535.19 | 2,417.17 | 2,445.31 |

**Message-only allocation (B/log)**

| Producers | Dignus dedicated thread | NLog | Serilog |
| :--- | ---: | ---: | ---: |
| 1 | 94.97 | 120.01 | 360.00 |
| 4 | 94.97 | 120.01 | 360.01 |
| 8 | 94.97 | 120.01 | 360.01 |

**Formatted time (ns/log; 97 bytes per line)**

| Producers | Dignus dedicated thread | NLog | Serilog |
| :--- | ---: | ---: | ---: |
| 1 | 1,794.01 | 2,062.85 | 2,496.40 |
| 4 | 1,819.39 | 3,099.06 | 3,162.45 |
| 8 | 1,826.28 | 3,142.41 | 3,174.48 |

**Formatted allocation (B/log)**

| Producers | Dignus dedicated thread | NLog | Serilog |
| :--- | ---: | ---: | ---: |
| 1 | 166.97 | 192.01 | 360.00 |
| 4 | 167.14 | 192.01 | 360.01 |
| 8 | 167.14 | 192.01 | 360.01 |
