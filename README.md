# Dignus.Log

English | [한국어](README.ko.md)

Configurable logging with synchronous and asynchronous loggers, console and file targets, and Microsoft logging integration.

## Requirements

Packages target netstandard2.1, net48, net481, net8.0, net9.0, and net10.0. Benchmark projects use .NET 10.

## Installation

```xml
<PackageReference Include="Dignus.Log" Version="3.0.0" />
```

## Quick start

Configure the default logger before retrieving it and writing messages.

```csharp
using Dignus.Log;
using Dignus.Log.LogTarget;
using Dignus.Log.Model;

var logConfiguration = new LogConfiguration();
logConfiguration.AddLogTarget(string.Empty, LogLevel.Info, new ConsoleLogTarget());
LogBuilder.Configuration(logConfiguration).Build();

ILogger logger = LogManager.GetDefaultLogger();
logger.Info("Application started");
```

## Usage

The API guide covers file targets, synchronous and asynchronous logging, caller information, and shutdown. Microsoft.Extensions.Logging integration has its own guide.

## Documentation

- [Logging API](publish/Dignus.Log.md)
- [Microsoft logging integration](publish/Dignus.Extensions.Logging.md)
- [Benchmark methodology and results](docs/Usage.md)

## License

MIT License. See [LICENSE](LICENSE).
