# MonadicSharp.Aspire

Aspire 13 integration for MonadicSharp: circuit-breaker health checks, Green Score
OpenTelemetry metrics, and pipeline trace correlation.

```bash
dotnet add package MonadicSharp.Aspire
```

## Setup (one line)

```csharp
// Program.cs
var circuitBreaker = builder.Services.AddMonadicSharpHealthChecks(
    pipelineName: "LlmSummarise",
    configure: opts =>
    {
        opts.FailureThreshold  = 0.4;  // open circuit at 40% failure
        opts.DegradedThreshold = 0.2;  // degrade at 20%
        opts.WindowSize        = 30;   // last 30 observations
    });

// Wire into pipeline side effects (zero-cost when healthy)
app.MapPost("/summarise", async (Request req, SummariseService svc) =>
    (await svc.SummariseAsync(req.Text))
        .Do(_    => circuitBreaker.Record(true))
        .DoError(_ => circuitBreaker.Record(false))
        .ToOkResult());
```

## Health check output

The circuit breaker appears in the Aspire dashboard and `/health` endpoint:

```json
{
  "status": "Degraded",
  "results": {
    "LlmSummarise": {
      "status": "Degraded",
      "description": "Circuit degraded: 23.3% failure rate.",
      "data": { "failureRate": "23.3%", "samples": 30, "threshold": "40%" }
    }
  }
}
```

## Green Score telemetry

```csharp
// Register standalone meter (or via AddMonadicSharpHealthChecks which includes it)
builder.Services.AddMonadicSharpTelemetry();

// Inject and use
public class MyService(GreenScoreMeter meter) { ... }

// Record outcomes
meter.RecordSuccess("LlmSummarise", durationMs: 142.5);
meter.RecordFailure("LlmSummarise", ErrorType.Failure, durationMs: 5200.0);
meter.RecordRetry("LlmSummarise", attempt: 2);

// Compute snapshot score
double score = GreenScoreMeter.ComputeGreenScore(successes: 95, failures: 5, retries: 8);
// → 87.6 (higher = healthier)
```

OpenTelemetry instruments published:
- `monadicsharp.pipeline.success` (Counter)
- `monadicsharp.pipeline.failure` (Counter, tagged with `error_type`)
- `monadicsharp.pipeline.retries` (Counter, tagged with `attempt`)
- `monadicsharp.pipeline.duration` (Histogram, ms)
