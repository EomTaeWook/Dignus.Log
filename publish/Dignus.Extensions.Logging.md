# Dignus.Extensions.Logging

**Integration module connecting `Dignus.Log` with `Microsoft.Extensions.Logging`.**  
Provides a lightweight logging bridge that enables seamless use of Dignus’ logger  
within the standard ASP.NET Core and .NET logging infrastructure.

---

## Features

| Feature | Description |
| :--- | :--- |
| **ASP.NET Core Integration** | Enables Dignus logging through the `ILogger` interface and `ILoggerFactory`. |
| **Category Support** | Adds log category support (`${category}`) for structured log output. |
| **Scope Handling** | Accepts `BeginScope()` values through `IExternalScopeProvider`; arbitrary scope values are not included in output by default. |
| **Automatic Registration** | `AddDignusLog()` extension method integrates logging and configuration automatically. |

---

## Core APIs

| API | Purpose |
| :--- | :--- |
| `AddDignusLog(this IServiceCollection services, LogConfiguration logConfiguration)` | Registers the Dignus logging provider and builds the log configuration with `LogBuilder`, so do not call `LogBuilder.Build()` separately. |
| `AddDignusLog(this IServiceCollection services, LogConfiguration logConfiguration, string loggerName)` | Same as above, but routes .NET log events to the Dignus logger with that name instead of the default logger. |
| `DignusLoggerProvider` | Implements `ILoggerProvider` to route .NET log events into the Dignus logger. Disposing the provider does not dispose that logger; `LogManager.Dispose()` does. |

---

## Example

```csharp
using Dignus.Extensions.Logging;
using Dignus.Log;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

// Load Dignus logging configuration
var logConfiguration = LogConfigXmlReader.Load("log.config");

// Integrate with Microsoft.Extensions.Logging
services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.Services.AddDignusLog(logConfiguration);
});

var serviceProvider = services.BuildServiceProvider();
var applicationLogger = serviceProvider.GetRequiredService<ILogger<Program>>();

applicationLogger.LogInformation("Hello from Dignus Logging!");
```

With the default overload, the provider writes through the default logger, which is named after the entry assembly. Leave `loggerName` empty or omit it on the XML `<logger>` element to attach targets to that logger.

## Design Principles

- **Category Awareness:** Extends `LogEvent` to include category metadata for structured logging.  
- **Optimized Formatting:** Uses cached delegates for element rendering to minimize overhead.
- **Full .NET Integration:** Works natively with ASP.NET Core’s logging pipeline.  
- **Scoped Logging Support:** Accepts `BeginScope()` values; arbitrary scope values are not rendered by default.

---
