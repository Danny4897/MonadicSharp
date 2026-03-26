# AgentResult\<T\>

`AgentResult<T>` models a multi-step LLM pipeline where each step is named, traced, and contributes to a running token count. The pipeline short-circuits on the first failing step. After completion, `Steps` and `TotalTokens` give you a full audit trail without manual instrumentation.

```csharp
using MonadicSharp.AI;
```

## StartAsync

```csharp
public static AgentResult<T> StartAsync(
    string stepName,
    Func<Task<Result<T>>> action)

// Overload accepting an initial value instead of a factory
public static AgentResult<T> StartAsync(
    string stepName,
    T initialValue)
```

**Parameters**

| Parameter | Type | Description |
|---|---|---|
| `stepName` | `string` | A label for this step, recorded in `Steps`. |
| `action` | `Func<Task<Result<T>>>` | The async factory for the first step. |
| `initialValue` | `T` | Shorthand when the initial value is already available. |

**Returns**: `AgentResult<T>` — a builder that accumulates steps.

---

## StepAsync

```csharp
public AgentResult<TNext> StepAsync<TNext>(
    string stepName,
    Func<T, Task<Result<TNext>>> action)
```

**Parameters**

| Parameter | Type | Description |
|---|---|---|
| `stepName` | `string` | A label for this step, recorded in `Steps`. |
| `action` | `Func<T, Task<Result<TNext>>>` | Receives the previous step's success value. Returns `Result<TNext>`. |

If the previous step failed, `action` is not called. The pipeline carries the existing failure forward.

---

## Properties

| Property | Type | Description |
|---|---|---|
| `Steps` | `IReadOnlyList<AgentStep>` | Ordered list of all steps that were executed. |
| `TotalTokens` | `int` | Sum of `TokensUsed` across all steps. |
| `Result` | `Result<T>` | The final outcome of the pipeline. |

### AgentStep

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | The label passed to `StartAsync` or `StepAsync`. |
| `Succeeded` | `bool` | `true` if this step returned `Ok`. |
| `TokensUsed` | `int` | Tokens consumed by this step (0 if not applicable). |
| `Error` | `AiError?` | The error produced by this step, if it failed. |

---

## Complete Example

```csharp
public record DocumentSummary(string Title, string Summary, string[] Keywords);

public async Task<Result<DocumentSummary>> SummarizeDocumentAsync(string docId)
{
    var traced = await AgentResult<string>
        .StartAsync("fetch", () => _storage.FetchDocumentAsync(docId))
        .StepAsync("clean",    text     => _preprocessor.CleanAsync(text))
        .StepAsync("summarize", cleaned => _llm.SummarizeAsync(cleaned))
        .StepAsync("validate",  raw     => ValidatedResult<DocumentSummary>
                                               .ParseAndValidateAsync(raw));

    // Log the trace regardless of outcome
    foreach (var step in traced.Steps)
    {
        logger.LogDebug(
            "Step [{Name}] {Status} — {Tokens} tokens",
            step.Name,
            step.Succeeded ? "OK" : "FAILED",
            step.TokensUsed);
    }

    logger.LogInformation(
        "Pipeline finished. Total tokens: {Total}",
        traced.TotalTokens);

    return traced.Result;
}
```

Calling code:

```csharp
var result = await SummarizeDocumentAsync("doc-42");

result.Match(
    onSuccess: summary => Console.WriteLine($"{summary.Title}: {summary.Summary}"),
    onFailure: err     => logger.LogError("Summarization failed: {Err}", err));
```

---

## Step Short-Circuit Behaviour

```csharp
var traced = await AgentResult<string>
    .StartAsync("fetch",     () => FetchAsync(id))   // returns Fail(AiError.Timeout)
    .StepAsync("summarize",  text => SummarizeAsync(text))  // SKIPPED
    .StepAsync("validate",   raw  => ValidateAsync(raw));   // SKIPPED

// traced.Steps contains only the "fetch" step
// traced.Steps[0].Succeeded == false
// traced.Steps[0].Error     == AiError.Timeout(...)
// traced.Result              == Result.Fail(AiError.Timeout)
```

---

## Token Attribution

Each step that interacts with an LLM reports its token usage. Steps that do not call an LLM (e.g., fetch from storage, in-process validation) contribute 0 tokens. `TotalTokens` is accurate for cost attribution as long as each LLM step reports its usage:

```csharp
// Steps: fetch(0) + summarize(812) + validate(0) = 812 total tokens
Console.WriteLine($"Cost: ${traced.TotalTokens * 0.000002:F4}");
```

---

## Combining with RetryResult

Each step can itself be a retry pipeline:

```csharp
var traced = await AgentResult<string>
    .StartAsync("fetch", () => _storage.FetchDocumentAsync(docId))
    .StepAsync("summarize-with-retry", text =>
        RetryResult<string>
            .ExecuteAsync(
                maxAttempts: 3,
                action: () => _llm.SummarizeAsync(text),
                shouldRetry: err => err is AiError.RateLimit or AiError.Timeout)
            .WithDelay(TimeSpan.FromSeconds(1)))
    .StepAsync("validate", raw => ValidatedResult<DocumentSummary>.ParseAndValidateAsync(raw));
```
