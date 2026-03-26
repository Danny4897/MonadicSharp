# Getting Started

[![NuGet](https://img.shields.io/nuget/v/MonadicSharp.AI.svg?style=flat-square)](https://www.nuget.org/packages/MonadicSharp.AI/) [![NuGet Downloads](https://img.shields.io/nuget/dt/MonadicSharp.AI.svg?style=flat-square)](https://www.nuget.org/packages/MonadicSharp.AI/)


MonadicSharp.AI extends [MonadicSharp](https://danny4897.github.io/MonadicSharp/) with five building blocks for working with LLMs — all returning `Result<T>` so they compose cleanly with the rest of your pipeline.

## Install

```bash
dotnet add package MonadicSharp.AI
```

**Requires**: .NET 8.0+, MonadicSharp ≥ 1.5.

## Quick Example

```csharp
using MonadicSharp.AI;

// Call an LLM with retry and structured output validation
var result = await RetryResult<WeatherReport>
    .ExecuteAsync(
        maxAttempts: 3,
        action: () => llmClient.CompleteAsync(prompt),
        shouldRetry: err => err is AiError.RateLimit or AiError.Timeout)
    .BindAsync(raw => ValidatedResult<WeatherReport>.ParseAndValidateAsync(raw))
    .MapAsync(report => report with { FetchedAt = DateTime.UtcNow });

result.Match(
    onSuccess: report => Console.WriteLine($"Temp: {report.Temperature}°C"),
    onFailure: err  => logger.LogError("LLM call failed: {Err}", err));
```

## AiError types

```csharp
// All LLM failures are typed — no stringly-typed exceptions
AiError.RateLimit(retryAfter: TimeSpan.FromSeconds(30))
AiError.Timeout(elapsed: TimeSpan.FromSeconds(10))
AiError.TokenExhausted(tokensUsed: 4096)
AiError.ContentFiltered(reason: "policy violation")
AiError.InvalidResponse(raw: responseBody)
```

## Streaming

```csharp
await foreach (var chunk in StreamResult.FromAsync(llmClient.StreamAsync(prompt)))
{
    chunk.Match(
        onToken: token => Console.Write(token),
        onError: err   => logger.LogWarning("Stream interrupted: {Err}", err));
}
```

## AgentResult — trace your pipeline

```csharp
var traced = await AgentResult<string>
    .StartAsync("summarize", input)
    .StepAsync("validate",   ValidateInputAsync)
    .StepAsync("call-llm",   CallLlmAsync)
    .StepAsync("parse",      ParseOutputAsync);

Console.WriteLine($"Steps: {traced.Steps.Count}, Tokens: {traced.TotalTokens}");
```

## Next steps

- [AiError reference](./api/ai-error)
- [RetryResult reference](./api/retry-result)
- [AgentResult reference](./api/agent-result)
