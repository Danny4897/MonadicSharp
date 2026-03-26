# AiError

`AiError` is the discriminated union that represents all typed LLM failures in MonadicSharp.AI. Every operation that can fail returns `Result<T>` with an `AiError` in the failure case — never throws.

```csharp
using MonadicSharp.AI;
```

## Cases

### `AiError.RateLimit`

The LLM provider returned a rate-limit response (HTTP 429 or equivalent).

**Constructor**

```csharp
AiError.RateLimit(TimeSpan retryAfter)
```

**Properties**

| Property | Type | Description |
|---|---|---|
| `RetryAfter` | `TimeSpan` | How long to wait before retrying, as reported by the provider. |

**When raised**: the provider explicitly signals that the request quota has been exceeded for the current window.

**Example**

```csharp
var result = await _llm.CompleteAsync(prompt);

result.Match(
    onSuccess: text => Process(text),
    onFailure: err  =>
    {
        if (err is AiError.RateLimit rl)
            logger.LogWarning("Rate limited. Retry after {Seconds}s", rl.RetryAfter.TotalSeconds);
    });
```

With `Bind`:

```csharp
await result.BindAsync(text =>
    text.Length > 0
        ? Result.Ok(text)
        : Result.Fail(AiError.RateLimit(TimeSpan.FromSeconds(60))));
```

---

### `AiError.Timeout`

The request did not complete within the configured deadline.

**Constructor**

```csharp
AiError.Timeout(TimeSpan elapsed)
```

**Properties**

| Property | Type | Description |
|---|---|---|
| `Elapsed` | `TimeSpan` | How long the request ran before being cancelled. |

**When raised**: the HTTP client or cancellation token fires before the provider responds.

**Example**

```csharp
result.Match(
    onSuccess: text => Process(text),
    onFailure: err  =>
    {
        if (err is AiError.Timeout t)
            metrics.RecordTimeout(t.Elapsed);
    });
```

---

### `AiError.TokenExhausted`

The request consumed the maximum allowed tokens for the model or the account plan.

**Constructor**

```csharp
AiError.TokenExhausted(int tokensUsed)
```

**Properties**

| Property | Type | Description |
|---|---|---|
| `TokensUsed` | `int` | The number of tokens that were consumed before exhaustion. |

**When raised**: the response was truncated or rejected because the token limit was reached mid-generation.

**Example**

```csharp
result.Match(
    onSuccess: text => Process(text),
    onFailure: err  =>
    {
        if (err is AiError.TokenExhausted te)
        {
            logger.LogError("Token budget exceeded: {TokensUsed} used", te.TokensUsed);
            // Reduce prompt length and retry
        }
    });
```

---

### `AiError.ContentFiltered`

The provider's safety system blocked the request or the response.

**Constructor**

```csharp
AiError.ContentFiltered(string reason)
```

**Properties**

| Property | Type | Description |
|---|---|---|
| `Reason` | `string` | The policy category or message returned by the provider's filter. |

**When raised**: the input prompt or the generated output triggered the provider's content moderation policy.

**Example**

```csharp
result.Match(
    onSuccess: text => Process(text),
    onFailure: err  =>
    {
        if (err is AiError.ContentFiltered cf)
        {
            logger.LogWarning("Content filtered: {Reason}", cf.Reason);
            // Return a safe fallback to the user
        }
    });
```

Note: `ContentFiltered` should **not** be retried. Use `shouldRetry` in `RetryResult<T>` to exclude it:

```csharp
RetryResult<string>.ExecuteAsync(
    maxAttempts: 3,
    action: () => _llm.CompleteAsync(prompt),
    shouldRetry: err => err is AiError.RateLimit or AiError.Timeout
    // AiError.ContentFiltered is excluded — retrying won't change the outcome
)
```

---

### `AiError.InvalidResponse`

The provider returned a response that could not be parsed as the expected type.

**Constructor**

```csharp
AiError.InvalidResponse(string raw)
```

**Properties**

| Property | Type | Description |
|---|---|---|
| `Raw` | `string` | The unparsed response body from the provider. |

**When raised**: JSON deserialization fails, the schema does not match, or the response body is empty when content was expected.

**Example**

```csharp
result.Match(
    onSuccess: report => Display(report),
    onFailure: err    =>
    {
        if (err is AiError.InvalidResponse ir)
        {
            logger.LogDebug("Bad LLM output (first 200 chars): {Raw}", ir.Raw[..Math.Min(200, ir.Raw.Length)]);
            // Optionally feed ir.Raw back into a correction prompt
        }
    });
```

---

## Exhaustive Match

Use the `Match` overload with all five cases to get compile-time coverage:

```csharp
var message = err.Match(
    onRateLimit:       e => $"Rate limited — retry after {e.RetryAfter.TotalSeconds:0}s",
    onTimeout:         e => $"Timed out after {e.Elapsed.TotalSeconds:0.0}s",
    onTokenExhausted:  e => $"Token budget exceeded ({e.TokensUsed} tokens used)",
    onContentFiltered: e => $"Blocked by content policy: {e.Reason}",
    onInvalidResponse: e => $"Unparseable response: {e.Raw[..50]}...");
```

## Composing with Bind

`AiError` values propagate automatically through `BindAsync` — you only handle them at the pipeline boundary:

```csharp
var result = await RetryResult<string>
    .ExecuteAsync(maxAttempts: 3, action: () => _llm.CompleteAsync(prompt),
                  shouldRetry: err => err is AiError.RateLimit or AiError.Timeout)
    .BindAsync(raw => ValidatedResult<WeatherReport>.ParseAndValidateAsync(raw));
// If RetryResult exhausts attempts → AiError passes through BindAsync untouched
// If ValidatedResult fails         → AiError.InvalidResponse is produced
```
