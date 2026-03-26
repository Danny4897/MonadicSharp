# MonadicSharp.Framework.Telemetry

`MonadicSharp.Framework.Telemetry` integrates the framework with [OpenTelemetry](https://opentelemetry.io/). It creates spans automatically for every pipeline step and propagates structured error data into trace attributes without any manual instrumentation.

## Install

```bash
dotnet add package MonadicSharp.Framework.Telemetry
```

## Setup

### Register with OpenTelemetry

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddMonadicFrameworkInstrumentation()
        .AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri("http://localhost:4317");
        }));
```

`AddMonadicFrameworkInstrumentation()` hooks into all `IAgentPipeline`, `IMonadicHttpClient`, and `IMonadicRepository` calls.

### Registration

```csharp
builder.Services.AddTelemetry(opts =>
{
    opts.ServiceName = "my-agent-service";
    opts.ServiceVersion = "1.0.0";
});
```

## Automatic spans

Each pipeline step produces a child span under a root `agent.run` span. The span name is `agent.step:{step-name}`.

### Span attributes

| Attribute | Type | Value |
|---|---|---|
| `step.name` | string | Value of `AgentStep.Name` |
| `step.status` | string | `ok` or `error` |
| `step.error.type` | string | Error type name (e.g. `AgentError.Timeout`) |
| `step.error.message` | string | Error message |
| `step.duration_ms` | int | Step wall-clock duration |
| `agent.run_id` | string | `AgentContext.RunId` as UUID |
| `agent.retry_count` | int | Retry attempt number |

When a step returns a failure result, the span status is set to `Error` and `step.error.type` / `step.error.message` are populated automatically.

## Example: exporting to Jaeger

Start Jaeger locally:

```bash
docker run -d -p 16686:16686 -p 4317:4317 jaegertracing/all-in-one
```

Configure the exporter:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddMonadicFrameworkInstrumentation()
        .AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri("http://localhost:4317");
            otlp.Protocol = OtlpExportProtocol.Grpc;
        }));
```

Open `http://localhost:16686` and search for traces by service name.

## Adding custom attributes

Override `OnStepCompleted` in a custom `ITelemetryEnricher` to add attributes that are specific to your domain.

```csharp
public sealed class UserContextEnricher : ITelemetryEnricher
{
    public void Enrich(Activity span, AgentContext context)
    {
        if (context.Properties.TryGetValue("userId", out var userId))
            span.SetTag("user.id", userId?.ToString());

        if (context.Properties.TryGetValue("tenantId", out var tenantId))
            span.SetTag("tenant.id", tenantId?.ToString());
    }
}
```

Register the enricher:

```csharp
builder.Services.AddTelemetry(opts =>
{
    opts.ServiceName = "my-agent-service";
})
.AddEnricher<UserContextEnricher>();
```

The enricher is called after each step completes, on both success and failure.

## Metrics

In addition to traces, `AddMonadicFrameworkInstrumentation()` registers the following meters:

| Metric | Type | Description |
|---|---|---|
| `monadic.pipeline.runs` | Counter | Total pipeline executions |
| `monadic.pipeline.failures` | Counter | Total pipeline failures |
| `monadic.step.duration` | Histogram | Step duration in milliseconds |
| `monadic.circuit_breaker.open` | ObservableGauge | 1 when circuit is open, 0 otherwise |

Export metrics via OTLP alongside traces with no additional configuration.
