# Dignus logging

- [Dignus.Log](./publish/Dignus.Log.md)
- [Dignus.Extensions.Logging](./publish/Dignus.Extensions.Logging.md)

## Benchmarks (2026-10-16)

Results are the median of seven runs on .NET 10.0.12 (Release, workstation GC, tiered compilation disabled). One logger was shared by 1, 4, or 8 producer threads. The numbers apply to these configurations, not to every logging workload.

File output: 100,000 messages of 64 bytes per line, including final disposal. Files stayed open and every write was flushed. Output content, order, and byte count were verified. Dignus and NLog log the message directly; Serilog uses a structured message template, so allocation figures do not represent identical call shapes. [Benchmark code](./Benchmarks/LoggerFileComparison/Program.cs).

**Time (ns/log)**

| Producers | Dignus.Log | NLog | Serilog |
| :--- | ---: | ---: | ---: |
| 1 | 2,184.51 | 2,207.82 | 2,340.35 |
| 4 | 2,126.50 | 3,138.21 | 3,076.96 |
| 8 | 2,071.91 | 3,150.50 | 3,044.54 |

**Allocation (B/log)**

| Producers | Dignus.Log | NLog | Serilog |
| :--- | ---: | ---: | ---: |
| 1 | 80.00 | 120.01 | 360.00 |
| 4 | 89.71 | 120.01 | 360.01 |
| 8 | 90.20 | 120.01 | 360.01 |

No-I/O: 1,048,576 calls with a pre-existing message, without rendering or physical output. [Benchmark code](./Benchmarks/LoggerLibraryComparison/Program.cs).

**Time (ns/log)**

| Producers | Dignus.Log | NLog default | NLog concurrent | Serilog |
| :--- | ---: | ---: | ---: | ---: |
| 1 | 74.03 | 41.55 | 41.81 | 97.77 |
| 4 | 81.86 | 101.24 | 100.44 | 80.71 |
| 8 | 99.69 | 143.45 | 139.14 | 134.78 |

**Allocation (B/log)**

| Producers | Dignus.Log | NLog default | NLog concurrent | Serilog |
| :--- | ---: | ---: | ---: | ---: |
| 1 | 56.00 | 120.00 | 120.00 | 160.00 |
| 4 | 56.06 | 120.00 | 120.00 | 160.00 |
| 8 | 58.95 | 120.00 | 120.00 | 160.00 |
