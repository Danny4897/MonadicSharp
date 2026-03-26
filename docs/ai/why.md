# Why MonadicSharp.AI

LLMs fail in ways that are fundamentally different from ordinary I/O. Rate limits, token exhaustion, content filters, and malformed JSON responses are not exceptional — they are routine. Wrapping every call in `try/catch` works for a single call, but it falls apart the moment you compose multiple LLM steps into a pipeline.

## The Problem with try/catch

Consider a typical implementation without MonadicSharp.AI:

```csharp
public async Task<WeatherReport> GetWeatherReportAsync(string location)
{
    try
    {
        var rawJson = await _llm.CompleteAsync($"Return weather for {location} as JSON.");

        WeatherReport report;
        try
        {
            report = JsonSerializer.Deserialize<WeatherReport>(rawJson)
                ?? throw new InvalidOperationException("Null response");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"LLM returned invalid JSON: {ex.Message}");
        }

        return report;
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
    {
        // retry? how many times? where is that logic?
        throw;
    }
    catch (TaskCanceledException)
    {
        // timeout — log? retry? propagate?
        throw;
    }
}
```

Problems with this approach:

- **Error type is erased.** By the time the caller receives an exception, the original reason (rate limit vs. timeout vs. bad JSON) is buried in message strings.
- **Composition is manual.** Chaining three LLM steps requires three nested try/catch blocks. Each step breaks the linear flow.
- **Retry logic leaks.** Deciding when and how to retry has to be duplicated at every call site.
- **Tracing is opt-in and fragile.** Logging which step failed and with what token count requires manual instrumentation at each boundary.

## The MonadicSharp.AI Approach

Every operation returns `Result<T>`. Failures are `AiError` values — first-class data, not exceptions. The pipeline stays linear regardless of how many steps it has.

```csharp
public async Task<Result<WeatherReport>> GetWeatherReportAsync(string location)
{
    return await RetryResult<string>
        .ExecuteAsync(
            maxAttempts: 3,
            action: () => _llm.CompleteAsync($"Return weather for {location} as JSON."),
            shouldRetry: err => err is AiError.RateLimit or AiError.Timeout)
        .BindAsync(raw => ValidatedResult<WeatherReport>.ParseAndValidateAsync(raw))
        .MapAsync(report => report with { FetchedAt = DateTime.UtcNow });
}
```

The caller decides what to do with success or failure, once, at the boundary:

```csharp
var result = await GetWeatherReportAsync("Milan");

result.Match(
    onSuccess: report => Console.WriteLine($"Temp: {report.Temperature}°C"),
    onFailure: err    => logger.LogError("Pipeline failed: {Err}", err));
```

## AiError as a Value

`AiError` is a discriminated union. Each case carries structured data — not a message string.

```csharp
// Before: stringly-typed, lost at the catch boundary
catch (Exception ex) when (ex.Message.Contains("rate limit")) { ... }

// After: typed, matchable, composable
err.Match(
    onRateLimit:       e => $"Retry after {e.RetryAfter.TotalSeconds}s",
    onTimeout:         e => $"Elapsed: {e.Elapsed.TotalSeconds}s",
    onTokenExhausted:  e => $"Used {e.TokensUsed} tokens",
    onContentFiltered: e => $"Blocked: {e.Reason}",
    onInvalidResponse: e => $"Raw: {e.Raw[..100]}");
```

Because `AiError` is a value, you can store it, compare it, serialize it, and pass it to telemetry without losing information.

## Pipeline Composition

`Result<T>` composes with `Bind`, `Map`, and `BindAsync`. A three-step pipeline with typed errors at every boundary:

```csharp
var traced = await AgentResult<SummaryReport>
    .StartAsync("fetch",    () => FetchDocumentAsync(docId))
    .StepAsync("summarize", text  => _llm.SummarizeAsync(text))
    .StepAsync("validate",  raw   => ValidatedResult<SummaryReport>.ParseAndValidateAsync(raw));

// Steps and token usage are recorded automatically
Console.WriteLine($"Completed {traced.Steps.Count} steps using {traced.TotalTokens} tokens");
```

If any step fails, the pipeline short-circuits. No guard clauses, no null checks, no nested try/catch.

## Typed Retry

`RetryResult<T>` keeps retry policy close to the error type that triggers it:

```csharp
// Retry only on transient errors — not on content filter violations
var result = await RetryResult<string>
    .ExecuteAsync(
        maxAttempts: 4,
        action: () => _llm.CompleteAsync(prompt),
        shouldRetry: err => err is AiError.RateLimit or AiError.Timeout)
    .WithDelay(TimeSpan.FromSeconds(2))
    .WithJitter(maxJitter: TimeSpan.FromMilliseconds(500));
```

Compare this to Polly, where the retry condition, the delay policy, and the action are defined in separate places and the error type must be re-inspected at runtime.

## Step Traceability

`AgentResult<T>` records each step name, its outcome, and the tokens consumed. This gives you a full audit trail without manual instrumentation:

```csharp
foreach (var step in traced.Steps)
{
    Console.WriteLine($"[{step.Name}] {(step.Succeeded ? "OK" : "FAILED")} — {step.TokensUsed} tokens");
}
```

Useful for cost attribution, debugging, and observability dashboards — without adding logging code to each step.
