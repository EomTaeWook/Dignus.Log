# Dignus 로깅 벤치마크

[English](Usage.md) | 한국어

[한국어 개요](../README.ko.md) | [로깅 API](../publish/Dignus.Log.ko.md) | [Microsoft 로깅 연동](../publish/Dignus.Extensions.Logging.ko.md)

## 사용법

기본 로거를 설정한 뒤 ILogger로 출력한다. Write와 수준별 확장은 생략한 호출 파일·행 정보를 자동 수집한다.

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

```powershell
dotnet build Dignus.Log.Benchmarks.sln -c Release -p:GeneratePackageOnBuild=false
```

벤치마크 프로젝트는 라이브러리의 공개 API를 참조한다. 솔루션은 [Dignus.Log.Benchmarks.sln](../Dignus.Log.Benchmarks.sln)이다.

## 측정 환경과 해석

원문 측정일은 2026-10-26이다. .NET 10.0.12, Release, workstation GC, 계층형 컴파일 비활성화 조건이다. CPU는 i9-14900K(24코어·32논리 프로세서), RAM 64GB(16GB×4, 4200MT/s), 파일용 Samsung 990 PRO 1TB NVMe, Windows 11 Pro 10.0.26200이다. 스레드 affinity는 지정하지 않았다.

의존성은 Dignus 1.4.3 로컬 패키지, NLog 6.2.1, Serilog 4.4.0, Serilog.Sinks.File 7.0.0, Serilog.Sinks.Async 2.1.0이다. 생산 스레드 1·4·8개가 로거 하나를 공유하며 지표는 독립 9회 실행의 중앙값이다.

Logger는 로거 잠금 안에서 동기 출력하고 File 대상도 자체 잠금을 사용한다. AsyncLogger는 무제한 MPSC 큐와 전용 소비자 스레드다. NLog 비동기는 AsyncTargetWrapper와 OverflowAction.Grow를 사용하고 나머지는 기본값이다. Serilog 비동기는 bufferSize=int.MaxValue, blockWhenFull=false로 크지만 유한한 큐다. 기존 측정은 한도에 닿거나 이벤트가 유실되지 않았다.

Dignus는 이벤트마다 DateTime.Now를 읽고 NLog는 기본 FastLocalTimeSource 캐시를 사용한다. 이 차이의 시간 기여도는 분리 측정하지 않았다. [NLog 시각 원본 설명](https://github.com/NLog/NLog/wiki/Time-Source)은 영어 외부 문서다.

실행마다 별도 폴더를 사용하고 워밍업 출력 완료 후 새 로거로 측정했다. 총 시간·할당은 최종 Flush·Dispose를 포함한다. GC 횟수·중단 시간은 같은 구간의 차이이며 측정 전 강제 GC는 제외한다. GC.GetTotalPauseDuration과 GC.CollectionCount(0/1/2)를 사용한다. 세대별 횟수는 겹치므로 합산하지 않는다. 개별 호출과 GC 이벤트를 연결하지 않았으므로 최대 지연을 GC만의 결과로 해석할 수 없다.

## 메시지만 기록하는 버퍼 파일 출력

실행당 400,000건, CRLF 포함 UTF-8 64바이트 줄이다. 이벤트 생성·렌더링·인코딩·파일 출력이 포함되어 순수 디스크 쓰기 측정이 아니다. 파일을 열어 두고 매번 Flush하지 않으며 최종 Flush·Dispose는 포함한다. 호출 방식·호출별 타이머·출력 확인은 다음 형식 포함 측정과 같다. [측정 코드](../Benchmarks/LoggerFileComparison/Program.cs)의 인자는 buffered다.

**로그당 총 시간(ns)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 213.81 | 162.73 | 270.88 | 199.62 | 337.46 | 367.97 |
| 4 | 330.80 | 180.56 | 453.39 | 225.92 | 407.81 | 639.63 |
| 8 | 360.13 | 197.14 | 422.76 | 245.91 | 414.19 | 652.41 |


**로그당 할당(B/log)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 80.02 | 85.68 | 120.03 | 166.56 | 360.04 | 360.72 |
| 4 | 80.02 | 91.33 | 120.03 | 173.45 | 360.04 | 381.02 |
| 8 | 80.02 | 92.60 | 120.03 | 172.16 | 360.04 | 381.02 |


**실행당 GC 중단 시간(ms)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.20 | 3.86 | 0.31 | 9.83 | 0.71 | 2.28 |
| 4 | 0.25 | 6.73 | 0.40 | 12.04 | 0.95 | 89.84 |
| 8 | 0.24 | 9.87 | 0.40 | 13.05 | 1.07 | 90.53 |


**GC 횟수(gen0 / gen1 / gen2)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 1 / 0 / 0 | 1 / 0 / 0 | 2 / 0 / 0 | 2 / 1 / 0 | 7 / 0 / 0 | 7 / 0 / 0 |
| 4 | 1 / 0 / 0 | 2 / 1 / 0 | 2 / 0 / 0 | 2 / 1 / 0 | 7 / 0 / 0 | 8 / 7 / 1 |
| 8 | 1 / 0 / 0 | 2 / 1 / 0 | 2 / 0 / 0 | 2 / 1 / 0 | 7 / 0 / 0 | 8 / 7 / 1 |


**로그 호출 p99(µs)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 3.40 | 0.20 | 3.50 | 2.00 | 3.60 | 0.90 |
| 4 | 9.10 | 0.50 | 26.90 | 3.90 | 18.40 | 6.10 |
| 8 | 18.30 | 1.50 | 12.50 | 5.60 | 38.60 | 9.40 |


**로그 호출 최댓값(ms, 실행별 최댓값의 중앙값)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.24 | 3.88 | 1.66 | 5.93 | 2.36 | 0.61 |
| 4 | 0.64 | 6.12 | 2.03 | 7.55 | 2.41 | 23.65 |
| 8 | 1.32 | 6.29 | 2.06 | 8.07 | 2.52 | 23.99 |


## 형식을 포함한 버퍼 파일 출력

실행당 400,000건, yyyy-MM-dd HH:mm:ss.fff | level | message 형식이며 CRLF 포함 UTF-8 97바이트다. 메시지는 사전 준비한다. Dignus는 LogEvent를 만들어 ILogger.Write를 호출하고 NLog는 문자열, Serilog는 {LogMessage:l}과 인자를 받으므로 호출·할당 형태가 다르다. Dignus·NLog 수준 표시는 Info, Serilog는 info다.

파일을 유지하고 매번 Flush하지 않는다. 기존 측정에서는 행 수·시각·수준·메시지·생산자별 순서를 확인했다. Stopwatch로 호출별 시간을 재므로 계측 비용이 포함된다. [측정 코드](../Benchmarks/LoggerFileComparison/Program.cs)의 인자는 formatted-buffered다.

**로그당 총 시간(ns)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 406.44 | 380.00 | 494.77 | 428.49 | 826.07 | 942.46 |
| 4 | 583.89 | 415.29 | 711.97 | 446.74 | 910.75 | 1,017.01 |
| 8 | 630.09 | 405.91 | 737.65 | 469.77 | 1,000.33 | 1,058.31 |


**로그당 할당(B/log)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 152.02 | 163.76 | 192.03 | 248.96 | 360.04 | 381.02 |
| 4 | 152.02 | 165.50 | 192.03 | 251.53 | 360.04 | 381.02 |
| 8 | 152.02 | 166.36 | 192.03 | 250.40 | 360.04 | 381.04 |


**실행당 GC 중단 시간(ms)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.35 | 19.42 | 0.51 | 32.39 | 0.73 | 80.67 |
| 4 | 0.44 | 21.59 | 0.62 | 37.90 | 0.92 | 88.06 |
| 8 | 0.45 | 19.69 | 0.68 | 36.91 | 1.18 | 88.94 |


**GC 횟수(gen0 / gen1 / gen2)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 3 / 0 / 0 | 3 / 2 / 0 | 4 / 0 / 0 | 4 / 3 / 0 | 7 / 0 / 0 | 8 / 7 / 1 |
| 4 | 3 / 0 / 0 | 3 / 2 / 0 | 4 / 0 / 0 | 4 / 3 / 0 | 7 / 0 / 0 | 8 / 7 / 1 |
| 8 | 3 / 0 / 0 | 3 / 2 / 0 | 4 / 0 / 0 | 4 / 3 / 0 | 7 / 0 / 0 | 8 / 7 / 1 |


**로그 호출 p99(µs)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 3.70 | 0.20 | 3.80 | 3.10 | 4.40 | 4.30 |
| 4 | 60.00 | 0.60 | 52.80 | 3.90 | 63.90 | 5.80 |
| 8 | 164.30 | 1.20 | 230.00 | 5.70 | 247.00 | 8.30 |


**로그 호출 최댓값(ms, 실행별 최댓값의 중앙값)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog sync | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.31 | 9.99 | 1.63 | 13.17 | 2.34 | 20.88 |
| 4 | 0.77 | 6.32 | 1.74 | 6.11 | 2.35 | 22.85 |
| 8 | 1.14 | 4.24 | 2.03 | 7.00 | 2.75 | 24.03 |


## I/O 없는 측정

실행당 1,048,576회이며 기존 메시지를 사용하고 렌더링·실제 출력은 하지 않는다. Dignus 대상은 잠금 없는 no-op, NLog 기본은 표준 대상 동기화, concurrent는 측정 코드의 재정의다. Serilog sink는 콜백을 잠금으로 보호한다. 결과는 이 구성들의 비교이며 라이브러리 전체의 보편적 순위가 아니다. [측정 코드](../Benchmarks/LoggerLibraryComparison/Program.cs)를 참조한다.

**로그당 총 시간(ns)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog default | NLog concurrent | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 49.60 | 140.00 | 44.51 | 42.56 | 133.95 | 85.63 | 325.75 |
| 4 | 67.66 | 78.69 | 52.27 | 60.15 | 121.53 | 51.36 | 354.06 |
| 8 | 137.24 | 65.69 | 121.54 | 121.60 | 170.58 | 126.15 | 389.64 |


**로그당 할당(B/log)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog default | NLog concurrent | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 56.00 | 56.14 | 120.00 | 120.00 | 136.84 | 160.00 | 160.13 |
| 4 | 56.00 | 56.21 | 120.00 | 120.00 | 126.84 | 160.00 | 176.02 |
| 8 | 56.00 | 56.77 | 120.00 | 120.00 | 127.42 | 160.00 | 192.04 |


**실행당 GC 중단 시간(ms)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog default | NLog concurrent | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.21 | 0.33 | 0.49 | 0.42 | 35.09 | 0.68 | 0.96 |
| 4 | 0.29 | 0.39 | 0.56 | 0.67 | 22.30 | 0.85 | 81.88 |
| 8 | 0.35 | 0.99 | 0.85 | 0.83 | 26.65 | 1.13 | 76.84 |


**GC 횟수(gen0 / gen1 / gen2)**

| Producers | Dignus Logger | Dignus AsyncLogger | NLog default | NLog concurrent | NLog async | Serilog sync | Serilog async |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 3 / 0 / 0 | 3 / 0 / 0 | 6 / 0 / 0 | 6 / 0 / 0 | 8 / 7 / 2 | 8 / 0 / 0 | 8 / 0 / 0 |
| 4 | 3 / 0 / 0 | 3 / 0 / 0 | 6 / 0 / 0 | 6 / 0 / 0 | 7 / 6 / 1 | 8 / 0 / 0 | 11 / 10 / 4 |
| 8 | 3 / 0 / 0 | 3 / 1 / 0 | 6 / 0 / 0 | 6 / 0 / 0 | 7 / 6 / 1 | 8 / 0 / 0 | 10 / 9 / 3 |


동기 구성의 실행별 범위(ns/log): 생산자 1개에서 Dignus 48.68–58.59, NLog 기본 42.74–45.95, Serilog 84.97–91.75; 4개에서 각각 34.46–102.81, 37.33–115.12, 35.15–114.73; 8개에서 112.82–167.85, 90.14–163.34, 105.27–173.33이다. 범위가 겹치면 9회 측정만으로 안정적 순위를 결론낼 수 없다. I/O 없는 측정은 위 파일 출력과 별개인 파이프라인 비용 보조 측정이다.

## 실행 인자

두 프로그램은 dignus, dignus-async, nlog, nlog-async, serilog, serilog-async를 받는다. I/O 없는 프로그램은 nlog-concurrent도 받는다. verify 모드는 전달·순서·직렬화된 대상 접근을 확인한다. 파일 프로그램의 buffered는 메시지만, formatted-buffered는 시각·수준도 기록한다. 구성별 폴더를 분리한다. Serilog의 회전 파일은 output-yyyyMMdd.log이므로 해당 날짜 파일을 경로 인자로 사용한다.
