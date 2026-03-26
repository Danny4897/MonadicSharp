# MonadicSharp.Framework.Agents

`MonadicSharp.Framework.Agents` provides the building blocks for multi-step agent workflows where every step is a typed, composable `Result<T>` operation.

## Install

```bash
dotnet add package MonadicSharp.Framework.Agents
```

## Core types

### `IAgentPipeline`

Entry point for executing a pipeline. Manages step orchestration, retry logic, and circuit breaker state.

```csharp
public interface IAgentPipeline
{
    Task<Result<TOut>> RunAsync<TIn, TOut>(
        TIn input,
        IReadOnlyList<AgentStep> steps,
        PipelineOptions? options = null);
}
```

### `AgentStep<TIn, TOut>`

A single pipeline step. Implement it by overriding `ExecuteAsync`.

```csharp
public abstract class AgentStep<TIn, TOut>
{
    public string Name { get; }
    public abstract Task<Result<TOut>> ExecuteAsync(TIn input, AgentContext context);
}
```

### `AgentContext`

Passed to every step. Contains the cancellation token, run metadata, and a shared property bag for passing data between steps without coupling them.

```csharp
public sealed class AgentContext
{
    public CancellationToken CancellationToken { get; }
    public Guid RunId { get; }
    public IDictionary<string, object> Properties { get; }
}
```

### `PipelineOptions`

Controls retry and timeout behavior at the pipeline level.

```csharp
var options = new PipelineOptions
{
    MaxRetries = 3,
    StepTimeout = TimeSpan.FromSeconds(30),
    RetryDelay = TimeSpan.FromMilliseconds(500),
    CircuitBreakerThreshold = 5
};
```

## Building a pipeline

The following example builds a summarization pipeline with three steps: input validation, LLM call, and output formatting.

### Step definitions

```csharp
public sealed class ValidateInputStep : AgentStep<string, string>
{
    public override string Name => "validate-input";

    public override Task<Result<string>> ExecuteAsync(string input, AgentContext ctx)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Task.FromResult(Result.Fail<string>(
                new AgentError.StepFailed(Name, "Input is empty")));

        if (input.Length > 8_000)
            return Task.FromResult(Result.Fail<string>(
                new AgentError.StepFailed(Name, "Input exceeds 8 000 character limit")));

        return Task.FromResult(Result.Ok(input.Trim()));
    }
}

public sealed class LlmSummarizeStep(ILlmClient llm) : AgentStep<string, string>
{
    public override string Name => "llm-summarize";

    public override async Task<Result<string>> ExecuteAsync(string input, AgentContext ctx)
    {
        var prompt = $"Summarize the following text in three sentences:\n\n{input}";
        return await llm.CompleteAsync(prompt, ctx.CancellationToken);
    }
}

public sealed class FormatOutputStep : AgentStep<string, string>
{
    public override string Name => "format-output";

    public override Task<Result<string>> ExecuteAsync(string input, AgentContext ctx)
    {
        var formatted = input.Trim();
        return Task.FromResult(Result.Ok(formatted));
    }
}
```

### Running the pipeline

```csharp
public class SummarizationService(IAgentPipeline pipeline)
{
    public async Task<Result<string>> SummarizeAsync(string text, CancellationToken ct = default)
    {
        var steps = new AgentStep[]
        {
            new ValidateInputStep(),
            new LlmSummarizeStep(_llm),
            new FormatOutputStep()
        };

        var options = new PipelineOptions
        {
            MaxRetries = 2,
            StepTimeout = TimeSpan.FromSeconds(20)
        };

        return await pipeline.RunAsync<string, string>(text, steps, options);
    }
}
```

## Circuit breaker

The circuit breaker is automatic. It activates after `CircuitBreakerThreshold` consecutive failures across all steps in a given pipeline instance. While open, `RunAsync` returns `AgentError.CircuitOpen` immediately without executing any step.

```csharp
var options = new PipelineOptions
{
    CircuitBreakerThreshold = 5,          // open after 5 consecutive failures
    CircuitBreakerResetAfter = TimeSpan.FromSeconds(60)  // try again after 60s
};
```

To inspect circuit state:

```csharp
if (pipeline.CircuitState == CircuitState.Open)
{
    // return a degraded response or skip processing
}
```

## Error types

| Error | When |
|---|---|
| `AgentError.StepFailed` | A step returned a failure result |
| `AgentError.Timeout` | A step exceeded `StepTimeout` |
| `AgentError.MaxRetriesExceeded` | All retry attempts failed |
| `AgentError.CircuitOpen` | Circuit breaker is open |

## Registration

```csharp
builder.Services.AddAgents(opts =>
{
    opts.DefaultMaxRetries = 3;
    opts.DefaultStepTimeout = TimeSpan.FromSeconds(30);
});
```
