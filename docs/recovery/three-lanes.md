# The Three Lanes

MonadicSharp.Recovery organises error handling around three execution tracks. Understanding which track a value is on at any point tells you exactly what happened to it and what can happen next.

## The Model

```
┌─────────────────────────────────────────────────────────────────┐
│                         INPUT                                   │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│  GREEN TRACK — success path                                     │
│  Result<T> = Ok(value)                                          │
│  Pipeline continues normally via Map / Bind                     │
└──────────┬──────────────────────────────────────────────────────┘
           │ error matches `when` predicate
           ▼
┌─────────────────────────────────────────────────────────────────┐
│  AMBER TRACK — recovery in progress                             │
│  RescueAsync    → one attempt to recover                        │
│  StartFixBranchAsync → up to maxAttempts attempts               │
└──────────┬────────────────────────────┬─────────────────────────┘
           │ recovery succeeds          │ recovery fails / exhausted
           ▼                            ▼
┌──────────────────────┐   ┌────────────────────────────────────┐
│  Back to GREEN TRACK │   │  RED TRACK — terminal failure      │
│  Ok(recoveredValue)  │   │  Result<T> = Fail(error)           │
│  Pipeline resumes    │   │  No further recovery attempted     │
└──────────────────────┘   └────────────────────────────────────┘
```

A value that reaches the Red track does not trigger further recovery operators downstream — it passes through `RescueAsync` and `StartFixBranchAsync` unchanged, exactly like an unmatched error in `BindAsync`.

---

## Green Track

The normal execution path. As long as each step returns `Ok`, the pipeline stays on the Green track and no recovery logic runs.

```csharp
var result = await FetchUserAsync(userId)   // Ok(user) → stays Green
    .BindAsync(user => EnrichAsync(user))   // Ok(enriched) → stays Green
    .MapAsync(user => user with { Loaded = true });
```

---

## Amber Track

A value enters the Amber track when a step returns `Fail` and the error matches the `when` predicate of a `RescueAsync` or `StartFixBranchAsync` call.

The original error is passed to the `recover` (or `fix`) delegate. The delegate produces a new `Result<T>`.

The track the value ends up on depends on whether the recovery delegate succeeds:

- Recovery returns `Ok` → value moves back to Green.
- Recovery returns `Fail` → value moves to Red.

### When to use RescueAsync

Use `RescueAsync` for single-attempt recovery where there is a meaningful alternative that either works or doesn't. The alternative itself should not be retried.

Typical cases:

- **Cache miss** — primary fetch fails, fall back to a slower authoritative source.
- **Guest user** — user record not found, return an anonymous guest profile.
- **Stale data** — cached value is expired, refresh from origin.

```csharp
// Cache miss → authoritative fetch
var user = await _cache.GetUserAsync(userId)
    .RescueAsync(
        when: err => err is CacheError.Miss,
        recover: _ => _db.GetUserAsync(userId));
```

```csharp
// User not found → guest profile
var user = await _userService.GetAsync(userId)
    .RescueAsync(
        when: err => err is UserError.NotFound,
        recover: _ => CreateGuestUserAsync());
```

### When to use StartFixBranchAsync

Use `StartFixBranchAsync` when the recovery itself may fail transiently and is worth repeating with a delay. The "fix" branch is the same action retried up to `maxAttempts` times.

Typical cases:

- **API rate limit** — the external call was throttled; wait and retry.
- **Intermittent timeout** — the downstream service is slow but recoverable.
- **Transient dependency failure** — a dependency returns 503, retry with backoff.

```csharp
// API timeout → retry up to 3 times with backoff
var response = await CallExternalApiAsync(request)
    .StartFixBranchAsync(
        when: err => err is ApiError.Timeout or ApiError.RateLimit,
        maxAttempts: 3,
        delay: TimeSpan.FromSeconds(2),
        fix: _ => CallExternalApiAsync(request));
```

---

## Red Track

A value is on the Red track when it holds a `Fail` that either:

- Did not match any `when` predicate in a recovery operator.
- Came out of a recovery delegate that also failed.
- Was produced by `StartFixBranchAsync` after exhausting all attempts.

Red-track values are inert. They pass through `Map`, `Bind`, `RescueAsync`, and `StartFixBranchAsync` without executing any delegate. They surface at the first `Match` call.

```csharp
var result = await FetchUserAsync(userId)
    // If UserError.NotFound → Amber → guest recovery
    .RescueAsync(
        when: err => err is UserError.NotFound,
        recover: _ => CreateGuestUserAsync())
    // If FetchUserAsync fails with a database error → Red track
    // The RescueAsync above does not match it → passes through
    .BindAsync(user => BuildProfileAsync(user));
    // BindAsync is also skipped if value is on Red track

result.Match(
    onSuccess: profile => Display(profile),
    onFailure: err     => logger.LogError("Could not build profile: {Err}", err));
```

---

## Combining Both Operators

The two operators compose in sequence. A value can enter recovery, return to Green, and then enter a second recovery if a later step fails:

```csharp
var report = await _cache.GetReportAsync(id)
    // First recovery: cache miss → fetch from DB
    .RescueAsync(
        when: err => err is CacheError.Miss,
        recover: _ => _db.GetReportAsync(id))
    .BindAsync(report => _enricher.EnrichAsync(report))
    // Second recovery: enrichment timeout → retry up to 2 times
    .StartFixBranchAsync(
        when: err => err is EnrichmentError.Timeout,
        maxAttempts: 2,
        delay: TimeSpan.FromSeconds(1),
        fix: _ => _enricher.EnrichAsync(report));
```

---

## Telemetry Across Tracks

Both operators accept an optional `IRecoveryTelemetry` parameter. Telemetry hooks fire at each track transition:

- Amber entry → `OnRecoveryAttempt`
- Green return → `OnRecoverySuccess`
- Red entry (exhausted) → `OnRecoveryExhausted`

This gives you observability on recovery behaviour without adding logging code inside your business logic. See [IRecoveryTelemetry](./api/telemetry) for implementation details.
