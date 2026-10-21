# Dignus.Log

`Dignus.Log` routes messages to console, file, and database targets. Configure targets in C# or XML, then write through `LogHelper` or `ILogger`.

## Install

```powershell
dotnet add package Dignus.Log
```

## Configure in C#

This example configures the default synchronous logger to write `Info` and higher messages to a file. `Build()` creates the default logger after configuring its targets.

```csharp
using Dignus.Log;
using Dignus.Log.Formatting;
using Dignus.Log.LogTarget;
using Dignus.Log.Model;
using Dignus.Log.Rule;

var fileLogTarget = new FileLogTarget
{
    LogFileName = "./logs/app.log",
    ArchiveFileName = "./logs/archive/app.{#}.log",
    ArchiveRollingType = FileRollingType.Day,
    MaxArchiveFile = 7,
    KeepOpenFile = true,
    AutoFlush = true,
    LogFormatRenderer = new LogFormatRenderer("${datetime} | ${level} | ${message}")
};

var logConfiguration = new LogConfiguration();
string defaultLoggerName = typeof(LogManager).Assembly.GetName().Name;
logConfiguration.AddLogRule("file", new LoggerRule(defaultLoggerName, LogLevel.Info, fileLogTarget));

LogBuilder.Configuration(logConfiguration).Build();
ILogger logger = LogManager.GetDefaultLogger();
logger.Write(LogLevel.Info, "Application started", string.Empty, 0);
```

## Synchronous and asynchronous logging

### Logger: synchronous output

With the default `UseAsyncLogger` value (`false`), `GetDefaultLogger()` returns a synchronous `Logger`. It writes to its targets on the calling thread, holding one logger-level lock until output finishes. It has no event queue or worker thread. Concurrent callers wait for that lock.

Direct `ILogger.Write` calls require a level, message, caller file path, and caller line number. Use `string.Empty` and `0` when caller information is unnecessary, or use `LogHelper` below to capture it automatically.

### AsyncLogger: background output

To use an `AsyncLogger` instead, set `logConfiguration.UseAsyncLogger = true` before `Build()`. `GetDefaultLogger()` then returns the asynchronous logger with the configured targets. Choose this setting before the one-time build.

```csharp
logConfiguration.UseAsyncLogger = true;
LogBuilder.Configuration(logConfiguration).Build();
ILogger asynchronousLogger = LogManager.GetDefaultLogger();
asynchronousLogger.Write(LogLevel.Info, "Queued for background output", string.Empty, 0);
```

The asynchronous snippet replaces the build and logger retrieval in the first example; do not call `Build()` twice. `Logger` and `AsyncLogger` each implement `ILogger`. `AsyncLogger` uses an unbounded MPSC queue and one dedicated background thread. Calls return after enqueueing; target output runs on that thread. `Dispose()` drains queued events before disposing targets. Sustained input faster than output can grow the queue and its memory usage.

### LogHelper

`LogHelper` provides `Debug`, `Info`, `Error`, and `Fatal` overloads for strings and exceptions. String overloads capture the caller's file path and line number. After `Build()`, it uses the default logger automatically:

```csharp
LogHelper.Info("Application started with caller information");
```

`LogHelper.SetLogger(ILogger)` selects a different logger for subsequent static calls. That selection is shared by all `LogHelper` calls.

### Shutdown and logger names

Stop producing messages, then shut down the logger:

```csharp
LogManager.Dispose();
```

This waits for pending asynchronous writes and disposes the targets of registered loggers. The builder also registers a process-exit handler.

`GetDefaultLogger()` returns the logger created by `Build()`. `GetLogger(name)` only retrieves a previously registered logger; it does not create one. To use a custom `ILogger`, register it with `LogManager.RegisterLogger(logger)` before retrieving it by name.

Target write exceptions from `Logger` propagate to the logging caller. With `AsyncLogger`, they occur on the dedicated processing thread and cannot propagate to the logging caller. Neither logger retries failed events.

## Configure in XML

Give each target a unique `id`. A `<logger>` refers to that target through its `id`; multiple entries can use the same `loggerName` to route one logger to several targets.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <targets>
    <target id="consoleConfig" logTarget="console" format="${datetime} | ${level} | ${message}">
      <option category="color" id="Info" value="Green"/>
      <option category="color" id="Error" value="Red"/>
    </target>
    <target id="fileConfig" logTarget="file" format="${datetime} | ${level} | ${message}">
      <option category="fileName" value="./logs/app.log"/>
      <option category="archiveFileName" value="./logs/archive/app.{#}.log"/>
      <option category="archiveRolling" value="Day"/>
      <option category="maxArchiveFiles" value="7"/>
      <option category="keepOpenFile" value="true"/>
      <option category="autoFlush" value="true"/>
    </target>
  </targets>
  <loggers>
    <logger id="consoleConfig" minLogLevel="Debug"/>
    <logger id="fileConfig" minLogLevel="Info"/>
  </loggers>
</configuration>
```

```csharp
LogBuilder.Configuration(LogConfigXmlReader.Load("DignusLog.config")).Build();
LogHelper.Info("Application started");
```

`minLogLevel` accepts `Debug`, `Info`, `Error`, or `Fatal`; the chosen level and higher levels go to that target. If `loggerName` is omitted, the XML reader uses the `Dignus.Log` assembly name.

## Format elements

The default console and file format is `${datetime} | ${level} | ${message}`. Text outside a `${...}` element is written literally.

| Element | Output |
| :--- | :--- |
| `${datetime}` | Event time, using `yyyy-MM-dd HH:mm:ss.fff` |
| `${level}` | `Debug`, `Info`, `Error`, or `Fatal` |
| `${message}` | Message text |
| `${callsite}` | Caller file path and line number |
| `${callerFileName}` | Caller file name |
| `${callerFilePath}` | Caller file path |
| `${callerLineNumber}` | Caller line number |

Use these plain element names. The current parser does not resolve `${symbol:format}`.

## Target options

The C# properties and XML options below configure the built-in targets. Option category values are parsed case-insensitively.

### Console

| C# property | XML | Effect |
| :--- | :--- | :--- |
| `LogFormatRenderer` | `format` attribute | Output format |
| `DebugColor` | `<option category="color" id="Debug" value="..."/>` | Debug text color |
| `InfoColor` | `<option category="color" id="Info" value="..."/>` | Info text color |
| `ErrorColor` | `<option category="color" id="Error" value="..."/>` | Error text color |
| `FatalColor` | `<option category="color" id="Fatal" value="..."/>` | Fatal text color |

Color values use `System.ConsoleColor` names. By default, Debug and Info are white; Error and Fatal are red. A console target writes one line for each message.

### File

| C# property | XML option category | Effect |
| :--- | :--- | :--- |
| `LogFormatRenderer` | `format` attribute | Output format |
| `LogFileName` | `fileName` | Active file path |
| `ArchiveFileName` | `archiveFileName` | Archive path; `{#}` is replaced with the date suffix |
| `ArchiveRollingType` | `archiveRolling` | `Hour`, `Day`, `Week`, or `Month` |
| `MaxArchiveFile` | `maxArchiveFiles` | Archive limit; `0` means no deletion by count |
| `KeepOpenFile` | `keepOpenFile` | Keep the stream open between writes when `true` |
| `AutoFlush` | `autoFlush` | Flush the stream after each write when `true` |

The file target writes UTF-8 lines. Rolling is based on the active file's creation time. `Week` rolls after seven days; set `ArchiveFileName` when using rolling. `KeepOpenFile` and `AutoFlush` default to `false`.

### Database

A database target uses an ADO.NET `IDbConnection` provider. Configure it with `DatabaseLogTargetConfig` in C#, or with a `<target logTarget="database">` entry in XML:

```xml
<target id="databaseConfig" logTarget="database">
  <database dbConnection="connection string"
            dbProvider="Namespace.ConnectionType, AssemblyName"
            dbCommand="insert into log (message) values (@message)">
    <option category="parameter" id="@message" value="${message}"/>
  </database>
</target>
```

Add a matching `<logger id="databaseConfig" loggerName="app" minLogLevel="Info"/>` entry under `<loggers>` to enable it. Replace the provider type and connection string with values for your ADO.NET provider.

| C# configuration property | XML | Effect |
| :--- | :--- | :--- |
| `DbConnection` | `dbConnection` | Connection string |
| `DbProvider` | `dbProvider` | Connection type; XML resolves an assembly-qualified type name |
| `CommandTextRenderer` | `dbCommand` | Command text, rendered for each event |
| `DbParameters` | `<option category="parameter" id="..." value="..."/>` | Parameter name and rendered value |
| `KeepConnection` | — | Keep the connection open between writes when `true` |

`keepConnectionOpen` is currently not read from XML. Set `KeepConnection` through `DatabaseLogTargetConfig` if needed.

## Custom targets

Implement `ILogTarget.Write(LogEvent)` and `ILogTarget.Dispose()`, then add the target with `LogConfiguration.AddLogRule(...)` as in the C# example. Each logger serializes its own target calls: `Logger` uses a lock, and `AsyncLogger` uses its dedicated consumer thread. If several loggers share the same target instance, the target implementation must handle their concurrent calls.

## License

Released under the MIT License. See the package's `LICENSE` file.
