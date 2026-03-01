# Changelog

All notable changes to MonadicSharp are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versioning follows [Semantic Versioning](https://semver.org/).

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
