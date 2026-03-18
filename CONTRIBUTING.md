# Contributing to MonadicSharp

Thank you for your interest in contributing! Here's how to get started.

## Quick start

```bash
git clone https://github.com/Danny4897/MonadicSharp.git
cd MonadicSharp
dotnet restore
dotnet test
```

## What to contribute

- **Bug reports** — open an issue with a minimal repro
- **Feature requests** — open an issue describing the use case first
- **PRs** — for small fixes, open a PR directly; for larger changes, open an issue first

## Code style

This project follows MonadicSharp-first patterns — see [`.cursorrules`](.cursorrules) for the full coding standard. In short:

- All fallible methods return `Result<T>` — no `throw` for expected failures
- Use `Error.Validation()`, `Error.NotFound()` etc. — not raw strings
- Async methods chain with `.Bind()` — not `if (result.IsSuccess)`

## Running tests

```bash
dotnet test                    # all tests
dotnet test --filter "Unit"    # unit tests only
```

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
