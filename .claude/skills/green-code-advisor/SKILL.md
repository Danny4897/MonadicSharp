---
name: green-code-advisor
description: >
  Use this skill when writing, reviewing, or refactoring any C# code that uses MonadicSharp.
  Automatically applies green-code principles: minimize wasted compute, LLM token consumption,
  unnecessary allocations, and retry cycles. Activates on any file touching Result<T>, Option<T>,
  AgentResult, PipelineAsync, WithRetry, Try.ExecuteAsync, or MonadicSharp.* namespaces.
---

# Green Code Advisor — MonadicSharp

This skill enforces **green code** principles across the MonadicSharp ecosystem.
"Green" means: zero wasted compute, zero unnecessary token spend, zero redundant retries.
Every pattern below is derived directly from the MonadicSharp codebase.

---

## Core principle: short-circuit is free compute

`Bind()` stops the chain the instant a failure occurs. This is not just safety — it is
**free compute savings**. Every step after a failure that you prevent from running is
CPU, memory, and potentially LLM tokens saved.

**Design rule:** put the cheapest and most likely-to-fail validations FIRST in every chain.

```csharp
// ✅ GREEN — cheap validation gates expensive operations
return ValidateInput(request)          // pure, no I/O
    .Bind(ValidateBusinessRules)       // pure, no I/O
    .Bind(r => _cache.GetOrSetAsync(   // cache hit → skips DB + LLM
        key: CacheKey(r),
        factory: ct => RunExpensivePipeline(r, ct)))
    .Bind(SaveToDb);                   // only runs if everything above succeeded

// ❌ WASTEFUL — expensive operations run even when basic validation will fail
return await _repo.LoadAsync(request.Id)       // DB hit
    .Bind(entity => _llm.SummarizeAsync(entity)) // LLM tokens spent
    .Bind(_ => ValidateInput(request));          // fails here — all previous work wasted
```

---

## Rule 1 — Never use `Map` where `Bind` is correct

`Map` signals "this transformation cannot fail". Using `Map` for operations that CAN fail
silently swallows errors and may cause downstream retries or corrupt state.

```csharp
// ❌ WRONG — hides a potential failure, may cause silent bad data
.Map(user => JsonSerializer.Deserialize<UserDto>(user.RawJson))

// ✅ CORRECT — failure is explicit, chain stops here if JSON is malformed
.Bind(user => Try.Execute(() => JsonSerializer.Deserialize<UserDto>(user.RawJson)))
```

**Green impact:** a silent `Map` failure that becomes a `null` propagated downstream
can trigger retry loops, corrupt cache entries, or generate LLM calls on bad input.

---

## Rule 2 — Retry only what is retriable; terminal errors stop immediately

`AiError.IsRetriable()` is the source of truth. Never configure `WithRetry` on an
operation that can return terminal errors without checking first. Retrying
`ContentFiltered`, `TokenLimitExceeded`, or `Validation` wastes compute and money.

```csharp
// ❌ WASTEFUL — retries even terminal errors (3x token spend on a content filter)
await Result.TryAsync(() => llm.CompleteAsync(prompt))
    .WithRetry(maxAttempts: 3);

// ✅ GREEN — WithRetry already checks AiError.IsRetriable() internally
// BUT: pre-validate input before entering retry scope to avoid retrying validation errors
return ValidatePrompt(prompt)                              // no retry scope yet
    .BindAsync(p => Result.TryAsync(() => llm.CompleteAsync(p))
        .WithRetry(maxAttempts: 3,
                   initialDelay: TimeSpan.FromSeconds(1),
                   useJitter: true));                      // jitter prevents thundering herd
```

**Green impact:** without jitter, concurrent retries hit the rate limit simultaneously,
triggering more 429s, more retries — a feedback loop. `useJitter: true` is always correct.

---

## Rule 3 — Use `CachingAgentWrapper` before any LLM call in a pipeline

Every identical LLM call that hits the cache instead of the model costs zero tokens.
`CachingAgentWrapper` is transparent — it wraps any `IAgent<TIn, TOut>` without changing
the pipeline signature.

```csharp
// ❌ WASTEFUL — same query hits the LLM every time
var pipeline = AgentPipeline
    .Start("RAG", retrievalAgent)
    .Then(summaryAgent);               // LLM call on every invocation

// ✅ GREEN — identical inputs return cached results at zero token cost
var cachedSummary = new CachingAgentWrapper<RetrievalResult, string>(
    inner: summaryAgent,
    cache: cache,
    policy: new AgentCachePolicy<RetrievalResult, string>
    {
        KeyFactory      = (name, input) => $"{name}:{input.QueryHash}",
        EntryOptions    = CacheEntryOptions.WithTtl(TimeSpan.FromMinutes(15)),
        CacheOnlySuccesses = true      // never cache a failure
    });

var pipeline = AgentPipeline
    .Start("RAG", retrievalAgent)
    .Then(cachedSummary);
```

---

## Rule 4 — Validate LLM output with `ValidatedResult<T>` before downstream processing

Passing unvalidated LLM output downstream silently propagates bad data through the entire
pipeline — potentially causing multiple downstream failures, each with their own retry.
Validate at the boundary, once.

```csharp
// ❌ WASTEFUL — bad JSON propagates through 3 more steps before failing
var result = await llm.CompleteAsync(prompt)
    .Map(r => JsonSerializer.Deserialize<OrderDto>(r))  // silent null on bad JSON
    .Bind(SaveOrder)                                     // DB write with null
    .Bind(NotifyCustomer)                                // notification sent
    .Bind(UpdateInventory);                              // inventory corrupted

// ✅ GREEN — validate at the LLM boundary, chain stops immediately on bad output
var result = await Result.TryAsync(() => llm.CompleteAsync(prompt))
    .WithRetry(3)
    .ParseAs<OrderDto>()
    .Validate(o => o.Total > 0,        "Total must be positive")
    .Validate(o => o.CustomerId != 0,  "CustomerId is required")
    .AsResultAsync()
    .Bind(SaveOrder)
    .Bind(NotifyCustomer)
    .Bind(UpdateInventory);
```

---

## Rule 5 — Use `StreamResult` for long completions; avoid buffering full responses

Buffering a full LLM completion before processing wastes memory proportional to response
length and delays time-to-first-token for the user. Use `StreamResult` when the output
is long or when the caller can act on partial results.

```csharp
// ❌ WASTEFUL — buffers entire response in memory before doing anything
var fullText = await llm.CompleteAsync(longPrompt);
Console.WriteLine(fullText);

// ✅ GREEN — processes tokens as they arrive, no full buffer needed
var result = await llm.StreamAsync(prompt)
    .ToStreamResult()
    .OnToken(token => Console.Write(token))   // streams to output immediately
    .CollectAsync();

result.Match(
    onSuccess: full  => SaveSummary(full),
    onFailure: error => LogError(error));
```

---

## Rule 6 — Use `Try.ExecuteAsync` at I/O boundaries, never `try/catch` inside a chain

A `try/catch` inside a `Bind` lambda breaks the railway: the exception exits the monadic
context and propagates as a real exception, bypassing all downstream error handling.

```csharp
// ❌ BREAKS THE RAILWAY — exception escapes Result<T> context
.Bind(async order => {
    try { return Result<Unit>.Success(await _db.SaveAsync(order)); }
    catch (Exception ex) { return Result<Unit>.Failure(Error.FromException(ex)); }
})

// ✅ CORRECT — Try.ExecuteAsync wraps the exception into Result<T>
.Bind(order => Try.ExecuteAsync(() => _db.SaveAsync(order)))
```

---

## Rule 7 — `AgentCapability` as a security AND compute gate

`AgentCapability` is not just security — it is a compute gate. An agent that lacks
`AgentCapability.CallExternalApi` will fail fast at the capability check,
before any HTTP call, token spend, or DB write occurs.

```csharp
// In pipeline setup — grant only what each agent actually needs
var context = AgentContext.Create(
    sessionId: Guid.NewGuid().ToString(),
    grantedCapabilities:
        AgentCapability.ReadDatabase |      // retrieval agent needs this
        AgentCapability.CallExternalApi);   // summary agent calls LLM

// ❌ WASTEFUL — over-granting lets agents run steps they shouldn't
var context = AgentContext.Create(sessionId, AgentCapability.All);
```

**Green impact:** over-granting capabilities allows agents to make expensive calls
(DB reads, LLM completions) that the business logic never actually needed.

---

## Rule 8 — `CircuitBreaker` prevents cascading token waste

When a downstream service (LLM, DB, external API) is degraded, a missing circuit breaker
means every request in the pipeline spends tokens/compute up to the failing step before
failing. The `CircuitBreaker` in `MonadicSharp.Agents` short-circuits at the first call.

```csharp
// ✅ GREEN — pipeline short-circuits at circuit check, zero downstream spend
var pipeline = AgentPipeline
    .Start("Ingest", ingestAgent)
    .Then(enrichmentAgent.WithCircuitBreaker(
        maxFailures:    5,
        openDuration:   TimeSpan.FromSeconds(30)))
    .Then(summaryAgent);   // never called while circuit is open
```

---

## Rule 9 — `Partition()` for batch operations; avoid fail-fast on collections

When processing a batch, failing the entire batch on the first error wastes work already
done and triggers full retries. Use `Partition()` to collect successes and failures separately.

```csharp
// ❌ WASTEFUL — first failure stops all 99 remaining items
var result = await items
    .Select(ProcessItem)
    .Sequence();           // fails on first error

// ✅ GREEN — process all items, segregate results, retry only failures
var (successes, failures) = items
    .Select(ProcessItem)
    .Partition();

await SaveBatch(successes);
await RetryQueue.Enqueue(failures);   // retry only what actually failed
```

---

## Rule 10 — Token budget awareness in prompt construction

When constructing prompts inside a `Bind` chain, validate token budget BEFORE the LLM call.
`AiError.TokenLimitExceeded` is terminal (not retriable) — failing after token spend is
worse than failing before.

```csharp
// ✅ GREEN — estimate tokens before spending them
return BuildPrompt(context)
    .Bind(prompt => prompt.EstimatedTokens > MaxTokenBudget
        ? Result<string>.Failure(
            AiError.TokenLimitExceeded(prompt.EstimatedTokens, MaxTokenBudget))
        : Result<string>.Success(prompt))
    .BindAsync(prompt => Result.TryAsync(() => llm.CompleteAsync(prompt))
        .WithRetry(3));
```

---

## Green code review checklist

When reviewing any MonadicSharp file, flag the following automatically:

| # | Pattern to flag | Suggested fix |
|---|----------------|---------------|
| 1 | `Map` on a call that throws or returns null | Replace with `Bind` + `Try.Execute` |
| 2 | `WithRetry` wrapping validation logic | Move validation outside retry scope |
| 3 | LLM call without `CachingAgentWrapper` in a repeated path | Add `CachingAgentWrapper` |
| 4 | LLM output used without `ParseAs` / `Validate` | Add `ValidatedResult<T>` chain |
| 5 | `try/catch` inside a `Bind` lambda | Replace with `Try.ExecuteAsync` |
| 6 | `AgentCapability.All` in production context | Grant minimum required capabilities |
| 7 | No `CircuitBreaker` on external service agents | Add `.WithCircuitBreaker(...)` |
| 8 | `.Sequence()` on large collections | Replace with `.Partition()` |
| 9 | Expensive operation before cheap validation | Reorder: cheap gates first |
| 10 | `useJitter: false` on `WithRetry` | Always use `useJitter: true` |

---

## Where this skill fits in the pipeline

```
Request enters
     │
     ▼
[cheap validation — Bind]        ← free compute, stops bad input early
     │ success
     ▼
[cache check — GetOrSetAsync]    ← zero token cost on hit
     │ miss
     ▼
[capability gate — AgentContext] ← fails fast if agent lacks permission
     │ granted
     ▼
[circuit breaker — CircuitBreaker] ← fails fast if service is down
     │ closed
     ▼
[token budget check — Bind]      ← validates before spending tokens
     │ within budget
     ▼
[LLM call — TryAsync + WithRetry(useJitter:true)] ← retries only retriable errors
     │ success
     ▼
[output validation — ValidatedResult<T>] ← validates at boundary, once
     │ valid
     ▼
[downstream steps — Bind chain]  ← only run on valid, successful data
     │
     ▼
[Match — onSuccess / onFailure]  ← single exit point
```

Every step in this order is a green-code decision. Reordering or skipping any step
moves compute waste from "zero" to "possible".
