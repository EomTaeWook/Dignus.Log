# Dignus.Log

`Dignus.Log` routes messages to console, file, and database targets. Configure targets in C# or XML, then write through `LogHelper` or `ILogger`.

## Install

```powershell
dotnet add package Dignus.Log
```

## Configure in C#

This example configures the default asynchronous logger to write `Info` and higher messages to a file. An empty logger name means the default logger. `Build()` creates the default logger after configuring its targets.

```csharp
using Dignus.Log;
using Dignus.Log.Formatting;
using Dignus.Log.LogTarget;
using Dignus.Log.Model;

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
logConfiguration.AddLogTarget(string.Empty, LogLevel.Info, fileLogTarget);

LogBuilder.Configuration(logConfiguration).Build();
ILogger logger = LogManager.GetDefaultLogger();
logger.Write(LogLevel.Info, "Application started", string.Empty, 0);
```

`ILogger.Write` takes a level, message, caller file path, and caller line number; the `Debug`, `Info`, `Error`, and `Fatal` extension methods on `ILogger` take the same arguments without the level. Use `string.Empty` and `0` when caller information is unnecessary, or use `LogHelper` below to capture it automatically.

## Synchronous and asynchronous logging

Each logger name has one logger type. When `AddLogTarget(name, minLogLevel, target)` first adds a name, its type is `LoggerType.Async`. Calling `AddLogTarget` again with the same name adds another target to the same logger; the chosen level and higher levels go to that target. An empty name means the default logger.

To select a logger's type explicitly, add it with `AddLogger(name, type)` before adding its targets, then chain its targets:

```csharp
logConfiguration.AddLogger("transaction", LoggerType.Sync)
    .AddLogTarget(LogLevel.Info, transactionFileTarget)
    .AddLogTarget(LogLevel.Error, consoleLogTarget);
```

`AddLogger` throws `InvalidOperationException` if the name already has a logger, including one created by `AddLogTarget`. Each logger's type is fixed when its name is first added. Add the targets before passing the configuration to `LogBuilder.Configuration(...)`.

### AsyncLogger: background output (default)

`AsyncLogger` uses an unbounded MPSC queue and one dedicated background thread. Calls return after enqueueing; target output runs on that thread. Target write exceptions are not caught: on that thread they are unhandled and terminate the process. `Dispose()` drains queued events before disposing targets; an event written concurrently with `Dispose()` may be dropped. Sustained input faster than output can grow the queue and its memory usage.

### Logger: synchronous output

`Logger` writes to its targets on the calling thread, holding one logger-level lock until output finishes. It has no event queue or worker thread. Concurrent callers wait for that lock. Target write exceptions propagate to the logging caller. To make the default logger synchronous, add it explicitly before adding its targets:

```csharp
var logConfiguration = new LogConfiguration();
logConfiguration.AddLogger(string.Empty, LoggerType.Sync).AddLogTarget(LogLevel.Info, fileLogTarget);
LogBuilder.Configuration(logConfiguration).Build();
```

Neither logger catches, retries, or drops target exceptions.

### Loggers by name

`GetLogger(name)` returns the registered logger with that name. For a name it has not seen, it creates a logger of the type configured for that name, attaches the targets added for that name, and registers it. A null or empty name returns the default logger. A name that has no configuration and is not registered throws `InvalidOperationException`. Calling it before `Build()` throws `InvalidOperationException`. The default logger is named after the entry assembly; `GetDefaultLogger()` returns it.

```csharp
ILogger auditLogger = LogManager.GetLogger("audit");
```

`Logger` and `AsyncLogger` are created only by `LogManager.GetLogger(name)` from the configuration.

A custom `ILogger` is registered with `LogManager.RegisterLogger(logger)` before retrieving it by name. Registering a name that already exists throws `InvalidOperationException`.

### LogHelper

`LogHelper` provides `Debug`, `Info`, `Error`, and `Fatal` overloads for strings and exceptions. String overloads capture the caller's file path and line number. Exception overloads write one event whose message is `Exception.ToString()` (type, message, stack trace, and inner exceptions); its location is the first stack frame with file information, or the caller when no frame has it. After `Build()`, it uses the default logger automatically:

```csharp
LogHelper.Info("Application started with caller information");
```

`LogHelper.SetLogger(ILogger)` selects a different logger for subsequent static calls. That selection is shared by all `LogHelper` calls.

### Shutdown

Stop producing messages, then shut down the logger:

```csharp
LogManager.Dispose();
```

This waits for pending asynchronous writes and disposes the targets of registered loggers. Each logger disposes each of its targets once. After it, `GetLogger(name)` throws `ObjectDisposedException`, and loggers obtained earlier ignore further writes. The builder also registers a process-exit handler that calls it.

`Build()` runs once per process; calling it again throws `InvalidOperationException`.

## Configure in XML

Give each target a unique `id`. Each `<logger>` declares one logger: `loggerName` names it, and `loggerType` (`Sync` or `Async`) sets its type. Inside it, each `<target>` refers to a target by `id` and sets the minimum level for that target. Several loggers can refer to the same target. A target id or a logger name can be declared only once, and a logger can refer only to a declared target; otherwise loading throws `InvalidOperationException`. `logTarget` accepts `console`, `file`, or `database`; other targets are added in C#.

Only targets referenced by a `<logger>` are created. An unused database target declaration does not load its provider assembly.

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
    <logger loggerName="" loggerType="Async">
      <target id="consoleConfig" minLogLevel="Debug"/>
      <target id="fileConfig" minLogLevel="Info"/>
    </logger>
  </loggers>
</configuration>
```

```csharp
LogBuilder.Configuration(LogConfigXmlReader.Load("DignusLog.config")).Build();
LogHelper.Info("Application started");
```

`minLogLevel` accepts `Debug`, `Info`, `Error`, or `Fatal`; the chosen level and higher levels go to that target. If `loggerName` is omitted or empty, the logger is the default logger. If `loggerType` is omitted, the logger is asynchronous.

## Format elements

The default console and file format is `${datetime} | ${level} | ${message}`; a null or empty format uses it. Text outside a `${...}` element is written literally, and so is a `${` without a closing `}`.

| Element | Output |
| :--- | :--- |
| `${datetime}` | Event time, using `yyyy-MM-dd HH:mm:ss.fff` |
| `${level}` | `Debug`, `Info`, `Error`, or `Fatal` |
| `${message}` | Message text |
| `${callsite}` | Caller file path and line number |
| `${callerFileName}` | Caller file name |
| `${callerFilePath}` | Caller file path |
| `${callerLineNumber}` | Caller line number |

Add a format after a colon to elements that support one: `${datetime:HH:mm:ss}` takes a `DateTime` format, and `${callerLineNumber:D5}` takes a numeric format. Other elements ignore the format. An unknown element name writes nothing.

To add an element, implement `IFormatElement` with a public parameterless constructor, mark the class with `[LogElement("name")]`, and call `LogBuilder.Configuration(logConfiguration).RegisterAssembly(assembly)` before `Build()`; calling it after `Build()` throws `InvalidOperationException`. Formats are parsed on first use, so elements registered before `Build()` are recognized even in targets created earlier.

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

Color values use `System.ConsoleColor` names. By default, Debug and Info are white; Error and Fatal are red. A console target writes one line for each message and restores the previous console color afterwards.

### File

| C# property | XML option category | Effect |
| :--- | :--- | :--- |
| `LogFormatRenderer` | `format` attribute | Output format |
| `LogFileName` | `fileName` | Active file path |
| `ArchiveFileName` | `archiveFileName` | Archive path; `{#}` is replaced with the creation-time suffix: `yyyyMMdd.HH` for `Hour`, `yyyyMMdd` for `Day` and `Week`, `yyyyMM` for `Month` |
| `ArchiveRollingType` | `archiveRolling` | `Hour` (default), `Day`, `Week`, or `Month` |
| `MaxArchiveFile` | `maxArchiveFiles` | Maximum archive files; the oldest are deleted when rolling would exceed it; `0` means no limit |
| `KeepOpenFile` | `keepOpenFile` | Keep the stream open between writes when `true` |
| `AutoFlush` | `autoFlush` | Flush the stream after each write when `true` |

The file target writes UTF-8 lines. Rolling compares each event's timestamp with the period of the active file's creation time, so an event queued by `AsyncLogger` goes to the file for the period in which it was created. `Week` rolls after seven days. Without `ArchiveFileName`, the file is never rolled. A relative archive path is resolved from the current directory, and its directory is created when needed. `KeepOpenFile` and `AutoFlush` default to `false`. Writes and disposal are serialized with a lock, so several loggers can share one file target.

### Database

A database target uses an ADO.NET `IDbConnection` provider. Set the `DatabaseLogTarget` properties and call `AddDbParameter(name, format)` in C#, or use a `<target logTarget="database">` entry in XML:

```xml
<target id="databaseConfig" logTarget="database">
  <database dbConnection="connection string"
            dbProvider="Namespace.ConnectionType, AssemblyName"
            dbCommand="insert into log (message) values (@message)"
            keepConnectionOpen="true">
    <option category="parameter" id="@message" value="${message}"/>
  </database>
</target>
```

Add a matching `<target id="databaseConfig" minLogLevel="Info"/>` entry inside a `<logger>` to enable it. Replace the provider type and connection string with values for your ADO.NET provider.

| C# | XML | Effect |
| :--- | :--- | :--- |
| `DbConnection` | `dbConnection` | Connection string |
| `DbProvider` | `dbProvider` | Connection type; XML resolves an assembly-qualified type name and throws if it cannot be loaded |
| `CommandTextRenderer` | `dbCommand` | Command text, rendered for each event |
| `AddDbParameter(name, format)` | `<option category="parameter" id="..." value="..."/>` | Parameter name and rendered value |
| `KeepConnection` | `keepConnectionOpen` | Keep the connection open between writes when `true` |

Database exceptions are not caught. If the connection is not open at the next write, for example after a failed open or a broken connection, the target disposes it and opens a new one. A `DbProvider` that is not an `IDbConnection` with a public parameterless constructor throws `InvalidOperationException` on the first write.

## Custom targets

Implement `ILogTarget.Write(LogEvent)` and `ILogTarget.Dispose()`, then add the target with `LogConfiguration.AddLogTarget(...)` as in the C# example. XML configuration creates only the built-in targets. Each logger serializes its own target calls: `Logger` uses a lock, and `AsyncLogger` uses its dedicated consumer thread. The built-in targets lock internally, so one instance can be shared between loggers. A custom target that is shared must handle concurrent calls itself, and its `Dispose()` is called once by each logger that uses it, so it must be safe to call more than once.

## License

Released under the MIT License. See the package's `LICENSE` file.
