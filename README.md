# MonadicSharp

[![NuGet Version](https://img.shields.io/nuget/v/MonadicSharp.svg)](https://www.nuget.org/packages/MonadicSharp/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MonadicSharp.svg)](https://www.nuget.org/packages/MonadicSharp/)
[![CI](https://github.com/Danny4897/MonadicSharp/actions/workflows/ci.yml/badge.svg)](https://github.com/Danny4897/MonadicSharp/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)

Railway-Oriented Programming for C#. Replace exception-driven control flow with composable, explicit error handling.

```bash
dotnet add package MonadicSharp
```

---

## The problem

```csharp
// You never know what this can throw, or when, or why
public User CreateUser(string name, string email)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new ValidationException("Name is required");

    if (!email.Contains('@'))
        throw new ValidationException("Invalid email");

    var user = _db.Users.FirstOrDefault(u => u.Email == email);
    if (user != null)
        throw new ConflictException("Email already exists");

    return _db.Save(new User(name, email));
}
```

Every caller needs a try/catch. Errors are invisible in the signature. Composing multiple operations that can fail is painful.

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
Result<int> Parse(string input) =>
    int.TryParse(input, out var n)
        ? Result<int>.Success(n)
        : Result<int>.Failure(Error.Validation("Not a number", field: "input"));

var result = Parse("42")
    .Map(n => n * 2)
    .Where(n => n < 200, "Value too large")
    .Match(
        onSuccess: n  => $"Result: {n}",
        onFailure: err => $"Error: {err.Message}"
    );
// "Result: 84"
```

### `Option<T>` — value or nothing

```csharp
Option<User> FindUser(int id) =>
    _db.TryGetValue(id, out var user) ? Option<User>.Some(user) : Option<User>.None;

string name = FindUser(42)
    .Map(u => u.Name.ToUpper())
    .GetValueOrDefault("Anonymous");
```

### `Error` — structured, composable errors

```csharp
// Semantic factory methods
Error.Validation("Name is required", field: "name")
Error.NotFound("User", identifier: id.ToString())
Error.Forbidden("Admin access required")
Error.Conflict("Email already registered", resource: "email")
Error.FromException(ex)

// Enrich with context
Error.Create("Payment failed")
    .WithMetadata("orderId", order.Id)
    .WithMetadata("amount", order.Total)
    .WithInnerError(gatewayError)

// Combine multiple errors
Error.Combine(nameError, emailError, ageError)
```

### `Either<TLeft, TRight>` — two explicit tracks

```csharp
Either<Error, User> Authenticate(string token) =>
    _tokenService.Validate(token)
        ? Either<Error, User>.FromRight(_users.GetByToken(token))
        : Either<Error, User>.FromLeft(Error.Forbidden("Invalid token"));

var message = Authenticate(token).Match(
    onLeft:  err  => $"Denied: {err.Message}",
    onRight: user => $"Welcome, {user.Name}"
);
```

---

## Async pipelines

Chain async operations with automatic short-circuiting on failure:

```csharp
var result = await GetOrderAsync(orderId)        // Task<Result<Order>>
    .Then(ValidateInventoryAsync)                // stops here if invalid
    .Then(ReserveStockAsync)
    .Then(SendConfirmationEmailAsync)
    .ExecuteAsync();
```

### Conditional steps

```csharp
await ProcessOrder(order)
    .ThenIf(o => o.Total > 1000, ApplyDiscountAsync)
    .ThenIf(o => o.IsInternational, CalculateShippingAsync)
    .ExecuteAsync();
```

### Built-in retry

```csharp
await GetOrder(id)
    .ThenWithRetry(
        operation:   CallExternalPaymentApiAsync,
        maxAttempts: 3,
        delay:       TimeSpan.FromSeconds(2))
    .ExecuteAsync();
```

---

## Entity Framework Core integration

`DbSetExtensions` wraps EF Core operations to return `Result<T>` and `Option<T>` instead of throwing:

```csharp
// FindAsync returns Option<T> — no null checks needed
Option<User> user = await _db.Users.FindAsync(id);

// AddAsync returns Result<T>
Result<User> created = await _db.Users.AddAsync(newUser);

// Composable pipeline with EF
var result = await _db.Users.FindAsync(id)
    .ToResult(Error.NotFound("User", id.ToString()))
    .BindAsync(user => _db.Users.Update(updatedUser))
    .BindAsync(_ => _db.SaveChangesAsync());
```

---

## Collecting multiple errors

`Sequence` and `Traverse` let you run all validations and collect every failure — not just the first one:

```csharp
var errors = new[]
{
    ValidateName(name),
    ValidateEmail(email),
    ValidateAge(age)
}.Sequence();

errors.Match(
    onSuccess: values => Save(values),
    onFailure: err    => ReturnAllErrors(err.GetAllErrors())
);
```

---

## Project templates

Scaffold a full project pre-wired with MonadicSharp:

```bash
dotnet new install MonadicSharp.Templates

dotnet new monadic-api   -n MyApi    # Minimal API + Result pattern + EF Core
dotnet new monadic-clean -n MyApp    # Clean Architecture + CQRS + MediatR
```

---

## Requirements

- .NET 8.0+
- C# 10.0+

## Contributing

Issues and pull requests are welcome. See [CHANGELOG](CHANGELOG.md) for version history.

## License

MIT — see [LICENSE](LICENSE).
