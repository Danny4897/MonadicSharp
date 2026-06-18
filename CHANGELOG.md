# Changelog

All notable changes to MonadicSharp are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versioning follows [Semantic Versioning](https://semver.org/).

---

## [Unreleased] — v1.6.0

### Added — Core (`MonadicSharp`)
- Multi-target: `net8.0`, `net9.0`, `net10.0`
- `LangVersion=14` — C# 14 features enabled across all projects
- `ReadOnlySpan<Result<T>>` zero-allocation overloads for `Partition<T>` and `Sequence<T>` (available on `net10.0+` via `#if NET10_0_OR_GREATER`)
- Framework-conditional package references (System.Text.Json / EF Core versioned per TFM)

### Added — `MonadicSharp.Serialization` (v1.0.0) 🆕
- `ResultJsonConverterFactory` + `ResultJsonConverter<T>`: serializes `Result<T>` as `{"ok":true,"value":...}` / `{"ok":false,"error":"...","code":"...","type":"..."}`
- `OptionJsonConverterFactory` + `OptionJsonConverter<T>`: serializes `Option<T>` as `{"hasValue":true,"value":...}` / `{"hasValue":false}`
- `EitherJsonConverterFactory` + `EitherJsonConverter<TLeft,TRight>`: serializes `Either` as `{"isRight":bool,...}`
- `SerializationExtensions.AddMonadicSharp(JsonSerializerOptions)` — opt-in, no global state
- `SerializationExtensions.CreateDefaultOptions()` — camelCase + all converters
- `ToJson` / `FromJson` / `FromJsonOption` extension methods

### Added — `MonadicSharp.Interop` (v1.0.0) 🆕
- `HttpResponseMessageExtensions`: `ToResult()` and `ReadAsResult<T>()` on `HttpResponseMessage`
  - On `.NET 10+`: exposed as C# 14 extension block members directly on the type
  - On earlier targets: standard extension methods (same API, full backward compat)
- `HttpClientExtensions`: `GetAsResult<T>()` and `PostAsResult<TReq,TRes>()` wrapping network errors
- `HttpErrorMapper`: maps `HttpStatusCode` → typed `Error` (NotFound, Validation, Forbidden, Conflict)
- `ResultMinimalApiExtensions`: `ToMinimalApiResult<T>()`, `ToOkResult<T>()`, `ToCreatedResult<T>()` — bridges `Result<T>` to ASP.NET Core minimal API `IResult` with correct status codes

### Added — `MonadicSharp.Query` (v1.0.0) 🆕
- `PartitionExtensions`: `PartitionMap`, `SuccessValues`, `FailureErrors`, `GroupByOutcome`, `SuccessRate`, `RequireSuccessRate` (quality-gate for batch LLM results)
- `ReconcileExtensions`: `ReconcileBy`, `MergeBy`, `AggregateAll`, `BestOf` — multi-source batch reconciliation for LLM pipelines

### Added — `MonadicSharp.Unions` (v1.0.0-preview.1) 🆕
- `Union2<T1,T2>` and `Union3<T1,T2,T3>`: tagged union types with `Map/Bind/Match/IsCase*`
- Implicit conversions from component types to union
- Bridge extensions: `ToResult()`, `ToOption()`, `ToEither()`, LLM-union factory helpers (`LlmSuccess`, `LlmRateLimit`, `LlmModelError`)
- Designed to be replaced by C# 15 native `union` keyword — MonadicSharp extension blocks will then add Map/Bind/Match to native DUs

### Added — `MonadicSharp.Aspire` (v1.0.0) 🆕
- `MonadicCircuitBreakerHealthCheck`: sliding-window circuit breaker integrated with ASP.NET Core health checks and Aspire 13 dashboard
- `CircuitBreakerOptions`: configurable window size, sample minimum, failure threshold, degraded threshold
- `GreenScoreMeter`: OpenTelemetry meter for pipeline success/failure/retry counters and `monadicsharp.pipeline.duration` histogram
- `GreenScoreMeter.ComputeGreenScore()`: snapshot Green Score heuristic (0–100)
- `AspireResourceExtensions.AddMonadicSharpHealthChecks()`: one-liner DI registration
- `AspireResourceExtensions.AddMonadicSharpTelemetry()`: register meter only

### Changed
- CI: matrix strategy now tests on .NET 8, 9, and 10
- CI: now covers all four new test projects (Serialization, Interop, Query)

---

## [1.4.0] - 2026-02-23

### Added
- Comprehensive xUnit test suite covering all core types and extensions (`ResultTests`, `OptionTests`, `EitherTests`, `ErrorTests`, `TryTests`, `UnitTests`, `FunctionalExtensionsTests`, `PipelineExtensionsTests`)
- GitHub Actions CI workflow: build + test on every push and pull request
- GitHub Actions Publish workflow: automatic NuGet publish on version tag push (`v*.*.*`)
- `CHANGELOG.md`
- Usage examples in `Examples/MonadicSharpExamples.cs`

### Changed
- Bumped `PackageVersion` to `1.4.0`
- Removed unused `Microsoft.Extensions.Hosting` dependency

### Fixed
- `.gitignore` updated to correctly exclude `bin/`, `obj/`, `.idea/`, and other build artifacts

---

## [1.3.0] - 2025-10-08

### Changed
- Complete rebranding from FunctionalSharp to MonadicSharp
- Templates, documentation, and package naming fully unified under `MonadicSharp`

---

## [1.2.0] - 2025-08-01

### Changed
- Changed namespace from `FunctionalSharp` to `MonadicSharp` to match NuGet package name

---

## [1.1.0] - 2025-07-01

### Added
- `Either<TLeft, TRight>` type for representing two alternative values
- Implicit `Right` = success, `Left` = failure convention

---

## [1.0.0] - 2025-06-10

### Added
- `Result<T>` — Railway-Oriented Programming type with `Map`, `Bind`, `Match`, `Do`, `Where`
- `Option<T>` — Null-safe optional value with `Map`, `Bind`, `Match`, `Where`
- `Error` — Immutable, hierarchical error representation with `ErrorType` enum
- `Try` — Exception-to-Result wrapper with sync and async variants
- `Unit` — Functional void-type equivalent
- `ResultExtensions` — `Sequence`, `Traverse`, `Ensure`, `Combine`, `OrElse`, async variants
- `OptionExtensions` — `Filter`, `Sequence`, `Traverse`, `ToResult`, async variants
- `PipelineExtensions` — `PipelineAsync`, `ThenIf`, `ThenWithRetry`, `PipelineBuilder<T>`
- `DbSetExtensions` — EF Core integration returning `Result<T>` and `Option<T>` from DbSet operations
