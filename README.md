# MonadicSharp

[![NuGet Version](https://img.shields.io/nuget/v/MonadicSharp.svg)](https://www.nuget.org/packages/MonadicSharp/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MonadicSharp.svg)](https://www.nuget.org/packages/MonadicSharp/)
[![CI](https://github.com/Danny4897/MonadicSharp/actions/workflows/ci.yml/badge.svg)](https://github.com/Danny4897/MonadicSharp/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![dev.to article](https://img.shields.io/badge/dev.to-article-black.svg)](https://dev.to/daniele_frau/railway-oriented-programming-in-c-without-languageext-2lfp)

Railway-Oriented Programming for C#. Replace exception-driven control flow with composable, explicit error handling — without LanguageExt.

```bash
dotnet add package MonadicSharp
```

---

## The problem

```csharp
// What does this throw? When? You have to read the entire call chain.
public User CreateUser(string name, string email)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new ValidationException("Name is required");

    if (!email.Contains('@'))
        throw new ValidationException("Invalid email");

    var existing = _db.Users.FirstOrDefault(u => u.Email == email);
    if (existing != null)
        throw new ConflictException("Email already exists");

    return _db.Save(new User(name, email));
}
```

Every caller needs a `try/catch`. Errors are invisible in the signature.
Composing multiple operations that can fail is painful.

## The solution

```csharp
public Result<User> CreateUser(string name, string email)
{
    return ValidateName(name)
        .Bind(_ => ValidateEmail(email))
        .Bind(_ => CheckEmailNotTaken(email))
        .Map(_ => new User(name, email))
        .Bind(user => _db.Users.AddAsync(user));
}
```

The signature tells the truth. Errors propagate automatically. No exceptions, no hidden branches.

---

## Core types

### `Result<T>` — success or failure

```csharp
// Map — transform the value (next step cannot fail)
Result<string> formatted = Parse("42").Map(n => $"Value: {n}");

// Bind — chain fallible operations (next step CAN fail)
Result<User> GetActiveUser(int id) =>
    FindUser(id)
        .Bind(ValidateActive)
        .Bind(LoadPermissions);

// Match — unwrap at the boundary (controller / endpoint only)
return result.Match(
    onSuccess: user  => Ok(user),
    onFailure: error => error.Type switch
    {
        ErrorType.NotFound   => NotFound(error.Message),
        ErrorType.Validation => BadRequest(error.Message),
        ErrorType.Conflict   => Conflict(error.Message),
        _                    => Problem(error.Message)
    });
```

### `Option<T>` — value or nothing

```csharp
Option<User> FindUser(int id) =>
    _db.TryGetValue(id, out var user)
        ? Option<User>.Some(user)
        : Option<User>.None;

string name = FindUser(id)
    .Map(u => u.Name)
    .GetValueOrDefault("Anonymous");
```

### Structured `Error` — semantic, HTTP-mappable

```csharp
// Each constructor maps directly to an HTTP status code
Error.Validation("Email is invalid", field: "email")   // → 400
Error.NotFound("Order", identifier: orderId.ToString()) // → 404
Error.Conflict("Username already taken")                // → 409
Error.Forbidden("Requires admin role")                  // → 403

// Enrich with context at any point in the pipeline
Error.Create("Payment gateway timeout")
    .WithMetadata("gatewayId", gateway.Id)
    .WithInnerError(originalException);
```

### Collect ALL validation errors with `Sequence`

```csharp
// Other libraries stop at the first error — this collects them all
var result = new[]
{
    ValidateName(request.Name),
    ValidateEmail(request.Email),
    ValidateAge(request.Age)
}.Sequence();
```

### Async-first pipelines

```csharp
public async Task<Result<OrderDto>> PlaceOrderAsync(PlaceOrderRequest request)
{
    return await ValidateRequest(request)
        .Bind(req   => _inventory.ReserveAsync(req))
        .Bind(inv   => _payment.ChargeAsync(inv))
        .Bind(pay   => _orders.SaveAsync(pay))
        .Map(order  => new OrderDto(order));
}
```

---

## Works with your AI Code Assistant

Add one file to your project and Copilot, Cursor, or Claude will generate MonadicSharp-first code automatically:

```bash
# Cursor
curl -o .cursorrules \
  https://raw.githubusercontent.com/Danny4897/MonadicSharp/main/.cursorrules

# GitHub Copilot
curl -o .github/copilot-instructions.md \
  https://raw.githubusercontent.com/Danny4897/MonadicSharp/main/.github/copilot-instructions.md
```

→ Full setup guide: [AI-ASSISTANT-SETUP.md](AI-ASSISTANT-SETUP.md)

---

## Ecosystem

| Package | Description | Install |
|---------|-------------|---------|
| [`MonadicSharp`](https://www.nuget.org/packages/MonadicSharp/) | Core — `Result<T>`, `Option<T>`, `Error`, async pipelines | `dotnet add package MonadicSharp` |
| [`MonadicSharp.AI`](https://www.nuget.org/packages/MonadicSharp.AI/) | Typed LLM errors, retry with backoff, agent tracing | `dotnet add package MonadicSharp.AI` |
| [`MonadicSharp.Recovery`](https://www.nuget.org/packages/MonadicSharp.Recovery/) | Self-healing pipelines — Amber track | `dotnet add package MonadicSharp.Recovery` |
| [`MonadicSharp.Agents`](https://www.nuget.org/packages/MonadicSharp.Agents/) | Multi-agent orchestration, circuit breaker | `dotnet add package MonadicSharp.Agents` |
| [`MonadicSharp.Http`](https://www.nuget.org/packages/MonadicSharp.Http/) | Result-aware HTTP client with typed retry | `dotnet add package MonadicSharp.Http` |
| [`MonadicSharp.Persistence`](https://www.nuget.org/packages/MonadicSharp.Persistence/) | Result-aware repository + UoW (EF Core 8) | `dotnet add package MonadicSharp.Persistence` |
| [`MonadicSharp.Security`](https://www.nuget.org/packages/MonadicSharp.Security/) | Prompt injection detection, secret masking | `dotnet add package MonadicSharp.Security` |
| [`MonadicSharp.Telemetry`](https://www.nuget.org/packages/MonadicSharp.Telemetry/) | OpenTelemetry tracing for agent pipelines | `dotnet add package MonadicSharp.Telemetry` |
| [`MonadicSharp.Azure.*`](https://github.com/Danny4897/MonadicSharp.Azure) | CosmosDB, Service Bus, Blob, Key Vault, OpenAI | `dotnet add package MonadicSharp.Azure.Core` |
| [`MonadicSharp.Framework`](https://www.nuget.org/packages/MonadicSharp.Framework/) | Everything in one package | `dotnet add package MonadicSharp.Framework` |

### Scaffold a full project

```bash
dotnet new install MonadicSharp.Templates
dotnet new monadic-api    # REST API with ROP wired end to end
dotnet new monadic-clean  # Clean Architecture + CQRS starter
```

---

## AI agent pipelines (MonadicSharp.AI)

LLM APIs fail in predictable, typed ways. `MonadicSharp.AI` gives you typed errors and automatic retry:

```csharp
// Typed errors — never catch HttpRequestException
AiError.RateLimit()           // 429 → retriable, backoff
AiError.ModelTimeout()        // timeout → retriable
AiError.InvalidOutput()       // bad JSON → self-healable 🔧
AiError.TokenLimitExceeded()  // context full → terminal ✋

// Retry with exponential backoff in one line
var result = await Result.TryAsync(() => _llm.CompleteAsync(prompt))
    .WithRetry(maxAttempts: 3, initialDelay: TimeSpan.FromSeconds(2));

// Multi-step agent with execution tracing
var agentResult = await AgentResult
    .StartTrace("SummarizeAgent", userInput)
    .Step("Retrieve",  q   => _search.FindChunksAsync(q))
    .Step("Generate",  ctx => _llm.CompleteAsync(ctx))
    .Step("Validate",  out => ParseAndValidate(out))
    .ExecuteAsync();
```

---

## Why not LanguageExt?

LanguageExt is excellent if you want the full Haskell-in-C# experience.
MonadicSharp is for teams that want Railway-Oriented Programming **without abandoning .NET idioms**:

| | MonadicSharp | LanguageExt |
|---|---|---|
| Dependencies | **Zero** | Heavy (Reactive, etc.) |
| Adoption | Incremental — one method at a time | Often requires full rewrite |
| AI/LLM support | First-class (`MonadicSharp.AI`) | General purpose |
| Azure integration | 7 focused packages | None |
| dotnet templates | `dotnet new monadic-api` | None |
| AI Code Assistant | `.cursorrules` + Copilot instructions | None |

---

## Requirements

- .NET 8.0+
- C# 10.0+

## Contributing

Issues and pull requests are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) and [CHANGELOG.md](CHANGELOG.md).

## License

MIT — see [LICENSE](LICENSE).

---

<p align="center">
  Built by <a href="https://github.com/Danny4897">Danny4897</a> ·
  Published on NuGet as <a href="https://www.nuget.org/profiles/Klexir">Klexir</a>
</p>
