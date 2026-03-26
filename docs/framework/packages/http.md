# MonadicSharp.Framework.Http

`MonadicSharp.Framework.Http` wraps `HttpClient` in a typed, result-oriented interface. Transient failures, rate limits, and timeouts are returned as typed errors instead of thrown exceptions.

## Install

```bash
dotnet add package MonadicSharp.Framework.Http
```

## Core types

### `IMonadicHttpClient`

```csharp
public interface IMonadicHttpClient
{
    Task<Result<TResponse>> GetMonadicAsync<TResponse>(
        string requestUri,
        RequestOptions? options = null,
        CancellationToken ct = default);

    Task<Result<TResponse>> PostMonadicAsync<TRequest, TResponse>(
        string requestUri,
        TRequest body,
        RequestOptions? options = null,
        CancellationToken ct = default);

    Task<Result<TResponse>> PutMonadicAsync<TRequest, TResponse>(
        string requestUri,
        TRequest body,
        RequestOptions? options = null,
        CancellationToken ct = default);

    Task<Result<Unit>> DeleteMonadicAsync(
        string requestUri,
        RequestOptions? options = null,
        CancellationToken ct = default);
}
```

Serialization is System.Text.Json. Configure a custom `JsonSerializerOptions` via `RequestOptions.SerializerOptions`.

### `RequestOptions`

```csharp
var options = new RequestOptions
{
    Timeout = TimeSpan.FromSeconds(10),
    RetryPolicy = RetryPolicy.Default,      // or RetryPolicy.None
    Headers = { ["X-Correlation-Id"] = correlationId }
};
```

## Error types

| Error | HTTP condition | Default behavior |
|---|---|---|
| `HttpError.NetworkFailure` | Connection refused, DNS failure | Retry up to `MaxRetries` |
| `HttpError.Timeout` | Request exceeded `Timeout` | Retry up to `MaxRetries` |
| `HttpError.RateLimit` | HTTP 429 | Retry with exponential backoff |
| `HttpError.Unauthorized` | HTTP 401 | Fail-fast, no retry |
| `HttpError.Forbidden` | HTTP 403 | Fail-fast, no retry |
| `HttpError.NotFound` | HTTP 404 | Fail-fast, no retry |
| `HttpError.ServerError` | HTTP 5xx | Retry up to `MaxRetries` |

Retry and fail-fast behavior is determined by the built-in retry policy. Override it per-request with `RequestOptions.RetryPolicy`.

## Retry policy

```csharp
var options = new RequestOptions
{
    RetryPolicy = new RetryPolicy
    {
        MaxRetries = 4,
        BaseDelay = TimeSpan.FromMilliseconds(200),
        BackoffMultiplier = 2.0,
        RetryOn = error => error is HttpError.RateLimit
                        or HttpError.NetworkFailure
                        or HttpError.Timeout
                        or HttpError.ServerError
    }
};
```

Setting `RetryOn` to a custom predicate overrides the default behavior completely.

## Example: external REST call with retry and timeout

```csharp
public sealed class WeatherClient(IMonadicHttpClient http)
{
    private const string BaseUrl = "https://api.weather.example.com";

    public async Task<Result<WeatherForecast>> GetForecastAsync(
        string city,
        CancellationToken ct = default)
    {
        var options = new RequestOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
            RetryPolicy = new RetryPolicy
            {
                MaxRetries = 3,
                BaseDelay = TimeSpan.FromMilliseconds(300),
                BackoffMultiplier = 2.0,
                RetryOn = err => err is HttpError.RateLimit or HttpError.Timeout
            }
        };

        return await http.GetMonadicAsync<WeatherForecast>(
            $"{BaseUrl}/forecast?city={Uri.EscapeDataString(city)}",
            options,
            ct);
    }
}
```

Callers receive a `Result<WeatherForecast>`. They never need to handle `HttpRequestException`.

## HttpResultExtensions

Extension methods on `Result<HttpResponseMessage>` for common transformations:

```csharp
// Deserialize or map failure
Result<MyDto> dto = await response.DeserializeAsync<MyDto>();

// Check for specific status
Result<Unit> ok = response.EnsureSuccessStatus();
```

## Registration

```csharp
builder.Services.AddMonadicHttpClient(opts =>
{
    opts.DefaultTimeout = TimeSpan.FromSeconds(30);
    opts.DefaultMaxRetries = 3;
});
```

Named clients work the same way:

```csharp
builder.Services.AddMonadicHttpClient("openai", opts =>
{
    opts.BaseAddress = new Uri("https://api.openai.com");
    opts.DefaultTimeout = TimeSpan.FromSeconds(60);
    opts.DefaultHeaders["Authorization"] = $"Bearer {apiKey}";
});
```

Inject with:

```csharp
public class OpenAiClient(IMonadicHttpClientFactory factory)
{
    private readonly IMonadicHttpClient _http = factory.CreateClient("openai");
}
```
