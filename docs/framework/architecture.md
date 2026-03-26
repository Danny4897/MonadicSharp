# Architecture

MonadicSharp.Framework is designed as a layered system where every package depends only on MonadicSharp core — they never depend on each other, so you can adopt them incrementally.

## Package Dependency Graph

```
                    MonadicSharp (core)
                         │
        ┌────────────────┼────────────────┐
        │                │                │
   Framework.Agents  Framework.Http  Framework.Persistence
        │                │                │
   Framework.Security Framework.Caching Framework.Telemetry
```

- **Horizontal independence**: Agents does not import Http; Http does not import Persistence. Zero coupling.
- **Vertical layering**: Security and Telemetry are cross-cutting concerns — they integrate with any other package via composition, not inheritance.

## The Result\<T\> Contract

Every public method in the framework returns `Result<T, Error>` or `Task<Result<T, Error>>`. This means:

```csharp
// What you write:
var result = await agent.RunAsync(input)
    .BindAsync(security.ValidateAsync)
    .BindAsync(cache.GetOrSetAsync)
    .BindAsync(http.PostAsync);

// What you never write:
try { ... } catch (HttpRequestException ex) { ... }
try { ... } catch (CacheException ex) { ... }
```

Errors are values. They compose.

## Error Hierarchy

All framework errors extend `MonadicSharp.Error`:

| Package | Error Types |
|---------|-------------|
| Agents | `AgentError.Timeout`, `AgentError.StepFailed`, `AgentError.MaxRetriesExceeded` |
| Security | `SecurityError.InjectionDetected`, `SecurityError.SecretLeaked`, `SecurityError.Unauthorized` |
| Http | `HttpError.NetworkFailure`, `HttpError.Timeout`, `HttpError.RateLimit` |
| Persistence | `PersistenceError.NotFound`, `PersistenceError.ConcurrencyConflict` |
| Caching | `CacheError.Miss`, `CacheError.Serialization` |

## Telemetry Integration

Framework.Telemetry automatically instruments all `Result<T, Error>` pipelines when OpenTelemetry is configured:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddMonadicFrameworkInstrumentation()
        .AddOtlpExporter());
```

Spans are created per pipeline step. Failed results automatically set `span.Status = Error` with the structured error message.
