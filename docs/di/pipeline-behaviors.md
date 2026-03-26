# Pipeline Behaviors

Pipeline behaviors wrap every handler invocation. They run in a chain: each behavior calls `next()` to continue to the next behavior (or the handler itself). This is the standard middleware/decorator pattern applied to the mediator.

## Execution order

Behaviors execute in the order they are registered. The handler runs last.

```
Request
  → Behavior 1 (before next)
    → Behavior 2 (before next)
      → Behavior 3 (before next)
        → Handler
      → Behavior 3 (after next)
    → Behavior 2 (after next)
  → Behavior 1 (after next)
Response
```

```csharp
// Registration order determines execution order
builder.Services.AddMonadicMediator(typeof(Program).Assembly);
builder.Services.AddBehavior(typeof(LoggingBehavior<,>));      // outermost
builder.Services.AddBehavior(typeof(ValidationBehavior<,>));   // middle
builder.Services.AddBehavior(typeof(CachingBehavior<,>));      // innermost (closest to handler)
```

## IPipelineBehavior\<TRequest, TResult\>

```csharp
public interface IPipelineBehavior<TRequest, TResult>
{
    Task<Result<TResult>> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken ct);
}
```

`RequestHandlerDelegate<TResult>` is `Func<Task<Result<TResult>>>`. Call `await next()` to continue the pipeline. Return early (without calling `next`) to short-circuit.

## ValidationBehavior

Runs before the handler. Returns a validation failure immediately without invoking `next()`.

```csharp
public class ValidationBehavior<TRequest, TResult>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResult>
    where TRequest : ICommand<TResult>
{
    public async Task<Result<TResult>> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
        {
            var error = Error.Validation(
                failures.Select(f => new ValidationFailure(f.PropertyName, f.ErrorMessage)));

            return Result.Failure<TResult>(error);
        }

        return await next();
    }
}
```

Register FluentValidation validators separately:

```csharp
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddBehavior(typeof(ValidationBehavior<,>));
```

## LoggingBehavior

Wraps `next()` with structured logging. Runs on both sides of the handler so you get duration and outcome.

```csharp
public class LoggingBehavior<TRequest, TResult>(ILogger<LoggingBehavior<TRequest, TResult>> logger)
    : IPipelineBehavior<TRequest, TResult>
{
    public async Task<Result<TResult>> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {Request}", requestName);

        var sw = Stopwatch.StartNew();
        var result = await next();
        sw.Stop();

        if (result.IsSuccess)
            logger.LogInformation("{Request} succeeded in {Elapsed}ms", requestName, sw.ElapsedMilliseconds);
        else
            logger.LogWarning("{Request} failed in {Elapsed}ms — {Error}", requestName, sw.ElapsedMilliseconds, result.Error);

        return result;
    }
}
```

## CachingBehavior

Short-circuits the pipeline on cache hit — `next()` is never called. Apply only to queries that implement `ICacheable`.

```csharp
public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan Expiry  { get; }
}

public class CachingBehavior<TRequest, TResult>(IDistributedCache cache)
    : IPipelineBehavior<TRequest, TResult>
    where TRequest : IQuery<TResult>, ICacheable
{
    public async Task<Result<TResult>> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken ct)
    {
        var cached = await cache.GetStringAsync(request.CacheKey, ct);
        if (cached is not null)
            return Result.Success(JsonSerializer.Deserialize<TResult>(cached)!);

        var result = await next();

        if (result.IsSuccess)
        {
            var json = JsonSerializer.Serialize(result.Value);
            await cache.SetStringAsync(request.CacheKey, json,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = request.Expiry },
                ct);
        }

        return result;
    }
}
```

```csharp
// Query that opts in to caching
public record GetProductCatalogQuery(string Category)
    : IQuery<List<ProductDto>>, ICacheable
{
    public string CacheKey => $"catalog:{Category}";
    public TimeSpan Expiry  => TimeSpan.FromMinutes(5);
}
```

## Composing behaviors

Behaviors are composable: each one is independent and knows nothing about the others. Common compositions:

**API endpoints (commands)**
```csharp
builder.Services.AddBehavior(typeof(LoggingBehavior<,>));
builder.Services.AddBehavior(typeof(ValidationBehavior<,>));
```

**Read-heavy queries with caching**
```csharp
builder.Services.AddBehavior(typeof(LoggingBehavior<,>));
builder.Services.AddBehavior(typeof(CachingBehavior<,>));
```

**Background jobs (no validation, lightweight logging)**
```csharp
builder.Services.AddBehavior(typeof(LoggingBehavior<,>));
```

Because `AddBehavior` is open-generic, one registration covers all request types. Use generic constraints (like `where TRequest : ICommand<TResult>`) inside the behavior class to limit which requests it runs for.
