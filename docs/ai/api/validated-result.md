# ValidatedResult\<T\>

`ValidatedResult<T>` parses a raw LLM string response into a strongly typed value and validates it against a schema or set of rules. It returns `Result<T>`, producing `AiError.InvalidResponse` when parsing or validation fails.

```csharp
using MonadicSharp.AI;
```

## ParseAndValidateAsync

```csharp
public static Task<Result<T>> ParseAndValidateAsync(
    string raw,
    JsonSerializerOptions? options = null,
    IEnumerable<Func<T, bool>>? validators = null)
```

**Parameters**

| Parameter | Type | Description |
|---|---|---|
| `raw` | `string` | The raw string returned by the LLM. Expected to be valid JSON. |
| `options` | `JsonSerializerOptions?` | Optional custom serializer options (e.g. camelCase, converters). |
| `validators` | `IEnumerable<Func<T, bool>>?` | Optional list of predicates. The result fails if any predicate returns `false`. |

**Returns**: `Task<Result<T>>`

- `Ok(value)` — JSON parsed successfully and all validators passed.
- `Fail(AiError.InvalidResponse(raw))` — JSON deserialization threw, or at least one validator returned `false`.

---

## Basic Usage

Define the target type as a record or class that matches the JSON the LLM is expected to produce:

```csharp
public record WeatherReport(
    string Location,
    double Temperature,
    string Condition,
    DateTime FetchedAt);
```

Parse the raw LLM output:

```csharp
var result = await ValidatedResult<WeatherReport>
    .ParseAndValidateAsync(raw: llmResponse);

result.Match(
    onSuccess: report => Console.WriteLine($"{report.Location}: {report.Temperature}°C"),
    onFailure: err    => logger.LogError("Parse failed: {Err}", err));
```

---

## Schema Validation with Validators

Pass predicates to enforce domain constraints after parsing:

```csharp
var result = await ValidatedResult<WeatherReport>
    .ParseAndValidateAsync(
        raw: llmResponse,
        validators: new Func<WeatherReport, bool>[]
        {
            r => r.Temperature is >= -90 and <= 60,
            r => !string.IsNullOrWhiteSpace(r.Location),
            r => r.Condition is "Sunny" or "Cloudy" or "Rainy" or "Snowy"
        });
```

If any predicate fails, the result is `AiError.InvalidResponse` containing the original `raw` string. You can inspect it to build a correction prompt:

```csharp
result.Match(
    onSuccess: report => Display(report),
    onFailure: err    =>
    {
        if (err is AiError.InvalidResponse ir)
        {
            // Feed the bad output back to the LLM with instructions to fix it
            var correctionPrompt = $"The following JSON is invalid:\n{ir.Raw}\nPlease correct it.";
        }
    });
```

---

## Custom JsonSerializerOptions

Use `options` when the LLM returns camelCase property names or requires custom converters:

```csharp
var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
};

var result = await ValidatedResult<ForecastResponse>
    .ParseAndValidateAsync(raw: llmResponse, options: options);
```

---

## In a Full Pipeline

`ValidatedResult<T>.ParseAndValidateAsync` is designed to be chained after `RetryResult<T>`:

```csharp
var result = await RetryResult<string>
    .ExecuteAsync(
        maxAttempts: 3,
        action: () => _llm.CompleteAsync(structuredPrompt),
        shouldRetry: err => err is AiError.RateLimit or AiError.Timeout)
    .WithDelay(TimeSpan.FromSeconds(1))
    .BindAsync(raw => ValidatedResult<WeatherReport>.ParseAndValidateAsync(
        raw,
        validators: new Func<WeatherReport, bool>[]
        {
            r => r.Temperature is >= -90 and <= 60,
            r => !string.IsNullOrWhiteSpace(r.Location)
        }))
    .MapAsync(report => report with { FetchedAt = DateTime.UtcNow });

result.Match(
    onSuccess: report => Console.WriteLine($"{report.Location}: {report.Temperature}°C, fetched {report.FetchedAt:u}"),
    onFailure: err    => logger.LogError("Pipeline failed: {Err}", err));
```

---

## Prompting for Structured Output

`ValidatedResult<T>` works best when your prompt explicitly requests JSON. Example system instruction:

```csharp
var prompt = $"""
    Return a JSON object with the following fields only:
    - Location (string): the city name
    - Temperature (number): degrees Celsius
    - Condition (string): one of Sunny, Cloudy, Rainy, Snowy

    Query: {userQuery}
    """;
```

If the model does not follow the format, `ValidatedResult<T>` captures the raw output in `AiError.InvalidResponse.Raw`, giving you the data needed to build a self-correction loop.
