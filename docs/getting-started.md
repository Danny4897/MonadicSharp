# Getting Started

MonadicSharp is a zero-dependency library that brings Railway-Oriented Programming to C#. Install, write your first `Result<T>`, and let errors propagate automatically.

## Installation

```bash
dotnet add package MonadicSharp
```

**Requirements:** .NET 8.0+ · C# 10.0+

## Your first Result

A `Result<T>` is either `Success(value)` or `Failure(error)` — never both.

```csharp
using MonadicSharp;

// Creating results
Result<int> ok  = Result<int>.Success(42);
Result<int> err = Result<int>.Failure(Error.Validation("Must be positive"));

// Implicit conversions — the idiomatic way
Result<int> a = 42;                            // → Success
Result<int> b = Error.NotFound("User", "99"); // → Failure
```

## Map and Bind

These two methods are the heart of Railway-Oriented Programming.

**`Map`** — transform the value. The operation cannot fail.

```csharp
Result<string> formatted = Result<int>.Success(42)
    .Map(n => $"Value: {n}");
// → Success("Value: 42")
```

**`Bind`** — chain a fallible operation. If the input is already a failure, the function is never called.

```csharp
Result<User> GetActiveUser(int id) =>
    FindUser(id)           // returns Result<User>
        .Bind(ValidateActive)   // returns Result<User>
        .Bind(LoadPermissions); // returns Result<User>
```

If `FindUser` returns a failure, `ValidateActive` and `LoadPermissions` are skipped automatically.

## Unwrap with Match

**Never access `.Value` directly.** Unwrap at the boundary — typically a controller or endpoint — with `Match`:

```csharp
IActionResult result = GetActiveUser(id).Match(
    onSuccess: user  => Ok(user),
    onFailure: error => error.Type switch
    {
        ErrorType.NotFound   => NotFound(error.Message),
        ErrorType.Validation => BadRequest(error.Message),
        ErrorType.Forbidden  => Forbid(),
        _                    => Problem(error.Message)
    });
```

## Compose validation errors

Use `Result.Combine` to run multiple validations and collect **all** failures at once:

```csharp
var result = Result.Combine(
    ValidateName(request.Name),
    ValidateEmail(request.Email),
    ValidateAge(request.Age)
);

// result is Success only if ALL validations pass.
// On failure: a single Error with SubErrors listing each failure.
```

## Real-world pipeline

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

The pipeline stops at the first failure and carries the error to the caller — no `try/catch` anywhere.

## Next steps

- [Result\<T\> API reference](/core/result) — all methods with examples
- [Option\<T\>](/core/option) — replace null with an explicit absence type
- [Structured Errors](/core/error) — semantic, HTTP-mappable, composable
- [Async Pipelines](/pipelines) — PipelineBuilder, ThenWithRetry, ThenIf
