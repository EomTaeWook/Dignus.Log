# Dignus logging

- [Dignus.Log](./publish/Dignus.Log.md)
- [Dignus.Extensions.Logging](./publish/Dignus.Extensions.Logging.md)

Open [Dignus.Log.Benchmarks.sln](./Dignus.Log.Benchmarks.sln) for the benchmark projects. They reference the library project and use its public APIs.

```powershell
dotnet build Dignus.Log.Benchmarks.sln -c Release -p:GeneratePackageOnBuild=false
```

## Benchmarks (2026-10-26)

Measured from this working tree on .NET 10.0.12, Release, workstation GC, tiered compilation disabled. Machine: Intel Core i9-14900K (24 cores, 32 logical processors), 64 GB (4 × 16 GB, 4200 MT/s), Samsung SSD 990 PRO 1TB (NVMe) for log files, Windows 11 Pro 10.0.26200. Benchmark threads were not pinned to specific cores. Dependencies: Dignus 1.4.3 (local package), NLog 6.2.1, Serilog 4.4.0, Serilog.Sinks.File 7.0.0, and Serilog.Sinks.Async 2.1.0. One logger is shared by 1, 4, or 8 producer threads. Each metric is the median of nine separate runs.

`Logger` writes synchronously under a logger-level lock; the built-in file target also serializes its own writes with a lock. `AsyncLogger` uses an unbounded MPSC queue and a dedicated consumer thread. NLog async uses `AsyncTargetWrapper` with `OverflowAction.Grow`; other wrapper options retain their defaults. Serilog async uses `bufferSize: int.MaxValue, blockWhenFull: false`: this is a large finite limit, not a truly unbounded queue. No measured run reached that limit or lost events.

Timestamp acquisition also differs: Dignus reads `DateTime.Now` for every event, while NLog retains its default cached `FastLocalTimeSource`. The contribution of this difference to the timing results has not been isolated. [NLog time-source documentation](https://github.com/NLog/NLog/wiki/Time-Source).

For file output, each run uses a separate directory. Warmup output is fully drained, then fresh logger instances are created before measurement for every variant. Total time and allocation include final flush and disposal. GC counts and pause duration are deltas over the same interval; forced collections before measurement are excluded. Pause duration comes from `GC.GetTotalPauseDuration()`. Generation counts come from `GC.CollectionCount(0/1/2)`; these overlapping counters must not be added together. Individual log calls are not correlated with GC events, so maximum latency cannot be attributed solely to GC.

### Message-only buffered file output

400,000 messages per run, with message text only (64 UTF-8 bytes per line including CRLF). This still includes normal event creation, message rendering, encoding, and file output; it is not an isolated disk-write measurement. Files remain open, flushing after each write is disabled, and final flush and disposal are included. The call shape, per-call timer, and output validation match the formatted file test below. [Benchmark code](./Benchmarks/LoggerFileComparison/Program.cs), format argument: `buffered`.

**Total time per log (ns)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 213.81 | 162.73 | 270.88 | 199.62 | 337.46 | 367.97 |
| 4 | 330.80 | 180.56 | 453.39 | 225.92 | 407.81 | 639.63 |
| 8 | 360.13 | 197.14 | 422.76 | 245.91 | 414.19 | 652.41 |

**Allocation (B/log)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 80.02 | 85.68 | 120.03 | 166.56 | 360.04 | 360.72 |
| 4 | 80.02 | 91.33 | 120.03 | 173.45 | 360.04 | 381.02 |
| 8 | 80.02 | 92.60 | 120.03 | 172.16 | 360.04 | 381.02 |

**GC pause duration per run (ms)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.20 | 3.86 | 0.31 | 9.83 | 0.71 | 2.28 |
| 4 | 0.25 | 6.73 | 0.40 | 12.04 | 0.95 | 89.84 |
| 8 | 0.24 | 9.87 | 0.40 | 13.05 | 1.07 | 90.53 |

**GC collection counts (gen0 / gen1 / gen2)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 1 / 0 / 0 | 1 / 0 / 0 | 2 / 0 / 0 | 2 / 1 / 0 | 7 / 0 / 0 | 7 / 0 / 0 |
| 4 | 1 / 0 / 0 | 2 / 1 / 0 | 2 / 0 / 0 | 2 / 1 / 0 | 7 / 0 / 0 | 8 / 7 / 1 |
| 8 | 1 / 0 / 0 | 2 / 1 / 0 | 2 / 0 / 0 | 2 / 1 / 0 | 7 / 0 / 0 | 8 / 7 / 1 |

**Log-call p99 (µs)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 3.40 | 0.20 | 3.50 | 2.00 | 3.60 | 0.90 |
| 4 | 9.10 | 0.50 | 26.90 | 3.90 | 18.40 | 6.10 |
| 8 | 18.30 | 1.50 | 12.50 | 5.60 | 38.60 | 9.40 |

**Log-call maximum (ms; median of each run's maximum)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.24 | 3.88 | 1.66 | 5.93 | 2.36 | 0.61 |
| 4 | 0.64 | 6.12 | 2.03 | 7.55 | 2.41 | 23.65 |
| 8 | 1.32 | 6.29 | 2.06 | 8.07 | 2.52 | 23.99 |

### Formatted, buffered file output

400,000 messages per run, with `yyyy-MM-dd HH:mm:ss.fff | level | message` (97 UTF-8 bytes per line including CRLF). Message strings are prepared before measurement. Dignus creates a `LogEvent` and calls `ILogger.Write`; NLog receives the message string; Serilog receives `"{LogMessage:l}"` and an argument, so the call shapes and allocations differ. Dignus and NLog render `Info`; Serilog renders `info`.

Files remain open, and flushing after each write is disabled. Every file is checked for line count, timestamp format, level, message content, and per-producer order. Calls are individually timed with `Stopwatch`, which adds measurement overhead. [Benchmark code](./Benchmarks/LoggerFileComparison/Program.cs), format argument: `formatted-buffered`.

**Total time per log (ns)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 406.44 | 380.00 | 494.77 | 428.49 | 826.07 | 942.46 |
| 4 | 583.89 | 415.29 | 711.97 | 446.74 | 910.75 | 1,017.01 |
| 8 | 630.09 | 405.91 | 737.65 | 469.77 | 1,000.33 | 1,058.31 |

**Allocation (B/log)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 152.02 | 163.76 | 192.03 | 248.96 | 360.04 | 381.02 |
| 4 | 152.02 | 165.50 | 192.03 | 251.53 | 360.04 | 381.02 |
| 8 | 152.02 | 166.36 | 192.03 | 250.40 | 360.04 | 381.04 |

**GC pause duration per run (ms)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.35 | 19.42 | 0.51 | 32.39 | 0.73 | 80.67 |
| 4 | 0.44 | 21.59 | 0.62 | 37.90 | 0.92 | 88.06 |
| 8 | 0.45 | 19.69 | 0.68 | 36.91 | 1.18 | 88.94 |

**GC collection counts (gen0 / gen1 / gen2)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 3 / 0 / 0 | 3 / 2 / 0 | 4 / 0 / 0 | 4 / 3 / 0 | 7 / 0 / 0 | 8 / 7 / 1 |
| 4 | 3 / 0 / 0 | 3 / 2 / 0 | 4 / 0 / 0 | 4 / 3 / 0 | 7 / 0 / 0 | 8 / 7 / 1 |
| 8 | 3 / 0 / 0 | 3 / 2 / 0 | 4 / 0 / 0 | 4 / 3 / 0 | 7 / 0 / 0 | 8 / 7 / 1 |

**Log-call p99 (µs)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 3.70 | 0.20 | 3.80 | 3.10 | 4.40 | 4.30 |
| 4 | 60.00 | 0.60 | 52.80 | 3.90 | 63.90 | 5.80 |
| 8 | 164.30 | 1.20 | 230.00 | 5.70 | 247.00 | 8.30 |

**Log-call maximum (ms; median of each run's maximum)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.31 | 9.99 | 1.63 | 13.17 | 2.34 | 20.88 |
| 4 | 0.77 | 6.32 | 1.74 | 6.11 | 2.35 | 22.85 |
| 8 | 1.14 | 4.24 | 2.03 | 7.00 | 2.75 | 24.03 |

### No-I/O

1,048,576 calls per run with a pre-existing message, without rendering or physical output. Dignus uses a no-op target with no target-level lock. NLog default uses its standard target synchronization; NLog concurrent uses the benchmark's concurrent-target override. The Serilog benchmark sink protects its output callback with a lock. These results measure those configurations rather than universal library rankings. [Benchmark code](./Benchmarks/LoggerLibraryComparison/Program.cs).

**Total time per log (ns)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog default | NLog concurrent | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 49.60 | 140.00 | 44.51 | 42.56 | 133.95 | 85.63 | 325.75 |
| 4 | 67.66 | 78.69 | 52.27 | 60.15 | 121.53 | 51.36 | 354.06 |
| 8 | 137.24 | 65.69 | 121.54 | 121.60 | 170.58 | 126.15 | 389.64 |

Per-run ranges (ns/log) for the synchronous variants: 1 producer — Dignus 48.68–58.59, NLog default 42.74–45.95, Serilog 84.97–91.75; 4 producers — Dignus 34.46–102.81, NLog default 37.33–115.12, Serilog 35.15–114.73; 8 producers — Dignus 112.82–167.85, NLog default 90.14–163.34, Serilog 105.27–173.33. Where ranges overlap, the ranking is not a stable conclusion from these nine runs. No-I/O is a supplementary pipeline-cost test, separate from the two file-output workloads above.

**Allocation (B/log)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog default | NLog concurrent | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 56.00 | 56.14 | 120.00 | 120.00 | 136.84 | 160.00 | 160.13 |
| 4 | 56.00 | 56.21 | 120.00 | 120.00 | 126.84 | 160.00 | 176.02 |
| 8 | 56.00 | 56.77 | 120.00 | 120.00 | 127.42 | 160.00 | 192.04 |

**GC pause duration per run (ms)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog default | NLog concurrent | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.21 | 0.33 | 0.49 | 0.42 | 35.09 | 0.68 | 0.96 |
| 4 | 0.29 | 0.39 | 0.56 | 0.67 | 22.30 | 0.85 | 81.88 |
| 8 | 0.35 | 0.99 | 0.85 | 0.83 | 26.65 | 1.13 | 76.84 |

**GC collection counts (gen0 / gen1 / gen2)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog default | NLog concurrent | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 3 / 0 / 0 | 3 / 0 / 0 | 6 / 0 / 0 | 6 / 0 / 0 | 8 / 7 / 2 | 8 / 0 / 0 | 8 / 0 / 0 |
| 4 | 3 / 0 / 0 | 3 / 0 / 0 | 6 / 0 / 0 | 6 / 0 / 0 | 7 / 6 / 1 | 8 / 0 / 0 | 11 / 10 / 4 |
| 8 | 3 / 0 / 0 | 3 / 1 / 0 | 6 / 0 / 0 | 6 / 0 / 0 | 7 / 6 / 1 | 8 / 0 / 0 | 10 / 9 / 3 |

Variant names in both programs: `dignus`, `dignus-async`, `nlog`, `nlog-async`, `serilog`, and `serilog-async`. The No-I/O program also accepts `nlog-concurrent`. Its `verify` mode checks delivery, ordering, and serialized target access. For the file program, `buffered` selects message-only output and `formatted-buffered` adds the timestamp and level. Pass a separate directory per configuration; Serilog's rolling sink writes `output-yyyyMMdd.log`, so use that day's dated filename as the log-path argument.
