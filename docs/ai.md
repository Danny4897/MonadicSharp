# MonadicSharp.AI

`MonadicSharp.AI` extends the core library with typed errors, retry primitives, and agent tracing purpose-built for LLM-powered applications.

```bash
dotnet add package MonadicSharp.AI
```

## Why typed LLM errors

LLM APIs fail in distinct, predictable ways — each requiring a different response strategy:

| Error | Cause | Strategy |
|-------|-------|----------|
| `RateLimit` | 429 Too Many Requests | Exponential backoff + retry |
| `ModelTimeout` | Request exceeded timeout | Retry with longer deadline |
| `InvalidOutput` | Response failed schema validation | Self-healing prompt + retry |
| `TokenLimitExceeded` | Prompt + completion > context window | Truncate or abort (terminal) |
| `Unavailable` | Upstream provider outage | Fallback model or abort |

Without typed errors, all of these surface as `HttpRequestException` or raw `Exception` — impossible to handle differently in a pipeline.

## AiError factory methods

```csharp
using MonadicSharp.AI;

AiError.RateLimit()                        // retriable, backoff
AiError.ModelTimeout()                     // retriable
AiError.InvalidOutput("Expected JSON")     // self-healable
AiError.TokenLimitExceeded()               // terminal
AiError.Unavailable("Provider down")       // terminal
```

Every `AiError` is an `Error` — it composes with `Result<T>` like any other failure:

```csharp
Result<string> completion = await _llm.CompleteAsync(prompt);

return completion.Match(
    onSuccess: text  => text,
    onFailure: error => error.Type switch
    {
        ErrorType.Failure when error.HasCode("RATE_LIMIT")   => "Rate limited",
        ErrorType.Failure when error.HasCode("TOKEN_LIMIT")  => "Context too long",
        _                                                     => error.Message
    });
```

## Retry with exponential backoff

`Result.TryAsync` + `.WithRetry` adds automatic retry in one line:

```csharp
var result = await Result
    .TryAsync(() => _llm.CompleteAsync(prompt))
    .WithRetry(
        maxAttempts:  3,
        initialDelay: TimeSpan.FromSeconds(2));
// Delays: 2s → 4s → 8s (exponential)
```

### Retry with condition

Only retry specific error types:

```csharp
var result = await Result
    .TryAsync(() => _llm.CompleteAsync(prompt))
    .WithRetry(
        maxAttempts:  3,
        initialDelay: TimeSpan.FromSeconds(1),
        shouldRetry:  error => error.HasCode("RATE_LIMIT") || error.HasCode("TIMEOUT"));
```

### Jitter

Add random jitter to prevent thundering herd:

```csharp
var result = await Result
    .TryAsync(() => _llm.CompleteAsync(prompt))
    .WithRetryJitter(
        maxAttempts:   3,
        baseDelay:     TimeSpan.FromSeconds(1),
        jitterPercent: 0.2); // ±20% jitter
```

## AgentResult — multi-step tracing

`AgentResult` wraps a multi-step LLM pipeline with structured execution tracing. Each step records its name, input, output, duration, and success/failure state.

```csharp
using MonadicSharp.AI;

var agentResult = await AgentResult
    .StartTrace("SummarizeAgent", userInput)
    .Step("Retrieve",  query  => _search.FindChunksAsync(query))
    .Step("Generate",  chunks => _llm.CompleteAsync(BuildPrompt(chunks)))
    .Step("Validate",  output => ParseAndValidate(output))
    .ExecuteAsync();
```

### Accessing the trace

```csharp
if (agentResult.IsSuccess)
{
    Console.WriteLine(agentResult.Value);

    foreach (var step in agentResult.Trace.Steps)
        Console.WriteLine($"  {step.Name}: {step.Duration.TotalMs}ms — {step.Status}");
}
else
{
    // Which step failed and why
    var failed = agentResult.Trace.FailedStep;
    Console.WriteLine($"Step '{failed?.Name}' failed: {agentResult.Error.Message}");
}
```

### Conditional steps

```csharp
var result = await AgentResult
    .StartTrace("EnrichAgent", document)
    .Step("Parse",   doc => ParseAsync(doc))
    .StepIf(
        condition: doc => doc.HasImages,
        name:      "AnalyseImages",
        step:      doc => _vision.AnalyseAsync(doc))
    .Step("Index",   doc => _search.IndexAsync(doc))
    .ExecuteAsync();
```

## ValidatedResult

`ValidatedResult<T>` is a specialised Result for LLM output validation — it carries both the raw output and the validated, typed result.

```csharp
ValidatedResult<SummaryDto> validated = await _llm
    .CompleteAsync(prompt)
    .ValidateAs<SummaryDto>(json => JsonSerializer.Deserialize<SummaryDto>(json));

validated.Match(
    onSuccess: dto     => ProcessSummary(dto),
    onFailure: aiError => HandleInvalidOutput(aiError, validated.RawOutput));
```

## Self-healing pipelines

Combine `InvalidOutput` errors with `MonadicSharp.Recovery` for automatic prompt repair:

```csharp
using MonadicSharp.Recovery;

var result = await AgentResult
    .StartTrace("ExtractAgent", document)
    .Step("Extract",  doc => _llm.ExtractAsync<InvoiceDto>(doc))
    .WithRecovery(
        on:     error => error.HasCode("INVALID_OUTPUT"),
        repair: ctx   => _llm.RepairAsync(ctx.RawOutput, ctx.Schema))
    .ExecuteAsync();
```

## Token budget management

Prevent token-limit errors before they occur:

```csharp
var budget = TokenBudget.For(model: "gpt-4o", maxTokens: 8192);

Result<string> prompt = budget
    .Plan()
    .System(systemPrompt)
    .History(conversationHistory, priority: Priority.High)
    .User(userMessage)
    .Build(); // Fails fast with TokenLimitExceeded if budget exceeded
```

## OpenTelemetry integration

Pair with `MonadicSharp.Telemetry` for distributed tracing:

```csharp
// Each AgentResult step becomes an OpenTelemetry span
services.AddMonadicTelemetry(options =>
{
    options.ServiceName  = "MyAiService";
    options.TraceAgents  = true;
    options.TraceRetries = true;
});
```

Spans include step name, duration, token count, model name, and failure reason — without any additional instrumentation code.

## AI Code Assistant files

Drop these files into your repository so Copilot, Cursor, or Claude generate MonadicSharp.AI-first code automatically:

```bash
# GitHub Copilot
curl -o .github/copilot-instructions.md \
  https://raw.githubusercontent.com/Danny4897/MonadicSharp/main/.github/copilot-instructions.md

# Cursor
curl -o .cursorrules \
  https://raw.githubusercontent.com/Danny4897/MonadicSharp/main/.cursorrules
```

→ See [AI-ASSISTANT-SETUP.md](https://github.com/Danny4897/MonadicSharp/blob/main/AI-ASSISTANT-SETUP.md) for the full Claude Code integration guide.

## API reference

| Member | Description |
|--------|-------------|
| `AiError.RateLimit()` | Rate-limited error (retriable) |
| `AiError.ModelTimeout()` | Timeout error (retriable) |
| `AiError.InvalidOutput(message)` | Schema/parse failure (self-healable) |
| `AiError.TokenLimitExceeded()` | Context window exceeded (terminal) |
| `AiError.Unavailable(message)` | Provider unavailable (terminal) |
| `.WithRetry(maxAttempts, initialDelay)` | Exponential backoff on `Task<Result<T>>` |
| `.WithRetryJitter(maxAttempts, baseDelay, jitter)` | Backoff + jitter |
| `AgentResult.StartTrace(name, input)` | Begin a traced pipeline |
| `.Step(name, operation)` | Add a step |
| `.StepIf(condition, name, step)` | Conditional step |
| `.ExecuteAsync()` | Run and return `AgentResult<T>` |
| `AgentResult<T>.Trace` | Full execution trace |
| `AgentResult<T>.Trace.FailedStep` | Which step failed |
| `ValidatedResult<T>` | LLM output + validated type |
| `TokenBudget.For(model, maxTokens)` | Budget builder |
