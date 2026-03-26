# Error

`Error` is an immutable, composable record that carries structured failure information. Every `Result<T>` failure wraps an `Error`.

```csharp
public sealed record Error
{
    public string   Code      { get; init; }
    public string   Message   { get; init; }
    public ErrorType Type     { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; }
    public Error?   InnerError { get; init; }
    public IReadOnlyList<Error> SubErrors { get; init; }
}
```

## ErrorType

Maps directly to HTTP status codes — no conversion layer needed.

| `ErrorType`  | HTTP equivalent | Use when |
|-------------|-----------------|----------|
| `Failure`    | 500             | Unexpected internal error |
| `Validation` | 400             | Input does not meet rules |
| `NotFound`   | 404             | Resource does not exist |
| `Forbidden`  | 403             | Access denied |
| `Conflict`   | 409             | Duplicate or state conflict |
| `Exception`  | 500             | Captured exception |

## Factory methods

### Error.Validation

```csharp
// Without field
Error e = Error.Validation("Name is required");

// With field — adds Metadata["Field"]
Error e = Error.Validation("Name is required", field: "name");
```

### Error.NotFound

```csharp
Error e = Error.NotFound("User");
Error e = Error.NotFound("User", identifier: "42"); // adds Metadata["Identifier"]
```

### Error.Conflict

```csharp
Error e = Error.Conflict("Email already registered");
Error e = Error.Conflict("Email already registered", resource: "User"); // adds Metadata["Resource"]
```

### Error.Forbidden

```csharp
Error e = Error.Forbidden();                     // "Access denied"
Error e = Error.Forbidden("Insufficient tier");
```

### Error.Create

Generic factory for custom codes and types.

```csharp
Error e = Error.Create("Rate limit exceeded", code: "RATE_LIMIT", type: ErrorType.Failure);
```

### Error.FromException

Captures an exception as a structured error (preserves `ExceptionType` and `StackTrace` in metadata).

```csharp
Result<string> r = Result.Try(() => File.ReadAllText(path));
// Internally: Error.FromException(ex) on any thrown exception
```

## Enriching errors

### WithMetadata

Attach arbitrary key-value pairs without creating a new factory.

```csharp
Result<Order> order = PlaceOrder(req)
    .MapError(e => e
        .WithMetadata("requestId", Request.Headers["X-Request-Id"])
        .WithMetadata("userId",    currentUserId));
```

### WithInnerError

Wrap a lower-level error as context for a higher-level one.

```csharp
Error high = Error.Conflict("Cannot delete user with active orders")
    .WithInnerError(repositoryError);
```

## Composing errors

### Error.Combine

Merge multiple errors into one with `SubErrors` listing each individual failure. Used internally by `Result.Combine`.

```csharp
Error combined = Error.Combine(
    Error.Validation("Name is required"),
    Error.Validation("Email is invalid"),
    Error.Validation("Age must be positive")
);
// combined.SubErrors.Count == 3
// combined.Code == "MULTIPLE_ERRORS"
```

### GetAllErrors

Flatten the entire error tree (root + sub-errors + inner chain) into a sequence.

```csharp
foreach (var e in combinedError.GetAllErrors())
    logger.LogError("{Code}: {Message}", e.Code, e.Message);
```

## Dispatching at the boundary

```csharp
IActionResult response = result.Match(
    onSuccess: dto   => Ok(dto),
    onFailure: error => error.Type switch
    {
        ErrorType.NotFound   => NotFound(error.Message),
        ErrorType.Validation => BadRequest(new { error.Message, Fields = error.SubErrors.Select(e => e.Metadata) }),
        ErrorType.Conflict   => Conflict(error.Message),
        ErrorType.Forbidden  => Forbid(),
        _                    => Problem(error.Message)
    });
```

## Serialization / interop

```csharp
// JSON (camelCase, indented)
string json = error.ToJson();

// Convert to exception — for middleware or Application Insights
MonadicException ex = error.ToException();
throw ex; // only at true boundaries
```

## Predicates

```csharp
error.IsOfType(ErrorType.Validation); // true/false
error.HasCode("NOT_FOUND");           // case-insensitive
```

## API reference

| Member | Description |
|--------|-------------|
| `Code` | Machine-readable identifier (e.g. `"NOT_FOUND"`) |
| `Message` | Human-readable description |
| `Type` | `ErrorType` enum |
| `Metadata` | Key-value pairs attached to the error |
| `InnerError` | Wraps a lower-level cause |
| `SubErrors` | List of child errors (from `Combine`) |
| `Error.Create(message, code?, type?)` | Generic factory |
| `Error.Validation(message, field?)` | Validation error |
| `Error.NotFound(resource, id?)` | Not found error |
| `Error.Conflict(message, resource?)` | Conflict error |
| `Error.Forbidden(message?)` | Access denied |
| `Error.FromException(ex, code?)` | Wrap thrown exception |
| `Error.Combine(params Error[])` | Merge into one with SubErrors |
| `WithMetadata(key, value)` | Attach metadata (immutable) |
| `WithInnerError(error)` | Nest a cause (immutable) |
| `GetAllErrors()` | Flatten tree |
| `IsOfType(type)` | Predicate on ErrorType |
| `HasCode(code)` | Predicate on Code (case-insensitive) |
| `ToJson()` | Serialize to JSON |
| `ToException()` | Convert to `MonadicException` |
