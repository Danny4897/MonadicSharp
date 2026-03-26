# ROL Patterns

ROL (Railway-Oriented Learning) is the practice of recognizing common imperative C# patterns and transforming them into their green equivalents. Each pattern below shows the original code and the MonadicSharp-idiomatic replacement.

## Pattern 1: try/catch → RescueAsync

### Problem

`try/catch` is exception-based control flow. The failure path is invisible to the type system and difficult to compose.

**Before:**

```csharp
public async Task<string> FetchUserNameAsync(Guid userId)
{
    try
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            throw new KeyNotFoundException($"User {userId} not found");

        return user.Name;
    }
    catch (DbException ex)
    {
        _logger.LogError(ex, "Database error");
        throw;
    }
}
```

**After:**

```csharp
public async Task<Result<string>> FetchUserNameAsync(Guid userId)
{
    return await _users.FindByIdAsync(userId)
        .ToResult(new PersistenceError.NotFound(userId.ToString()))
        .MapAsync(user => user.Name)
        .RescueAsync(ex => ex is DbException
            ? Result.Fail<string>(new PersistenceError.DatabaseUnavailable(ex.Message))
            : Result.Fail<string>(new UnexpectedError(ex.Message)));
}
```

`RescueAsync` is the green equivalent of `catch`. It transforms a failure `Result` into another `Result` — it never throws. The original exception is gone; what remains is a typed error value.

## Pattern 2: null return → Option\<T\>

### Problem

Returning `null` for "not found" forces the caller to remember to null-check. The compiler can warn but cannot enforce handling.

**Before:**

```csharp
public async Task<Product?> FindBySkuAsync(string sku)
{
    return await _db.Products
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Sku == sku);
}

// caller
var product = await _catalog.FindBySkuAsync(sku);
var price = product?.Price ?? 0m; // silent default — is this correct?
```

**After:**

```csharp
public async Task<Option<Product>> FindBySkuAsync(string sku)
{
    var product = await _db.Products
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Sku == sku);

    return product is null ? Option.None<Product>() : Option.Some(product);
}

// caller — both paths are explicit
var result = await _catalog.FindBySkuAsync(sku);

return result.Match(
    some: product => Result.Ok(product.Price),
    none: () => Result.Fail<decimal>(new CatalogError.ProductNotFound(sku)));
```

The caller cannot access `.Price` without first handling the `None` case.

## Pattern 3: bool return for failable operations → Result\<T\>

### Problem

`bool` return tells you that something failed but not why. Callers must inspect side effects or logs to understand what went wrong.

**Before:**

```csharp
public bool SendNotification(Guid userId, string message)
{
    try
    {
        var endpoint = _registry.GetEndpoint(userId);
        if (endpoint is null) return false;

        _smtp.Send(endpoint.Email, message);
        return true;
    }
    catch
    {
        return false;
    }
}
```

**After:**

```csharp
public async Task<Result<Unit>> SendNotificationAsync(Guid userId, string message)
{
    return await _registry.FindEndpointAsync(userId)
        .ToResult(new NotificationError.EndpointNotRegistered(userId))
        .BindAsync(endpoint => _smtp.SendAsync(endpoint.Email, message));
}
```

The caller now knows whether the failure was a missing endpoint or an SMTP error. Both are modeled as distinct error types.

## Pattern 4: exception-based validation → ValidatedResult\<T\>

### Problem

Throwing validation exceptions creates a special control flow path that must be caught somewhere up the stack. Multiple validation errors can only be reported one at a time.

**Before:**

```csharp
public void Validate(CreateOrderRequest request)
{
    if (string.IsNullOrWhiteSpace(request.CustomerId))
        throw new ValidationException("CustomerId is required");

    if (request.Items.Count == 0)
        throw new ValidationException("Order must have at least one item");

    if (request.Items.Any(i => i.Quantity <= 0))
        throw new ValidationException("All quantities must be positive");
}
```

**After:**

```csharp
public ValidatedResult<CreateOrderRequest> Validate(CreateOrderRequest request)
{
    return Validated.Start(request)
        .Require(r => !string.IsNullOrWhiteSpace(r.CustomerId),
            "CustomerId is required")
        .Require(r => r.Items.Count > 0,
            "Order must have at least one item")
        .Require(r => r.Items.All(i => i.Quantity > 0),
            "All quantities must be positive")
        .ToResult();
}
```

`ValidatedResult<T>` collects all failures before returning. Callers receive the full list of errors in a single pass — no repeated submissions required.

### Using `ValidatedResult<T>` in a pipeline

```csharp
public async Task<Result<Order>> CreateOrderAsync(CreateOrderRequest request)
{
    return await _validator.Validate(request)
        .ToResult()
        .BindAsync(r => _inventory.ReserveItemsAsync(r.Items))
        .BindAsync(_ => _orders.SaveAsync(Order.From(request)));
}
```

`ValidatedResult<T>.ToResult()` converts to `Result<T>` (with the first error) or `Result<IReadOnlyList<string>>` (with all errors) depending on the overload used.

## Quick reference

| Imperative pattern | Green equivalent |
|---|---|
| `throw` / `catch` | `Result<T>` + `RescueAsync` |
| `T?` return for "not found" | `Option<T>` + `Match` |
| `bool` return for "did it work" | `Result<Unit>` |
| `bool` return for "did it work, here's the result" | `Result<T>` |
| Multiple validation throws | `ValidatedResult<T>` |
| `if (result != null) { ... }` chains | `BindAsync` / `MapAsync` |
