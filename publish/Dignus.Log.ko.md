# Dignus.Log

[English](Dignus.Log.md) | 한국어

[한국어 개요](../README.ko.md)

콘솔·파일·데이터베이스로 로그를 출력한다. C# 또는 XML로 대상을 구성한 뒤 `LogHelper` 또는 `ILogger`로 기록한다.

## 설치

```powershell
dotnet add package Dignus.Log
```

## C# 설정과 메시지 기록

아래 예제는 기본 비동기 로거의 Info 이상 메시지를 파일에 기록한다. 빈 로거 이름은 기본 로거를 뜻하며 `Build()`가 설정된 대상을 연결해 생성한다.

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
logger.Info("Application started");
```

```csharp
logger.Debug("Diagnostic details");
logger.Info("Application started");
logger.Error("Operation failed");
logger.Fatal("Application cannot continue");
```

`Write(level, message)`와 `Debug`·`Info`·`Error`·`Fatal` 확장은 생략한 호출 파일·행을 자동 수집한다. 명시적으로 전달하면 덮어쓰며, 필요 없으면 빈 문자열과 0을 전달한다. 위 설정에서는 Debug가 제외된다.

## 동기·비동기 로거

로거 이름마다 출력 방식이 하나다. `AddLogTarget(name, minLogLevel, target)`으로 이름을 처음 추가하면 Async가 기본이며, 같은 이름의 후속 호출은 대상을 추가한다. 대상마다 지정한 최소 수준 이상을 출력한다.

방식을 선택하려면 대상 추가 전에 `AddLogger(name, type)`를 호출한다. 이미 존재하는 이름이면 `InvalidOperationException`이 발생하며 대상은 `LogBuilder.Configuration(...)` 전에 구성한다.

```csharp
logConfiguration.AddLogger("transaction", LoggerType.Sync)
    .AddLogTarget(LogLevel.Info, transactionFileTarget)
    .AddLogTarget(LogLevel.Error, consoleLogTarget);
```

```csharp
var logConfiguration = new LogConfiguration();
logConfiguration.AddLogger(string.Empty, LoggerType.Sync).AddLogTarget(LogLevel.Info, fileLogTarget);
LogBuilder.Configuration(logConfiguration).Build();
```

```csharp
ILogger auditLogger = LogManager.GetLogger("audit");
```

```csharp
LogHelper.Info("Application started with caller information");
```

```csharp
LogManager.Dispose();
```

### AsyncLogger

무제한 MPSC 큐와 전용 백그라운드 스레드를 사용한다. 호출은 큐에 넣은 뒤 반환하며 실제 출력은 해당 스레드에서 수행한다. 대상의 쓰기 예외를 잡지 않으므로 백그라운드의 미처리 예외는 프로세스를 종료시킨다. `Dispose()`는 대기 로그를 비운 뒤 대상을 해제하지만 동시에 추가되는 로그는 유실될 수 있다. 입력이 계속 출력보다 빠르면 메모리가 증가한다.

### Logger

호출 스레드에서 로거 단위 잠금을 유지하며 출력한다. 큐나 작업 스레드가 없고 다른 호출자는 잠금을 기다린다. 대상 예외는 호출자에게 전파한다. 기본 로거도 대상 추가 전에 Sync로 등록하면 동기로 사용할 수 있다.

```csharp
var logConfiguration = new LogConfiguration();
logConfiguration.AddLogger(string.Empty, LoggerType.Sync).AddLogTarget(LogLevel.Info, fileLogTarget);
LogBuilder.Configuration(logConfiguration).Build();
```

두 방식 모두 대상 예외를 재시도하거나 숨기지 않는다.

### 이름별 조회와 사용자 로거

`GetLogger(name)`는 등록된 로거를 반환하거나 해당 이름의 설정으로 생성한다. null·빈 이름은 기본 로거다. 기본 이름은 진입 어셈블리 이름이며 `GetDefaultLogger()`로 가져온다. 등록·설정이 없는 이름이나 Build 이전 조회는 `InvalidOperationException`이다.

```csharp
ILogger auditLogger = LogManager.GetLogger("audit");
```

내장 Logger·AsyncLogger는 LogManager가 설정으로 생성한다. 사용자 `ILogger`는 `RegisterLogger(logger)`로 등록하며 중복 이름은 예외다.

### LogHelper

문자열과 예외용 정적 메서드를 제공한다. 문자열은 호출 위치를 수집하고 예외는 `Exception.ToString()`을 하나의 이벤트로 기록한다. 예외의 파일 위치는 파일 정보가 있는 첫 프레임이며 없으면 호출 위치를 사용한다. Build 후 기본 로거를 사용한다.

```csharp
LogHelper.Info("Application started with caller information");
```

`SetLogger(ILogger)`는 이후 모든 LogHelper 호출이 공유하는 로거를 바꾼다.

### 종료

메시지 생산을 먼저 멈추고 종료한다.

```csharp
LogManager.Dispose();
```

대기 중 비동기 출력 완료를 기다린 뒤 등록 로거의 대상을 해제한다. 각 로거는 대상별로 한 번 해제한다. 이후 조회는 `ObjectDisposedException`, 기존 로거의 추가 쓰기는 무시한다. 프로세스 종료 처리에도 등록된다. Build는 프로세스당 한 번이며 재호출은 예외다.

## XML 설정

대상 `id`와 로거 이름은 각각 고유해야 한다. `<logger>` 안에서 선언된 대상을 id로 연결하고 최소 수준을 지정한다. 미선언 대상 참조나 중복 선언은 예외다. XML은 console·file·database를 지원하며 참조하지 않는 대상은 생성하지 않는다. 미사용 DB 대상의 공급자 어셈블리도 로드하지 않는다.

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

최소 수준은 Debug·Info·Error·Fatal이다. 로거 이름 생략·빈 값은 기본 로거, loggerType 생략은 Async다.

## 출력 형식

기본 형식은 `${datetime} | ${level} | ${message}`이며 null·빈 형식에도 적용한다. 요소 밖의 문자열과 닫는 괄호가 없는 `${`는 그대로 출력한다.

| 요소 | 출력 |
| --- | --- |
| `${datetime}` | 이벤트 시각, 기본 yyyy-MM-dd HH:mm:ss.fff |
| `${level}` | Debug·Info·Error·Fatal |
| `${message}` | 메시지 |
| `${callsite}` | 호출 파일 경로와 행 |
| `${callerFileName}` | 호출 파일 이름 |
| `${callerFilePath}` | 호출 파일 경로 |
| `${callerLineNumber}` | 호출 행 번호 |

`${datetime:HH:mm:ss}`, `${callerLineNumber:D5}`처럼 지원 요소에 형식을 지정한다. 다른 요소는 형식을 무시하며 알 수 없는 요소는 출력하지 않는다. 사용자 요소는 공개 기본 생성자가 있는 `IFormatElement`에 `[LogElement("name")]`을 지정하고 Build 전에 `RegisterAssembly(assembly)`로 등록한다. 형식은 최초 사용 시 파싱한다.

## 대상별 설정

XML option category는 대소문자를 구분하지 않는다.

### 콘솔

`LogFormatRenderer`는 format 속성에 대응한다. `DebugColor`·`InfoColor`·`ErrorColor`·`FatalColor`는 color category와 해당 수준 id로 지정한다. 값은 ConsoleColor 이름이다. 기본 Debug·Info는 흰색, Error·Fatal은 빨간색이다. 메시지마다 한 줄을 출력하고 기존 색을 복원한다.

### 파일

| C# 속성 | XML category | 의미 |
| --- | --- | --- |
| LogFormatRenderer | format 속성 | 출력 형식 |
| LogFileName | fileName | 활성 파일 경로 |
| ArchiveFileName | archiveFileName | 보관 경로와 `{#}` 접미사 |
| ArchiveRollingType | archiveRolling | Hour(기본)·Day·Week·Month |
| MaxArchiveFile | maxArchiveFiles | 보관 개수, 0은 무제한 |
| KeepOpenFile | keepOpenFile | 쓰기 사이 스트림 유지 |
| AutoFlush | autoFlush | 쓰기마다 Flush |

UTF-8 줄 단위로 기록한다. 파일 생성 시각의 기간과 이벤트 시각을 비교해 회전하므로 비동기 대기 이벤트도 생성된 기간의 파일로 간다. Week는 7일 후 회전한다. 보관 경로가 없으면 회전하지 않는다. 접미사는 Hour에서 yyyyMMdd.HH, Day·Week에서 yyyyMMdd, Month에서 yyyyMM이다. 한도를 넘기면 가장 오래된 파일을 삭제한다. 상대 보관 경로는 작업 디렉터리 기준이며 필요한 폴더를 생성한다. KeepOpenFile·AutoFlush의 기본값은 false다. 쓰기·해제는 잠금으로 직렬화한다.

### 데이터베이스

ADO.NET `IDbConnection` 공급자를 사용한다. C#에서 속성과 `AddDbParameter`를 설정하거나 XML로 구성한다. logger 내부에도 해당 target 참조를 추가해야 활성화된다.

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

| C# | XML | 의미 |
| --- | --- | --- |
| DbConnection | dbConnection | 연결 문자열 |
| DbProvider | dbProvider | 어셈블리 한정 연결 타입 이름 |
| CommandTextRenderer | dbCommand | 이벤트마다 렌더링할 명령 |
| AddDbParameter(name, format) | parameter category | 매개변수 이름과 렌더링 값 |
| KeepConnection | keepConnectionOpen | 쓰기 사이 연결 유지 |

공급자·연결 문자열은 사용하는 DB에 맞춘다. 타입을 로드하지 못하면 예외다. DB 예외를 잡지 않으며 다음 쓰기에서 연결이 닫혀 있으면 기존 연결을 해제하고 다시 연다. 공개 기본 생성자를 가진 IDbConnection 타입이 아니면 첫 쓰기에 InvalidOperationException이다.

## 사용자 대상

`ILogTarget.Write(LogEvent)`와 Dispose를 구현하고 AddLogTarget으로 추가한다. XML은 내장 대상만 생성한다. 각 로거는 자신의 대상 호출을 직렬화하지만 로거 사이에 공유하는 사용자 대상은 동시 호출을 직접 처리해야 한다. 공유 대상은 로거마다 Dispose가 호출되므로 반복 해제에도 안전해야 한다.

## 라이선스

MIT 라이선스. [LICENSE](../LICENSE)를 참조한다.
