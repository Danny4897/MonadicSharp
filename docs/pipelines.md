# Async Pipelines

MonadicSharp has first-class support for `Task<Result<T>>`. Every `Bind` and `Map` has an async overload, and `PipelineBuilder` provides a fluent API for composing multi-step async workflows.

## Async Bind and Map

Chain async operations directly — the compiler handles the `await` internally.

```csharp
public async Task<Result<OrderDto>> PlaceOrderAsync(PlaceOrderRequest request) =>
    await ValidateRequest(request)
        .Bind(req   => _inventory.ReserveAsync(req))   // Task<Result<Reserved>>
        .Bind(inv   => _payment.ChargeAsync(inv))      // Task<Result<Payment>>
        .Bind(pay   => _orders.SaveAsync(pay))         // Task<Result<Order>>
        .Map(order  => new OrderDto(order));            // sync transform
```

The pipeline stops at the first failure. Subsequent steps are never awaited.

## PipelineBuilder

`PipelineBuilder<T>` is the fluent builder for homogeneous pipelines — all steps share the same `T`.

```csharp
Result<Document> result = await GetDocumentAsync(id)
    .Then(doc  => ValidateSchemaAsync(doc))
    .Then(doc  => EnrichMetadataAsync(doc))
    .Then(doc  => IndexAsync(doc))
    .ExecuteAsync();
```

### ThenIf — conditional step

```csharp
Result<Order> result = await CreateOrderAsync(req)
    .Then(order => EnrichAsync(order))
    .ThenIf(
        condition: order => order.RequiresApproval,
        operation: order => RequestApprovalAsync(order))
    .ExecuteAsync();
```

`ThenIf` passes the value through unchanged when the condition is `false`.

### Do — side effects

```csharp
Result<User> result = await CreateUserAsync(req)
    .Then(user => SaveAsync(user))
    .Do(user   => _logger.LogInformationAsync("User created: {Id}", user.Id))
    .Then(user => SendWelcomeEmailAsync(user))
    .ExecuteAsync();
```

`Do` does not modify the value — the pipeline continues with the same `T`.

## ThenIf extension method

Available directly on `Task<Result<T>>` — no builder required.

```csharp
Task<Result<Order>> pipeline = CreateOrderAsync(req)
    .ThenIf(
        condition: o => o.Amount > 10_000,
        operation: o => FraudCheckAsync(o));
```

## ThenWithRetry

Automatically retries a step on failure.

```csharp
Task<Result<string>> result = FetchDataAsync(url)
    .ThenWithRetry(
        operation:   data => ProcessAsync(data),
        maxAttempts: 3,
        delay:       TimeSpan.FromSeconds(2));
```

::: warning Retry scope
`ThenWithRetry` retries the single operation, not the entire pipeline. Place it immediately after the step that may transiently fail.
:::

## PipelineAsync (array variant)

Pass steps as an array when composing programmatically.

```csharp
Func<Order, Task<Result<Order>>>[] steps = [
    EnrichAsync,
    ValidatePricingAsync,
    ApplyDiscountsAsync,
];

Result<Order> result = await GetOrderAsync(id)
    .PipelineAsync(steps);
```

## Functional extensions for Task\<Result\<T\>\>

`TaskResultExtensions` (in `MonadicSharp.Extensions`) adds Map/Bind/Do directly on `Task<Result<T>>`:

```csharp
using MonadicSharp.Extensions;

Result<string> r = await _db.FindAsync(id)   // Task<Result<User>>
    .Map(u  => u.Email)                       // sync map
    .Bind(e => SendEmailAsync(e))             // async bind
    .Do(async _ => await AuditAsync("sent")); // async side effect
```

## Sequence and Traverse

Process collections of results.

```csharp
using MonadicSharp.Extensions;

// All must succeed — fails with combined errors if any fail
Result<IEnumerable<UserDto>> all = users
    .Select(u => ValidateUser(u))
    .Sequence();

// Apply a function to each element
Result<IEnumerable<UserDto>> validated = userList
    .Traverse(u => ValidateUser(u));
```

## Partition

For batch processing where partial success is acceptable.

```csharp
var (successes, failures) = orders
    .Select(o => ProcessOrder(o))
    .Partition();

_logger.LogWarning("{Count} orders failed", failures.Count());
await SaveSuccessfulOrdersAsync(successes);
```

## OrElse — fallback

Recover from a failure with an alternative result.

```csharp
Result<User> user = GetFromCache(id)
    .OrElse(_ => GetFromDatabase(id));

// Static alternative
Result<Config> config = LoadFromFile(path)
    .OrElse(Config.Default());
```

## API reference

| Member | Description |
|--------|-------------|
| `Task<Result<T>>.Bind(Func<T, Task<Result<TResult>>>)` | Async chain |
| `Task<Result<T>>.Map(Func<T, TResult>)` | Sync transform on async result |
| `Task<Result<T>>.Do(Func<T, Task>)` | Async side effect |
| `Task<Result<T>>.Then(operation)` → `PipelineBuilder<T>` | Start fluent builder |
| `PipelineBuilder<T>.Then(operation)` | Add a step |
| `PipelineBuilder<T>.ThenIf(condition, operation)` | Conditional step |
| `PipelineBuilder<T>.Do(sideEffect)` | Side-effect step |
| `PipelineBuilder<T>.ExecuteAsync()` | Execute the pipeline |
| `ThenIf(condition, operation)` | Extension on `Task<Result<T>>` |
| `ThenWithRetry(operation, maxAttempts, delay)` | Retry on failure |
| `PipelineAsync(params operations[])` | Array-based pipeline |
| `Sequence()` | `IEnumerable<Result<T>>` → `Result<IEnumerable<T>>` |
| `Traverse(selector)` | Map + Sequence |
| `Partition()` | Split into successes and failures |
| `OrElse(recovery)` | Fallback on failure |
