# RescueAsync

`RescueAsync` is an extension method on `Task<Result<T>>` that intercepts a failing result, tests the error against a predicate, and — if the predicate matches — invokes a recovery delegate. It makes a single recovery attempt. If the recovery delegate also fails, the new error propagates.

```csharp
using MonadicSharp.Recovery;
```

## Signature

```csharp
public static Task<Result<T>> RescueAsync<T>(
    this Task<Result<T>> source,
    Func<Error, bool> when,
    Func<Error, Task<Result<T>>> recover,
    IRecoveryTelemetry? telemetry = null)
```

**Parameters**

| Parameter | Type | Description |
|---|---|---|
| `source` | `Task<Result<T>>` | The upstream pipeline step. `RescueAsync` awaits this first. |
| `when` | `Func<Error, bool>` | Predicate that decides whether to attempt recovery. |
| `recover` | `Func<Error, Task<Result<T>>>` | Recovery delegate. Receives the original error. |
| `telemetry` | `IRecoveryTelemetry?` | Optional telemetry hook. Fires on attempt, success, and exhaustion. |

**Returns**: `Task<Result<T>>`

---

## Behaviour

```
source == Ok(value)
    → returns Ok(value) immediately, recover is NOT called

source == Fail(err) AND when(err) == false
    → returns Fail(err) immediately, recover is NOT called

source == Fail(err) AND when(err) == true
    → calls recover(err)
       → recover returns Ok(v)   → returns Ok(v)
       → recover returns Fail(e) → returns Fail(e)   ← new error, not original
```

The original error is available inside `recover` as the parameter. If you need to preserve it alongside the recovery error, capture it in the delegate:

```csharp
.RescueAsync(
    when: err => err is DbError.Timeout,
    recover: originalErr =>
    {
        logger.LogWarning("Primary DB timed out: {Err}", originalErr);
        return _fallbackDb.GetAsync(id);
    })
```

---

## Basic Examples

### Cache miss with fallback

```csharp
var user = await _cache.GetUserAsync(userId)
    .RescueAsync(
        when: err => err is CacheError.Miss,
        recover: _ => _db.GetUserAsync(userId));
```

### Guest user on not-found

```csharp
var user = await _userService.GetAsync(userId)
    .RescueAsync(
        when: err => err is UserError.NotFound,
        recover: _ => CreateGuestUserAsync());
```

### Stale data refresh

```csharp
var data = await _cache.GetAsync(key)
    .RescueAsync(
        when: err => err is CacheError.Stale or CacheError.Miss,
        recover: _ => _origin.FetchAndCacheAsync(key));
```

---

## With Telemetry

```csharp
var telemetry = new MyRecoveryTelemetry(logger);

var result = await _externalService.GetAsync(id)
    .RescueAsync(
        when: err => err is ServiceError.Unavailable,
        recover: _ => _fallbackService.GetAsync(id),
        telemetry: telemetry);
```

Telemetry calls:
- `OnRecoveryAttempt` fires when `when` returns `true`, before calling `recover`.
- `OnRecoverySuccess` fires when `recover` returns `Ok`.
- `OnRecoveryExhausted` fires when `recover` returns `Fail`.

See [IRecoveryTelemetry](./telemetry) for the interface definition and a logging implementation.

---

## Composing Multiple RescueAsync Calls

Multiple `RescueAsync` calls chain in sequence. Each one only fires if the value reaching it is still on the Red track:

```csharp
var result = await _primaryCache.GetAsync(id)
    .RescueAsync(
        when: err => err is CacheError.Miss,
        recover: _ => _secondaryCache.GetAsync(id))
    .RescueAsync(
        when: err => err is CacheError.Miss,
        recover: _ => _db.GetAsync(id));
// primary miss → try secondary → secondary miss → try DB → DB result is final
```

---

## Edge Cases

**`when` throws**: the exception propagates out of `RescueAsync` as a normal .NET exception. Predicates should be pure and non-throwing.

**`recover` returns a different error type**: the downstream pipeline sees the new error. The original is not preserved unless you capture it inside the delegate.

**`source` is already on the Red track from a previous step**: `when` is still evaluated. If it matches, `recover` is called with that error. Use specific predicates to avoid unintended recovery of unrelated errors:

```csharp
// Too broad — catches any failure including unrelated infrastructure errors
.RescueAsync(when: _ => true, recover: _ => FallbackAsync())

// Correct — only catches the specific case you know how to handle
.RescueAsync(when: err => err is UserError.NotFound, recover: _ => GuestAsync())
```

**`source` never completes**: `RescueAsync` awaits `source` and will not advance until it does. If the upstream pipeline hangs, `RescueAsync` hangs. Use `CancellationToken` in the upstream call rather than inside `recover`.

---

## Difference from StartFixBranchAsync

| | `RescueAsync` | `StartFixBranchAsync` |
|---|---|---|
| Recovery attempts | 1 | Up to `maxAttempts` |
| Delay between attempts | No | Yes (`delay`, optional backoff) |
| Best for | Known alternative path | Transient failures worth retrying |
| Exhaustion result | `recover`'s error | Last attempt's error |
