# Railway-Oriented Programming in C# — without LanguageExt

If you've ever tried to bring functional error handling into a C# codebase, you've probably landed on LanguageExt. It's powerful. It's also 42 million downloads worth of a paradigm shift that turns your entire codebase inside out.

This article is about a lighter path: Railway-Oriented Programming with **MonadicSharp**, a zero-dependency library that fits into your existing .NET 8 code without asking you to rewrite everything.

---

## The railway metaphor

Scott Wlaschin's Railway-Oriented Programming describes a simple idea: every operation in your system runs on two tracks — a **success track** and a **failure track**. Once you leave the success track, you stay on the failure track. No re-entry.

In practice this means:

```
Input ──► Validate ──► Fetch from DB ──► Process ──► Save ──► Output
             │               │               │           │
             └── Error ──────┴───────────────┴───────────┘
                   (failure propagates automatically)
```

Every step either passes the value forward, or switches to the failure track. You only handle the failure once — at the end.

---

## What's wrong with exceptions for control flow?

Nothing, when they represent *unexpected* conditions. But in most .NET codebases, exceptions are used for *expected* failures: validation errors, not-found entities, business rule violations. This causes real problems:

```csharp
// What does this throw? When? Under what conditions?
// You have to read the implementation to find out.
public User CreateUser(CreateUserRequest request)
{
    ValidateRequest(request);       // throws ValidationException?
    CheckEmailUniqueness(request);  // throws ConflictException?
    return _repo.Save(MapToUser(request)); // throws DbException?
}
```

The method signature lies. Callers don't know what to expect. Test coverage for the failure paths requires exception interception. And composing multiple fallible operations means nested try/catch blocks.

---

## The Result<T> type

`Result<T>` makes failure explicit in the type system:

```csharp
public Result<User> CreateUser(CreateUserRequest request)
{
    return ValidateName(request.Name)
        .Bind(_ => ValidateEmail(request.Email))
        .Bind(_ => CheckEmailNotTaken(request.Email))
        .Map(_ => new User(request.Name, request.Email))
        .Bind(user => _repo.SaveAsync(user));
}
```

Now the signature is honest. The caller knows this can fail. There are no hidden branches. Each step only runs if the previous one succeeded.

Install it:

```bash
dotnet add package MonadicSharp
```

---

## Building blocks

### Map — transform the value

```csharp
Result<int> parsed = Parse("42");         // Success(42)
Result<string> formatted = parsed.Map(n => $"Value: {n}"); // Success("Value: 42")
```

### Bind — chain fallible operations

```csharp
Result<User> GetActiveUser(int id) =>
    FindUser(id)                           // Result<User>
        .Bind(ValidateActive)              // Result<User>
        .Bind(LoadPermissions);            // Result<User>
```

If `FindUser` fails, `ValidateActive` never runs. The error propagates.

### Match — handle both tracks

```csharp
var response = result.Match(
    onSuccess: user  => Ok(user),
    onFailure: error => error.Type switch
    {
        ErrorType.NotFound   => NotFound(error.Message),
        ErrorType.Validation => BadRequest(error.Message),
        ErrorType.Forbidden  => Forbid(),
        _                    => Problem(error.Message)
    }
);
```

---

## Structured errors

One of MonadicSharp's differentiators over simpler Result libraries is the `Error` type. It's not just a string — it carries semantic type, error code, metadata, sub-errors, and inner errors:

```csharp
// Semantic constructors
Error.Validation("Email is invalid", field: "email")
Error.NotFound("Order", identifier: orderId.ToString())
Error.Forbidden("Requires admin role")
Error.Conflict("Username already taken", resource: "username")

// Context enrichment
Error.Create("Payment gateway timeout")
    .WithMetadata("gatewayId", gateway.Id)
    .WithMetadata("attemptedAt", DateTime.UtcNow)
    .WithInnerError(originalException)
```

This means your API can map errors to HTTP status codes automatically — no string parsing, no custom exception hierarchy.

---

## Collecting all validation errors

Other libraries (like CSharpFunctionalExtensions) stop at the first error. With `Sequence`, you collect them all:

```csharp
var result = new[]
{
    ValidateName(request.Name),
    ValidateEmail(request.Email),
    ValidateAge(request.Age)
}.Sequence();

// result is either:
//   Success([name, email, age])
//   Failure(MULTIPLE_ERRORS with SubErrors containing each failure)
```

---

## Async pipelines with retry

This is where MonadicSharp goes beyond basic Result libraries. The `PipelineExtensions` let you compose async steps with conditional execution and built-in retry:

```csharp
var result = await GetOrder(orderId)
    .Then(ValidateInventory)
    .ThenIf(o => o.Total > 500, ApplyDiscount)
    .ThenWithRetry(
        operation:   CallPaymentGateway,
        maxAttempts: 3,
        delay:       TimeSpan.FromSeconds(1))
    .Then(SendConfirmation)
    .ExecuteAsync();
```

No Polly, no Refit, no external retry library needed for the common case. Three lines.

---

## Entity Framework integration

`DbSetExtensions` wraps EF Core operations to return `Result<T>` and `Option<T>`:

```csharp
// No null check needed — FindAsync returns Option<User>
var user = await _db.Users.FindAsync(id);

// Composable
var result = await _db.Users.FindAsync(userId)
    .ToResult(Error.NotFound("User", userId.ToString()))
    .BindAsync(user => UpdateUserFields(user, request))
    .BindAsync(updated => _db.Users.Update(updated))
    .BindAsync(_ => _db.SaveChangesAsync());
```

---

## Minimal API integration

In an ASP.NET Core Minimal API, the pattern becomes extremely clean:

```csharp
app.MapPost("/users", async (CreateUserRequest req, UserService svc) =>
    (await svc.CreateUser(req)).Match(
        onSuccess: user  => Results.Created($"/users/{user.Id}", user),
        onFailure: error => error.Type switch
        {
            ErrorType.Validation => Results.BadRequest(error),
            ErrorType.Conflict   => Results.Conflict(error),
            _                    => Results.Problem(error.Message)
        }
    ));
```

---

## When to use MonadicSharp vs alternatives

| | MonadicSharp | CSharpFunctionalExtensions | LanguageExt | ErrorOr |
|---|---|---|---|---|
| Learning curve | Low | Low | High | Low |
| Structured errors | Yes | Partial | Yes | Partial |
| Built-in retry | Yes | No | No | No |
| EF Core integration | Yes | No | No | No |
| Project templates | Yes | No | No | No |
| Full functional paradigm | No | No | Yes | No |

If you want to add functional error handling to an existing codebase without a paradigm shift, MonadicSharp fits. If you're building a new system and want full Haskell-style FP in C#, LanguageExt is your answer.

---

## Getting started

```bash
dotnet add package MonadicSharp
```

Or scaffold a full project:

```bash
dotnet new install MonadicSharp.Templates
dotnet new monadic-api   -n MyApi
dotnet new monadic-clean -n MyApp
```

Source and docs: [github.com/Danny4897/MonadicSharp](https://github.com/Danny4897/MonadicSharp)

---

*Built by [Danny4897](https://github.com/Danny4897). Contributions welcome.*
