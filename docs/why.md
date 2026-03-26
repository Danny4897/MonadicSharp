# Why MonadicSharp?

## The problem with exceptions

Exceptions were designed for truly exceptional situations — hardware failures, out-of-memory conditions, bugs. In modern applications they are used for routine control flow: validation errors, not-found lookups, auth failures. This causes three concrete problems.

**1. Invisible contracts.** A method that throws `ValidationException` has the same signature as one that never throws. Callers have no way to know what can go wrong without reading the implementation.

**2. Silent propagation.** An unhandled exception unwinds the call stack silently. Middleware catches it at the top, logs it, and returns a generic 500. The developer adds logging, adds a try/catch, ships a fix — and the cycle repeats.

**3. Impossible composition.** You cannot chain two methods that both throw. Each one needs its own try/catch. The code for error handling often exceeds the code for the happy path.

## Railway-Oriented Programming

The solution — introduced by Scott Wlaschin — is to make failure a first-class value in the type system.

```
Input → Step 1 ──success──→ Step 2 ──success──→ Step 3 → Output
                   │                   │
                failure              failure
                   └──────────────────┴──→ Error track (bypasses remaining steps)
```

- A `Result<T>` is either `Success(value)` or `Failure(error)`.
- `Bind` connects two steps. If the first is a failure, the second is never called.
- Errors propagate automatically — no `try/catch` anywhere in the pipeline.
- `Match` at the boundary forces the caller to handle both outcomes.

## Before and after

```csharp
// Exception-driven
public User CreateUser(string name, string email)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new ValidationException("Name required");        // invisible

    if (!email.Contains('@'))
        throw new ValidationException("Invalid email");        // invisible

    if (_db.Users.Any(u => u.Email == email))
        throw new ConflictException("Email taken");            // invisible

    return _db.Save(new User(name, email));                    // can also throw
}
// Every caller must wrap in try/catch and remember to handle each case.
```

```csharp
// Railway-Oriented
public Result<User> CreateUser(string name, string email) =>
    ValidateName(name)                     // Result<string>
        .Bind(_ => ValidateEmail(email))   // Result<string>
        .Bind(_ => CheckEmailNotTaken(email)) // Result<Unit>
        .Map(_ => new User(name, email))   // Result<User>
        .Bind(user => _db.AddAsync(user)); // Task<Result<User>>

// The signature is honest: it can succeed or fail.
// No try/catch anywhere. Errors propagate automatically.
// The controller handles outcomes once:
return result.Match(
    onSuccess: user  => Ok(user),
    onFailure: error => error.Type switch {
        ErrorType.Validation => BadRequest(error.Message),
        ErrorType.Conflict   => Conflict(error.Message),
        _                    => Problem(error.Message)
    });
```

## Why not LanguageExt or ErrorOr?

MonadicSharp is designed for teams that want:

- **Zero dependencies.** One package. No transitive deps, no version conflicts.
- **Incremental adoption.** Start with `Result<T>` in one service. The rest of the codebase stays unchanged. Implicit conversions mean `return user;` is still valid inside a `Result<User>` method.
- **Structured errors.** `Error` is not a string. It has a `Code`, a `Type` that maps to HTTP status codes, composable `SubErrors`, and `Metadata`. No mapping layer at the boundary.
- **AI-ready.** `.cursorrules` and `copilot-instructions.md` ship in the package. Copilot, Cursor, and Claude generate MonadicSharp-first code from day one — no prompt engineering required.
- **Async-native.** `Bind` and `Map` work transparently on `Task<Result<T>>`. No `.GetAwaiter().GetResult()` antipatterns.

## What MonadicSharp is not

- **Not a full functional programming library.** For discriminated unions, immutable collections, and category-theory abstractions, use LanguageExt.
- **Not a validation framework.** MonadicSharp handles errors; for complex validation rules, pair it with FluentValidation.
- **Not a framework.** It is a library — no DI requirements, no base classes, no magic. Add it to any existing project.

## Who uses it

MonadicSharp is used in:
- ASP.NET Core APIs where controllers map `Result<T>` to HTTP responses
- CQRS command/query handlers that return `Result<T>` instead of throwing
- Background workers and Service Bus consumers that need partial-failure handling via `Partition`
- AI agent pipelines where typed errors drive retry and recovery logic
