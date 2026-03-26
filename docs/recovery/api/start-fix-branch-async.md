# StartFixBranchAsync

`StartFixBranchAsync` is an extension method on `Task<Result<T>>` that intercepts a failing result and retries a fix delegate up to `maxAttempts` times with a configurable delay. If all attempts fail, it returns the last error. If any attempt succeeds, the pipeline resumes on the Green track.

```csharp
using MonadicSharp.Recovery;
```

## Signature

```csharp
public static Task<Result<T>> StartFixBranchAsync<T>(
    this Task<Result<T>> source,
    Func<Error, bool> when,
    int maxAttempts,
    TimeSpan delay,
    Func<Error, Task<Result<T>>> fix,
    BackoffStrategy backoff = BackoffStrategy.Fixed,
    IRecoveryTelemetry? telemetry = null)
```

**Parameters**

| Parameter | Type | Description |
|---|---|---|
| `source` | `Task<Result<T>>` | The upstream pipeline step. Awaited before any recovery logic runs. |
| `when` | `Func<Error, bool>` | Predicate that decides whether to enter the fix branch. |
| `maxAttempts` | `int` | Maximum number of fix attempts. Must be ≥ 1. |
| `delay` | `TimeSpan` | Base wait time between attempts. |
| `fix` | `Func<Error, Task<Result<T>>>` | The fix delegate. Receives the most recent error before each attempt. |
| `backoff` | `BackoffStrategy` | `Fixed` (default) or `Exponential`. Controls how `delay` scales across attempts. |
| `telemetry` | `IRecoveryTelemetry?` | Optional telemetry hook. |

**Returns**: `Task<Result<T>>`

---

## Behaviour

```
source == Ok(value)
    → returns Ok(value) immediately, fix is NOT called

source == Fail(err) AND when(err) == false
    → returns Fail(err) immediately, fix is NOT called

source == Fail(err) AND when(err) == true
    → attempt 1: call fix(err)
         → Ok(v) → return Ok(v)
         → Fail(e1) AND attempt < maxAttempts → wait delay, attempt 2: call fix(e1)
         → Fail(e2) AND attempt < maxAttempts → wait delay, attempt 3: call fix(e2)
         ...
         → Fail(eN) AND attempts exhausted → return Fail(eN)
```

The error passed to `fix` on each attempt is the error from the **previous attempt**, not the original source error. This lets the fix delegate adapt based on the latest failure.

---

## BackoffStrategy

| Value | Behaviour |
|---|---|
| `BackoffStrategy.Fixed` | Every delay is exactly `delay`. |
| `BackoffStrategy.Exponential` | Delay doubles on each attempt: `delay`, `delay × 2`, `delay × 4`, … |

---

## Examples

### Retry on API rate limit or timeout

```csharp
var result = await CallExternalApiAsync(request)
    .StartFixBranchAsync(
        when: err => err is ApiError.RateLimit or ApiError.Timeout,
        maxAttempts: 3,
        delay: TimeSpan.FromSeconds(2),
        fix: _ => CallExternalApiAsync(request));
```

### Exponential backoff

```csharp
var result = await _paymentGateway.ChargeAsync(order)
    .StartFixBranchAsync(
        when: err => err is PaymentError.GatewayUnavailable,
        maxAttempts: 4,
        delay: TimeSpan.FromSeconds(1),
        backoff: BackoffStrategy.Exponential,
        fix: _ => _paymentGateway.ChargeAsync(order));
// Delays: 1s, 2s, 4s — then exhaustion
```

### Adapting the fix based on the latest error

```csharp
var result = await _reportService.GenerateAsync(reportId)
    .StartFixBranchAsync(
        when: err => err is ReportError.Timeout,
        maxAttempts: 3,
        delay: TimeSpan.FromSeconds(3),
        fix: latestErr =>
        {
            // On each retry, use a lighter report variant
            logger.LogWarning("Report attempt failed: {Err} — retrying with reduced scope", latestErr);
            return _reportService.GenerateSummaryAsync(reportId);
        });
```

### With telemetry

```csharp
var telemetry = new MyRecoveryTelemetry(logger);

var result = await FetchInventoryAsync(productId)
    .StartFixBranchAsync(
        when: err => err is InventoryError.ServiceUnavailable,
        maxAttempts: 3,
        delay: TimeSpan.FromSeconds(2),
        fix: _ => FetchInventoryAsync(productId),
        telemetry: telemetry);
```

---

## Exhaustion Behaviour

When all `maxAttempts` are consumed without a successful result, `StartFixBranchAsync` returns `Result.Fail` with the error produced by the **last** fix attempt — not the original source error.

```csharp
// source fails with ApiError.Timeout
// attempt 1: fix fails with ApiError.Timeout
// attempt 2: fix fails with ApiError.ServerError  ← this is the exhaustion error
// attempt 3: fix fails with ApiError.Timeout
// result: Fail(ApiError.Timeout) — last attempt's error
```

If you need to expose exhaustion as a distinct error type, wrap the result downstream:

```csharp
var result = await _service.FetchAsync(id)
    .StartFixBranchAsync(
        when: err => err is ServiceError.Timeout,
        maxAttempts: 3,
        delay: TimeSpan.FromSeconds(1),
        fix: _ => _service.FetchAsync(id));

// Promote exhaustion to a domain error
if (result.IsFailure)
    return Result.Fail(new DomainError.RecoveryExhausted(underlyingError: result.Error));
```

---

## Differences from RescueAsync

| | `RescueAsync` | `StartFixBranchAsync` |
|---|---|---|
| Attempts | 1 | 1 to `maxAttempts` |
| Delay between attempts | None | `delay` (fixed or exponential) |
| Best for | Alternative path (cache fallback, guest user) | Transient failures (timeout, rate limit, 503) |
| Exhaustion result | `recover`'s error | Last `fix` attempt's error |
| Error passed to delegate | Original source error | Most recent attempt's error |

---

## Composing with RescueAsync

The two operators compose freely. Apply `RescueAsync` for alternatives and `StartFixBranchAsync` for retries in the same pipeline:

```csharp
var result = await _primaryDb.GetAsync(id)
    // Single-attempt fallback to replica
    .RescueAsync(
        when: err => err is DbError.ConnectionFailed,
        recover: _ => _replicaDb.GetAsync(id))
    // If replica is also slow, retry up to 2 more times
    .StartFixBranchAsync(
        when: err => err is DbError.Timeout,
        maxAttempts: 2,
        delay: TimeSpan.FromSeconds(1),
        fix: _ => _replicaDb.GetAsync(id));
```
