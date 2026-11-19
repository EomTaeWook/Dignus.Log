# Dignus.Log

[English](README.md) | 한국어

동기·비동기 로거, 콘솔·파일 출력과 Microsoft 로깅 연동을 제공하는 라이브러리.

## 요구 사항

패키지는 netstandard2.1, net48, net481, net8.0, net9.0, net10.0을 대상으로 한다. 벤치마크 프로젝트는 .NET 10을 사용한다.

## 설치

```xml
<PackageReference Include="Dignus.Log" Version="3.0.0" />
```

## 빠른 시작

기본 로거를 설정한 뒤 조회하여 메시지를 기록한다.

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

## 사용법

파일 출력, 동기·비동기 로깅, 호출 위치와 종료 처리는 API 가이드를 따른다. Microsoft.Extensions.Logging 연동은 별도 가이드로 제공한다.

## 문서

- [로깅 API · 영어](publish/Dignus.Log.md)
- [Microsoft 로깅 연동 · 영어](publish/Dignus.Extensions.Logging.md)
- [벤치마크 방법과 결과 · 영어](docs/Usage.md)

## 라이선스

MIT 라이선스. [LICENSE](LICENSE)를 참조한다.
