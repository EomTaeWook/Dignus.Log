# Dignus.Extensions.Logging

[English](Dignus.Extensions.Logging.md) | 한국어

[한국어 개요](../README.ko.md) | [로깅 API](Dignus.Log.ko.md)

Dignus.Log를 Microsoft.Extensions.Logging의 ILogger·ILoggerFactory와 연결한다.

## 기능과 API

- `AddDignusLog(IServiceCollection, LogConfiguration)`는 공급자를 등록하고 LogBuilder로 설정을 Build한다. 별도 Build는 호출하지 않는다.
- loggerName을 받는 오버로드는 기본 로거 대신 해당 이름의 로거로 전달한다.
- `DignusLoggerProvider`는 ILoggerProvider를 구현한다. 공급자 해제는 Dignus 로거를 해제하지 않으며 최종 종료는 LogManager.Dispose로 처리한다.
- `${category}`로 범주를 출력한다. BeginScope 값은 IExternalScopeProvider로 받지만 임의 scope 값은 기본 출력에 포함하지 않는다.

## 사용 예제

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

기본 오버로드는 진입 어셈블리 이름의 기본 로거를 사용한다. XML의 loggerName을 생략하거나 빈 값으로 두면 해당 로거에 대상을 연결한다.

## 종료와 형식

애플리케이션의 로그 생산을 멈춘 뒤 LogManager.Dispose를 호출한다. 형식 렌더링은 캐시된 델리게이트를 사용하며 ASP.NET Core의 표준 로깅 파이프라인과 연결한다. 파일·DB 대상과 상세 형식은 [로깅 API](Dignus.Log.ko.md)를 따른다.
