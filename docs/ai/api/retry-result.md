# RetryResult\<T\>

`RetryResult<T>` executes an async action up to `maxAttempts` times, retrying only when the `shouldRetry` predicate returns `true` for the produced `AiError`. On success it returns `Result<T>`. On exhaustion it returns `Result.Fail` with the last error.

```csharp
using MonadicSharp.AI;
```

## ExecuteAsync

```csharp
public static Task<Result<T>> ExecuteAsync(
    int maxAttempts,
    Func<Task<Result<T>>> action,
    Func<AiError, bool> shouldRetry)
```

**Parameters**

| Parameter | Type | Description |
|---|---|---|
| `maxAttempts` | `int` | Maximum number of total attempts (initial + retries). Must be ≥ 1. |
| `action` | `Func<Task<Result<T>>>` | The async factory to call on each attempt. Called fresh every time. |
| `shouldRetry` | `Func<AiError, bool>` | Returns `true` if the error is retryable. Called only on failure. |

**Returns**: `Task<Result<T>>` — succeeds with the first successful attempt, or fails with the last error if all attempts produce a non-retryable failure or `maxAttempts` is exhausted.

**Behaviour**

1. Calls `action()`.
2. If the result is `Ok`, returns immediately.
3. If the result is `Fail` and `shouldRetry(error)` is `false`, returns immediately with that error.
4. If the result is `Fail` and `shouldRetry(error)` is `true`, increments the attempt counter and repeats from step 1 if attempts remain.
5. After exhausting all attempts, returns the last failure.

**Example — basic retry on transient errors**

```csharp
var result = await RetryResult<string>
    .ExecuteAsync(
        maxAttempts: 3,
        action: () => _llm.CompleteAsync(prompt),
        shouldRetry: err => err is AiError.RateLimit or AiError.Timeout);

result.Match(
    onSuccess: text => Console.WriteLine(text),
    onFailure: err  => logger.LogError("Failed after 3 attempts: {Err}", err));
```

**Example — excluding non-retryable errors**

```csharp
// ContentFiltered is not retryable — retrying the same prompt won't help
var result = await RetryResult<string>
    .ExecuteAsync(
        maxAttempts: 5,
        action: () => _llm.CompleteAsync(prompt),
        shouldRetry: err => err is AiError.RateLimit
                         or AiError.Timeout
                         or AiError.InvalidResponse);
```

---

## WithDelay

```csharp
public RetryResult<T> WithDelay(TimeSpan delay)
```

Adds a fixed wait between retry attempts. The delay is applied after each failed attempt before the next call.

**Parameters**

| Parameter | Type | Description |
|---|---|---|
| `delay` | `TimeSpan` | Fixed duration to wait between each retry. |

**Example**

```csharp
var result = await RetryResult<string>
    .ExecuteAsync(
        maxAttempts: 3,
        action: () => _llm.CompleteAsync(prompt),
        shouldRetry: err => err is AiError.RateLimit or AiError.Timeout)
    .WithDelay(TimeSpan.FromSeconds(2));
```

When the error is `AiError.RateLimit`, prefer using `RetryAfter` from the error itself:

```csharp
shouldRetry: err =>
{
    if (err is AiError.RateLimit rl)
    {
        Thread.Sleep(rl.RetryAfter); // or use WithDelay dynamically
        return true;
    }
    return err is AiError.Timeout;
}
```

---

## WithJitter

```csharp
public RetryResult<T> WithJitter(TimeSpan maxJitter)
```

Adds random jitter (0 to `maxJitter`) on top of any fixed delay. Jitter reduces thundering-herd problems when multiple clients retry simultaneously.

**Parameters**

| Parameter | Type | Description |
|---|---|---|
| `maxJitter` | `TimeSpan` | Upper bound of the random offset added to each delay. |

**Example**

```csharp
var result = await RetryResult<string>
    .ExecuteAsync(
        maxAttempts: 4,
        action: () => _llm.CompleteAsync(prompt),
        shouldRetry: err => err is AiError.RateLimit or AiError.Timeout)
    .WithDelay(TimeSpan.FromSeconds(1))
    .WithJitter(TimeSpan.FromMilliseconds(400));
// Actual delay per retry: 1000ms + random(0..400ms)
```

---

## Composing with the rest of the pipeline

`RetryResult<T>.ExecuteAsync` returns `Task<Result<T>>`, which composes directly with `BindAsync` and `MapAsync`:

```csharp
var result = await RetryResult<string>
    .ExecuteAsync(
        maxAttempts: 3,
        action: () => _llm.CompleteAsync(prompt),
        shouldRetry: err => err is AiError.RateLimit or AiError.Timeout)
    .WithDelay(TimeSpan.FromSeconds(1))
    .BindAsync(raw  => ValidatedResult<WeatherReport>.ParseAndValidateAsync(raw))
    .MapAsync(report => report with { FetchedAt = DateTime.UtcNow });
```

If any step returns `Fail`, the remaining steps are skipped and the error propagates to the final `Match`.

---

## Comparison with Polly

| | `RetryResult<T>` | Polly |
|---|---|---|
| Error type | `AiError` — typed, matchable | `Exception` — catch by type or message |
| Retry condition | `Func<AiError, bool>` — inline, close to the call | `Handle<TException>` — defined separately from use |
| Return type | `Result<T>` — composes with `BindAsync` | `T` — must wrap in try/catch to compose |
| Exhaustion | Returns `Result.Fail(lastError)` | Throws `BrokenCircuitException` or rethrows |
| Pipeline integration | Native — no adapter needed | Requires wrapping async methods |

Use `RetryResult<T>` when your pipeline already uses `Result<T>`. Use Polly when you need circuit breakers, hedging, or other advanced resilience patterns not covered by MonadicSharp.AI.
