# Getting Started

AgentScope is a self-hosted AI agent observability platform for .NET. It auto-discovers traces from MonadicSharp.Telemetry, Microsoft.Extensions.AI, and Semantic Kernel.

## Install the NuGet package

```bash
dotnet add package AgentScope
```

## Configure in 2 lines

```csharp
// Program.cs
builder.Services.AddAgentScope();
app.UseAgentScope(); // serves the dashboard at /agentscope
```

That's it. AgentScope auto-discovers all MonadicSharp pipelines in your application.

## Connect OpenTelemetry (optional)

If your application already uses OpenTelemetry, AgentScope integrates automatically:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAgentScopeInstrumentation()  // adds AgentScope as a trace processor
        .AddOtlpExporter());
```

## Deploy standalone (production)

For production environments, run AgentScope as a separate service:

```bash
docker run -p 5000:5000 \
  -e OTLP_ENDPOINT=http://your-app:4317 \
  ghcr.io/danny4897/agentscope:latest
```

Then configure your application to export traces to AgentScope's OTLP endpoint.

## What you see in the dashboard

After your first agent run, navigate to `/agentscope`:

- **Pipeline waterfall** — every step with duration and Result state
- **Token usage** — per step and totals across the run
- **Error details** — structured MonadicSharp errors, not stack traces
- **Circuit breaker states** — live view of all registered breakers

## Next steps

- [Pipeline Tracing](./features/tracing) — understand the waterfall view
- [Metrics dashboard](./features/metrics) — configure metric collection
- [Deploy guide](./deploy) — production deployment options
