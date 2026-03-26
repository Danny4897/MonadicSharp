# IRecoveryTelemetry

`IRecoveryTelemetry` is an optional instrumentation hook accepted by both `RescueAsync` and `StartFixBranchAsync`. Implement it to record recovery events in your logging, metrics, or tracing infrastructure without coupling the recovery logic to any specific observability library.

```csharp
using MonadicSharp.Recovery;
```

## Interface Definition

```csharp
public interface IRecoveryTelemetry
{
    void OnRecoveryAttempt(RecoveryContext context);
    void OnRecoverySuccess(RecoveryContext context);
    void OnRecoveryExhausted(RecoveryContext context);
}
```

---

## RecoveryContext

Passed to every telemetry method. Contains the information available at the point of each event.

| Property | Type | Description |
|---|---|---|
| `OperationName` | `string` | Identifier for the pipeline step being recovered. |
| `AttemptNumber` | `int` | Current attempt number (1-based). |
| `MaxAttempts` | `int` | Total attempts allowed (`1` for `RescueAsync`). |
| `Error` | `Error` | The error that triggered or resulted from this attempt. |
| `Elapsed` | `TimeSpan` | Wall time since the first recovery attempt began. |

---

## Methods

### OnRecoveryAttempt

Called before each recovery attempt is made. `context.AttemptNumber` reflects the attempt about to start.

Use this to record that recovery was triggered and log the triggering error:

```csharp
public void OnRecoveryAttempt(RecoveryContext context)
{
    _logger.LogWarning(
        "Recovery attempt {Attempt}/{Max} for [{Operation}]. Triggering error: {Error}",
        context.AttemptNumber,
        context.MaxAttempts,
        context.OperationName,
        context.Error);
}
```

### OnRecoverySuccess

Called when a recovery attempt returns `Ok`. This is the transition from Amber back to Green track.

```csharp
public void OnRecoverySuccess(RecoveryContext context)
{
    _logger.LogInformation(
        "Recovery succeeded for [{Operation}] on attempt {Attempt}/{Max} after {Elapsed}ms",
        context.OperationName,
        context.AttemptNumber,
        context.MaxAttempts,
        context.Elapsed.TotalMilliseconds);

    _metrics.RecordRecoverySuccess(context.OperationName, context.AttemptNumber);
}
```

### OnRecoveryExhausted

Called when all attempts have been consumed without a successful result. This is the transition to the Red track.

```csharp
public void OnRecoveryExhausted(RecoveryContext context)
{
    _logger.LogError(
        "Recovery exhausted for [{Operation}] after {Attempt} attempt(s). Last error: {Error}",
        context.OperationName,
        context.AttemptNumber,
        context.Error);

    _metrics.RecordRecoveryExhausted(context.OperationName, context.AttemptNumber);
}
```

---

## Full Implementation with ILogger

```csharp
using Microsoft.Extensions.Logging;
using MonadicSharp.Recovery;

public sealed class LoggingRecoveryTelemetry : IRecoveryTelemetry
{
    private readonly ILogger _logger;

    public LoggingRecoveryTelemetry(ILogger logger)
        => _logger = logger;

    public void OnRecoveryAttempt(RecoveryContext context)
    {
        _logger.LogWarning(
            "[Recovery] {Operation} — attempt {N}/{Max}, error: {Err}",
            context.OperationName, context.AttemptNumber, context.MaxAttempts, context.Error);
    }

    public void OnRecoverySuccess(RecoveryContext context)
    {
        _logger.LogInformation(
            "[Recovery] {Operation} — succeeded on attempt {N}/{Max} in {Ms}ms",
            context.OperationName, context.AttemptNumber, context.MaxAttempts,
            context.Elapsed.TotalMilliseconds);
    }

    public void OnRecoveryExhausted(RecoveryContext context)
    {
        _logger.LogError(
            "[Recovery] {Operation} — exhausted after {N} attempt(s), last error: {Err}",
            context.OperationName, context.AttemptNumber, context.Error);
    }
}
```

---

## Registration with Dependency Injection

```csharp
// Program.cs / Startup.cs
services.AddSingleton<IRecoveryTelemetry>(sp =>
    new LoggingRecoveryTelemetry(
        sp.GetRequiredService<ILogger<LoggingRecoveryTelemetry>>()));
```

Inject and use:

```csharp
public class UserService
{
    private readonly IRecoveryTelemetry _telemetry;

    public UserService(IRecoveryTelemetry telemetry) => _telemetry = telemetry;

    public Task<Result<User>> GetUserAsync(Guid userId)
        => _cache.GetAsync(userId)
            .RescueAsync(
                when: err => err is CacheError.Miss,
                recover: _ => _db.GetAsync(userId),
                telemetry: _telemetry)
            .StartFixBranchAsync(
                when: err => err is DbError.Timeout,
                maxAttempts: 3,
                delay: TimeSpan.FromSeconds(1),
                fix: _ => _db.GetAsync(userId),
                telemetry: _telemetry);
}
```

---

## Metrics Implementation

For metrics (counters and histograms rather than logs):

```csharp
public sealed class MetricsRecoveryTelemetry : IRecoveryTelemetry
{
    private readonly IMeterFactory _meterFactory;
    private readonly Counter<int> _attemptCounter;
    private readonly Counter<int> _successCounter;
    private readonly Counter<int> _exhaustedCounter;

    public MetricsRecoveryTelemetry(IMeterFactory meterFactory)
    {
        _meterFactory = meterFactory;
        var meter = meterFactory.Create("MonadicSharp.Recovery");
        _attemptCounter   = meter.CreateCounter<int>("recovery.attempts");
        _successCounter   = meter.CreateCounter<int>("recovery.successes");
        _exhaustedCounter = meter.CreateCounter<int>("recovery.exhausted");
    }

    public void OnRecoveryAttempt(RecoveryContext ctx)
        => _attemptCounter.Add(1, new TagList { { "operation", ctx.OperationName } });

    public void OnRecoverySuccess(RecoveryContext ctx)
        => _successCounter.Add(1, new TagList { { "operation", ctx.OperationName } });

    public void OnRecoveryExhausted(RecoveryContext ctx)
        => _exhaustedCounter.Add(1, new TagList { { "operation", ctx.OperationName } });
}
```

---

## Null Telemetry

Passing `null` (the default) is always valid. Both `RescueAsync` and `StartFixBranchAsync` check for null before invoking any telemetry method — no NullReferenceException, no no-op wrapper required.

```csharp
// telemetry omitted — no overhead
var result = await _service.GetAsync(id)
    .RescueAsync(
        when: err => err is ServiceError.NotFound,
        recover: _ => _fallback.GetAsync(id));
```
