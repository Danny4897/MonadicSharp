# MonadicSharp.Framework.Caching

`MonadicSharp.Framework.Caching` wraps `IDistributedCache` with result-oriented semantics. Cache misses are represented as `Option.None<T>()`, not null or thrown exceptions.

## Install

```bash
dotnet add package MonadicSharp.Framework.Caching
```

## Core types

### `ICacheService`

```csharp
public interface ICacheService
{
    Task<Option<T>> GetAsync<T>(string key, CancellationToken ct = default);

    Task<Result<T>> GetOrSetAsync<T>(
        string key,
        Func<Task<Result<T>>> factory,
        CacheOptions? options = null,
        CancellationToken ct = default);

    Task<Result<Unit>> SetAsync<T>(
        string key,
        T value,
        CacheOptions? options = null,
        CancellationToken ct = default);

    Task<Result<Unit>> RemoveAsync(string key, CancellationToken ct = default);
}
```

`GetAsync` returns `Option<T>` because a missing entry is a valid state. `SetAsync` and `RemoveAsync` return `Result<Unit>` because write failures are real errors.

### `CacheOptions`

```csharp
var options = new CacheOptions
{
    AbsoluteExpiration = TimeSpan.FromMinutes(30),
    SlidingExpiration = TimeSpan.FromMinutes(5)
};
```

If both are set, the entry expires whichever comes first.

## Backends

### In-memory (development / single instance)

```csharp
builder.Services.AddCaching(opts => opts.UseInMemory());
```

### Redis (production / distributed)

```csharp
builder.Services.AddCaching(opts =>
    opts.UseRedis(builder.Configuration.GetConnectionString("Redis")!));
```

The `ICacheService` API is identical regardless of backend.

## Example: caching an LLM response

LLM calls are expensive and often deterministic for a given input. Cache responses keyed on a hash of the prompt.

```csharp
public sealed class CachedSummarizationService(
    ICacheService cache,
    ILlmClient llm)
{
    public async Task<Result<string>> SummarizeAsync(
        string text,
        CancellationToken ct = default)
    {
        var cacheKey = $"summary:{ComputeHash(text)}";

        return await cache.GetOrSetAsync(
            key: cacheKey,
            factory: () => llm.CompleteAsync(
                $"Summarize in three sentences:\n\n{text}", ct),
            options: new CacheOptions
            {
                AbsoluteExpiration = TimeSpan.FromHours(24)
            },
            ct: ct);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
```

`GetOrSetAsync` only calls the factory if the key is absent. If the factory returns a failure, the failure is propagated and nothing is written to the cache.

## Handling a cache miss explicitly

Use `GetAsync` when you need to distinguish a miss from a hit:

```csharp
var cached = await cache.GetAsync<WeatherForecast>(cacheKey, ct);

return await cached.Match(
    some: forecast => Task.FromResult(Result.Ok(forecast)),
    none: () => FetchAndCacheForecastAsync(city, ct));
```

## Error types

| Error | When |
|---|---|
| `CacheError.Serialization` | Value cannot be serialized/deserialized |
| `CacheError.BackendUnavailable` | Redis connection failure |
| `CacheError.KeyTooLong` | Key exceeds backend limit |

## Registration

```csharp
builder.Services.AddCaching(opts =>
{
    opts.UseRedis(connectionString);
    opts.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(60);
    opts.KeyPrefix = "myapp:";   // prefixed to all keys automatically
});
```
