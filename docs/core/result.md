# Result\<T\>

`Result<T>` is the core type of MonadicSharp. It represents the outcome of an operation that can either succeed with a value of type `T`, or fail with a structured `Error`.

```csharp
public readonly struct Result<T> // value type — zero heap allocation
```

## Creating results

```csharp
// Explicit constructors
Result<User> ok  = Result<User>.Success(user);
Result<User> err = Result<User>.Failure(Error.NotFound("User", id.ToString()));
Result<User> msg = Result<User>.Failure("Something went wrong");

// Implicit conversions — preferred
Result<User> a = user;                        // → Success
Result<User> b = Error.NotFound("User", "1"); // → Failure
Result<User> c = new Exception("timeout");    // → Failure (via Error.FromException)

// Static helpers
Result<int> x = Result.Success(42);
Result<int> y = Result.Failure<int>(Error.Validation("Too small"));
```

## Map

Transform the value inside a success. If the result is a failure, `Map` is a no-op and the error passes through.

```csharp
Result<string> name = GetUser(id)
    .Map(user => user.Name);

// Map never creates a new failure — use it for infallible transforms.
Result<UserDto> dto = GetUser(id)
    .Map(u => new UserDto(u.Id, u.Name, u.Email));
```

## Bind

Chain an operation that can itself fail. If the input is a failure, the function is never called.

```csharp
Result<User> GetActiveUser(int id) =>
    FindUser(id)              // Result<User>
        .Bind(ValidateActive) // Result<User>
        .Bind(LoadRoles);     // Result<User>

// Async Bind — works identically
public async Task<Result<OrderDto>> PlaceOrderAsync(PlaceOrderRequest req) =>
    await ValidateRequest(req)
        .Bind(r => _inventory.ReserveAsync(r))
        .Bind(i => _payment.ChargeAsync(i))
        .Bind(p => _orders.SaveAsync(p))
        .Map(o => new OrderDto(o));
```

::: tip Map vs Bind
- Use **`Map`** when the transform cannot fail (pure function).
- Use **`Bind`** when the next step can itself return a `Result<T>`.
:::

## Match

Unwrap the result at the boundary. **Only use Match at I/O boundaries** (controllers, endpoints, CLI handlers).

```csharp
// Returns a value from both branches
IActionResult response = result.Match(
    onSuccess: user  => Ok(user),
    onFailure: error => error.Type switch
    {
        ErrorType.NotFound   => NotFound(error.Message),
        ErrorType.Validation => BadRequest(error.Message),
        ErrorType.Conflict   => Conflict(error.Message),
        ErrorType.Forbidden  => Forbid(),
        _                    => Problem(error.Message)
    });

// Void overload — for side effects only
result.Match(
    onSuccess: user  => Console.WriteLine($"Created: {user.Name}"),
    onFailure: error => Console.WriteLine($"Error: {error.Message}"));
```

## Where (filter)

Converts a success to a failure if the predicate is not satisfied.

```csharp
Result<User> active = GetUser(id)
    .Where(u => u.IsActive, Error.Validation("User is inactive"));

// String overload
Result<int> positive = Parse(input)
    .Where(n => n > 0, "Value must be positive");
```

## MapError

Transform the error while leaving successes untouched. Useful for enriching errors with context.

```csharp
Result<Order> order = PlaceOrder(req)
    .MapError(e => e.WithMetadata("requestId", Request.Headers["X-Request-Id"]));
```

## Do / DoError

Side effects without breaking the chain. Both methods return `this` unchanged.

```csharp
Result<User> user = GetUser(id)
    .Do(u => _logger.LogInformation("User loaded: {Id}", u.Id))      // on success
    .DoError(e => _logger.LogWarning("GetUser failed: {Msg}", e.Message)); // on failure
```

## GetValueOrDefault

Safe extraction without pattern matching. Useful in query scenarios where a fallback value is acceptable.

```csharp
string name = GetUser(id)
    .Map(u => u.Name)
    .GetValueOrDefault("Unknown");

// Factory overload (lazy — only called on failure)
User user = GetUser(id)
    .GetValueOrDefault(() => User.Guest());
```

## ToOption

Converts to `Option<T>`, discarding the error information.

```csharp
Option<User> maybeUser = GetUser(id).ToOption();
```

## Combine

Runs multiple results and collects all errors. The result is a success only if **all** inputs succeed.

```csharp
Result<string[]> all = Result.Combine(
    ValidateName(request.Name),
    ValidateEmail(request.Email),
    ValidateAge(request.Age.ToString())
);

// On failure: Error with SubErrors listing each individual failure.
```

## Try / TryAsync

Wraps a throwing function, capturing exceptions as structured errors.

```csharp
Result<int>           r1 = Result.Try(() => int.Parse(input));
Task<Result<string>>  r2 = Result.TryAsync(() => File.ReadAllTextAsync(path));
```

## Implicit conversions

| Expression | Result |
|-----------|--------|
| `Result<T> r = value` | `Success(value)` |
| `Result<T> r = error` | `Failure(error)` |
| `Result<T> r = exception` | `Failure(Error.FromException(ex))` |

## API reference

| Member | Description |
|--------|-------------|
| `IsSuccess` / `IsFailure` | State predicates |
| `Value` | Throws if failure — use `Match` or `GetValueOrDefault` |
| `Error` | Throws if success |
| `Map<TResult>(Func<T, TResult>)` | Transform value, infallible |
| `Bind<TResult>(Func<T, Result<TResult>>)` | Chain fallible operation |
| `MapError(Func<Error, Error>)` | Transform the error |
| `Do(Action<T>)` | Side effect on success |
| `DoError(Action<Error>)` | Side effect on failure |
| `Match<TResult>(onSuccess, onFailure)` | Unwrap — returns value |
| `Match(onSuccess, onFailure)` | Unwrap — void |
| `Where(predicate, error)` | Filter success by predicate |
| `GetValueOrDefault(T)` | Safe extraction with fallback |
| `ToOption()` | Convert to `Option<T>` |
| `Result.Combine(params Result<T>[])` | Collect all errors |
| `Result.Try(Func<T>)` | Wrap throwing code |
| `Result.TryAsync(Func<Task<T>>)` | Wrap async throwing code |
