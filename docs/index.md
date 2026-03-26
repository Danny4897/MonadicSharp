---
layout: home

hero:
  name: "MonadicSharp"
  text: "Railway-Oriented Programming"
  tagline: "Replace exception-driven control flow with composable, explicit error handling — zero dependencies, pure .NET."
  image:
    src: /logo.svg
    alt: MonadicSharp
  actions:
    - theme: brand
      text: Get Started →
      link: /getting-started
    - theme: alt
      text: Core Types
      link: /core/result
    - theme: alt
      text: GitHub
      link: https://github.com/Danny4897/MonadicSharp

features:
  - icon: 🛤️
    title: Result<T>
    details: Success or failure, never both. Chain operations with Bind, transform values with Map, unwrap at the boundary with Match. Errors propagate automatically — no boilerplate.
    link: /core/result
    linkText: Learn more
  - icon: ❓
    title: Option<T>
    details: Value or nothing — NullReferenceException gone from the type system. Explicit absence encoded in the type. Map, Bind, GetValueOrDefault.
    link: /core/option
    linkText: Learn more
  - icon: 🔴
    title: Structured Errors
    details: Semantic, HTTP-mappable errors. Error.NotFound → 404. Error.Validation → 400. Compose sub-errors, attach metadata, chain inner errors.
    link: /core/error
    linkText: Learn more
  - icon: ⚡
    title: Async-first Pipelines
    details: Full Task<Result<T>> support. Bind and Map work transparently with async operations. PipelineBuilder for fluent, readable chains.
    link: /pipelines
    linkText: Learn more
  - icon: 📦
    title: Zero Dependencies
    details: One package, no transitive dependency hell. No Reactive, no LanguageExt. Adopt incrementally — one method at a time, in any existing codebase.
  - icon: 🤖
    title: AI Code Assistants
    details: Ship .cursorrules and copilot-instructions.md. Copilot, Cursor, and Claude generate MonadicSharp-first code automatically from day one.
    link: /templates
    linkText: Setup guide
---

<div class="vp-doc" style="max-width:960px;margin:0 auto;padding:4rem 1.5rem">

## Before vs After

The same operation — one with exceptions, one with Railway-Oriented Programming:

<div class="home-comparison">
<div>

```csharp
// Exception-driven: contract invisible to callers
public User CreateUser(string name, string email)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new ValidationException("Name required");

    if (!email.Contains('@'))
        throw new ValidationException("Invalid email");

    var existing = _db.Users
        .FirstOrDefault(u => u.Email == email);
    if (existing != null)
        throw new ConflictException("Email taken");

    return _db.Save(new User(name, email));
}
// Every caller needs try/catch.
// Errors are invisible in the signature.
```

</div>
<div>

```csharp
// Railway-Oriented: contract explicit in the return type
public Result<User> CreateUser(string name, string email) =>
    ValidateName(name)
        .Bind(_ => ValidateEmail(email))
        .Bind(_ => CheckEmailNotTaken(email))
        .Map(_ => new User(name, email))
        .Bind(user => _db.Users.AddAsync(user));

// The signature tells the truth.
// Errors propagate automatically — no try/catch.
// Compose at the controller with Match:
return result.Match(
    onSuccess: user  => Ok(user),
    onFailure: error => error.Type switch {
        ErrorType.Validation => BadRequest(error.Message),
        ErrorType.Conflict   => Conflict(error.Message),
        _                    => Problem(error.Message)
    });
```

</div>
</div>

## Ecosystem

MonadicSharp is a family of focused packages — use only what you need.

| Package | Description | Install |
|---------|-------------|---------|
| [`MonadicSharp`](https://www.nuget.org/packages/MonadicSharp/) | Core — `Result<T>`, `Option<T>`, `Error`, pipelines | `dotnet add package MonadicSharp` |
| [`MonadicSharp.AI`](https://www.nuget.org/packages/MonadicSharp.AI/) | Typed LLM errors, retry with backoff, agent tracing | `dotnet add package MonadicSharp.AI` |
| [`MonadicSharp.Recovery`](https://www.nuget.org/packages/MonadicSharp.Recovery/) | Self-healing pipelines — Amber track | `dotnet add package MonadicSharp.Recovery` |
| [`MonadicSharp.Agents`](https://www.nuget.org/packages/MonadicSharp.Agents/) | Multi-agent orchestration, circuit breaker | `dotnet add package MonadicSharp.Agents` |
| [`MonadicSharp.Http`](https://www.nuget.org/packages/MonadicSharp.Http/) | Result-aware HTTP client with typed retry | `dotnet add package MonadicSharp.Http` |
| [`MonadicSharp.Persistence`](https://www.nuget.org/packages/MonadicSharp.Persistence/) | Result-aware repository + UoW (EF Core 8) | `dotnet add package MonadicSharp.Persistence` |
| [`MonadicSharp.Security`](https://www.nuget.org/packages/MonadicSharp.Security/) | Prompt injection detection, secret masking | `dotnet add package MonadicSharp.Security` |
| [`MonadicSharp.Telemetry`](https://www.nuget.org/packages/MonadicSharp.Telemetry/) | OpenTelemetry tracing for agent pipelines | `dotnet add package MonadicSharp.Telemetry` |
| [`MonadicSharp.Framework`](https://www.nuget.org/packages/MonadicSharp.Framework/) | Everything in one meta-package | `dotnet add package MonadicSharp.Framework` |

→ [Full ecosystem docs](/ecosystem/)

</div>
