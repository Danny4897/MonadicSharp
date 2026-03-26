# Getting Started

MonadicSharp.Framework is a suite of six focused NuGet packages that extend [MonadicSharp](https://danny4897.github.io/MonadicSharp/) with enterprise-grade infrastructure for AI agents running on .NET 8.

## Prerequisites

- .NET 8.0 or higher
- [MonadicSharp](https://www.nuget.org/packages/MonadicSharp) core package

## Install

Install the full framework meta-package, or pick only the packages you need:

```bash
# Full framework
dotnet add package MonadicSharp.Framework

# Or individual packages
dotnet add package MonadicSharp.Framework.Agents
dotnet add package MonadicSharp.Framework.Security
dotnet add package MonadicSharp.Framework.Telemetry
dotnet add package MonadicSharp.Framework.Http
dotnet add package MonadicSharp.Framework.Persistence
dotnet add package MonadicSharp.Framework.Caching
```

## Configure with Dependency Injection

```csharp
using MonadicSharp.Framework;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMonadicFramework()
    .AddAgents()
    .AddSecurity(opts => opts.EnablePromptInjectionGuard = true)
    .AddTelemetry()
    .AddHttpClient()
    .AddPersistence()
    .AddCaching();

var app = builder.Build();
app.Run();
```

## Your First Agent Pipeline

```csharp
public class DocumentSummaryAgent(
    IAgentPipeline pipeline,
    ISecurityGuard guard,
    ICacheService cache)
{
    public async Task<Result<string>> SummarizeAsync(string documentId, string content)
    {
        return await guard.ValidateInputAsync(content)
            .BindAsync(safe => cache.GetOrSetAsync(
                key: $"summary:{documentId}",
                factory: () => pipeline
                    .StartAsync(safe)
                    .BindAsync(CallLlmAsync)
                    .MapAsync(FormatSummary)));
    }

    private Task<Result<string>> CallLlmAsync(string input) =>
        _llmClient.CompleteAsync(input);

    private static string FormatSummary(string raw) =>
        raw.Trim();
}
```

Every step returns `Result<T>`. Security violations, cache errors, and LLM failures all propagate cleanly without a single `try/catch`.

## Next Steps

- [Architecture overview](./architecture) — understand how the packages fit together
- [Agents package](./packages/agents) — build multi-step agent workflows
- [Security package](./packages/security) — protect your pipelines from injection attacks
