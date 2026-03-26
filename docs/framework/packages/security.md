# MonadicSharp.Framework.Security

`MonadicSharp.Framework.Security` protects AI pipelines from prompt injection, secret leakage, and malformed input. Every validation method returns `Result<T>` — no exceptions, no null returns.

## Install

```bash
dotnet add package MonadicSharp.Framework.Security
```

## Core types

### `ISecurityGuard`

Main entry point. Validates input strings before they enter an LLM pipeline.

```csharp
public interface ISecurityGuard
{
    Task<Result<string>> ValidateInputAsync(string input, ValidationOptions? options = null);
    Task<Result<string>> MaskSecretsAsync(string input);
}
```

`ValidateInputAsync` runs injection detection and content policy checks in sequence. It returns the original (untransformed) input on success. On failure it returns a typed error — it never throws.

### `PromptInjectionDetector`

Stateless analyzer that scores an input string for injection patterns. You can use it independently if you want raw scores.

```csharp
public sealed class PromptInjectionDetector
{
    public InjectionAnalysis Analyze(string input);
}

public sealed class InjectionAnalysis
{
    public bool IsInjectionDetected { get; }
    public float Score { get; }              // 0.0 – 1.0
    public IReadOnlyList<string> Triggers { get; }
}
```

### `SecretMasker`

Scans text for common secret patterns (API keys, connection strings, JWTs, PII) and replaces them with `[REDACTED]`.

```csharp
public sealed class SecretMasker
{
    public string Mask(string input);
    public bool ContainsSecrets(string input);
}
```

## Error types

| Error | When |
|---|---|
| `SecurityError.InjectionDetected` | Injection patterns found in input |
| `SecurityError.SecretLeaked` | A secret pattern found in output |
| `SecurityError.InputTooLong` | Input exceeds configured `MaxInputLength` |
| `SecurityError.Unauthorized` | Caller does not have required claim |

`SecurityError.InjectionDetected` carries the list of matched triggers:

```csharp
if (result.IsFailure && result.Error is SecurityError.InjectionDetected e)
{
    logger.LogWarning("Injection blocked. Triggers: {Triggers}", e.Triggers);
}
```

## Configuration

### Built-in defaults

By default, `ISecurityGuard` checks for:

- System prompt override patterns (`ignore previous instructions`, `disregard your prompt`, etc.)
- Role impersonation (`you are now`, `act as`, `pretend you are`)
- Encoded payloads (base64, unicode escapes)
- Common PII patterns (email, phone, SSN) in output masking

### Custom patterns

```csharp
builder.Services.AddSecurity(opts =>
{
    opts.EnablePromptInjectionGuard = true;
    opts.EnableSecretMasking = true;
    opts.MaxInputLength = 4_096;

    opts.InjectionPatterns.Add(new Regex(@"ignore\s+all\s+rules", RegexOptions.IgnoreCase));
    opts.SecretPatterns.Add(new Regex(@"sk-[a-zA-Z0-9]{32,}"));
});
```

## Using the guard in a pipeline

```csharp
public class SecureSummarizationService(
    ISecurityGuard guard,
    IAgentPipeline pipeline)
{
    public async Task<Result<string>> SummarizeAsync(string userInput)
    {
        return await guard.ValidateInputAsync(userInput)
            .BindAsync(safe => pipeline.RunAsync<string, string>(safe, _steps));
    }
}
```

The `BindAsync` call short-circuits if `ValidateInputAsync` returns a failure. The pipeline is never invoked with a potentially injected input.

## Filtering output before returning to the user

`MaskSecretsAsync` should be applied to LLM responses before surfacing them:

```csharp
return await pipeline.RunAsync<string, string>(input, _steps)
    .BindAsync(guard.MaskSecretsAsync);
```

If the LLM response contains a secret pattern, the method returns `SecurityError.SecretLeaked` and the response is not forwarded.

## Registration

```csharp
builder.Services.AddSecurity(opts =>
{
    opts.EnablePromptInjectionGuard = true;
    opts.EnableSecretMasking = true;
});
```
