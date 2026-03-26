# StreamResult

`StreamResult` wraps an async token stream from an LLM and surfaces each token — or any mid-stream error — as a `Result`-like discriminated union. It implements `IAsyncEnumerable<StreamChunk>` so it works naturally with `await foreach`.

```csharp
using MonadicSharp.AI;
```

## FromAsync

```csharp
public static StreamResult FromAsync(
    IAsyncEnumerable<string> source,
    CancellationToken cancellationToken = default)
```

**Parameters**

| Parameter | Type | Description |
|---|---|---|
| `source` | `IAsyncEnumerable<string>` | The raw token stream from an LLM client. |
| `cancellationToken` | `CancellationToken` | Optional token to cancel enumeration early. |

**Returns**: `StreamResult` — an async sequence of `StreamChunk` values.

---

## StreamChunk

Each element yielded by `StreamResult` is a `StreamChunk` with two cases:

| Case | Description |
|---|---|
| `StreamChunk.Token(string value)` | A successfully received token fragment. |
| `StreamChunk.Error(AiError error)` | An error that interrupted the stream. |

Use `Match` to handle both:

```csharp
chunk.Match(
    onToken: token => Console.Write(token),
    onError: err   => logger.LogWarning("Stream error: {Err}", err));
```

---

## Basic Enumeration

```csharp
await foreach (var chunk in StreamResult.FromAsync(_llm.StreamAsync(prompt)))
{
    chunk.Match(
        onToken: token => Console.Write(token),
        onError: err   => logger.LogWarning("Stream interrupted: {Err}", err));
}
```

The loop ends when the source is exhausted or produces an error. After an error chunk, the stream terminates — no further tokens are yielded.

---

## With CancellationToken

Pass a `CancellationToken` to stop enumeration early. This is important for UI scenarios where the user may cancel mid-generation:

```csharp
using var cts = new CancellationTokenSource(timeout: TimeSpan.FromSeconds(30));

var sb = new StringBuilder();

try
{
    await foreach (var chunk in StreamResult.FromAsync(
        _llm.StreamAsync(prompt), cts.Token))
    {
        chunk.Match(
            onToken: token =>
            {
                sb.Append(token);
                Console.Write(token);
            },
            onError: err =>
            {
                logger.LogError("Stream failed mid-way: {Err}", err);
            });
    }
}
catch (OperationCanceledException)
{
    logger.LogInformation("Stream cancelled by user after {Chars} chars", sb.Length);
}
```

---

## Collecting the Full Response

When you need the assembled text after streaming completes:

```csharp
var sb = new StringBuilder();
AiError? streamError = null;

await foreach (var chunk in StreamResult.FromAsync(_llm.StreamAsync(prompt)))
{
    chunk.Match(
        onToken: token => sb.Append(token),
        onError: err   => { streamError = err; });
}

if (streamError is not null)
{
    logger.LogError("Streaming failed: {Err}", streamError);
    return Result.Fail(streamError);
}

return Result.Ok(sb.ToString());
```

---

## Chaining with ValidatedResult

Collect the stream and then validate the assembled output:

```csharp
var sb = new StringBuilder();

await foreach (var chunk in StreamResult.FromAsync(_llm.StreamAsync(structuredPrompt)))
    chunk.Match(onToken: t => sb.Append(t), onError: _ => { });

var result = await ValidatedResult<WeatherReport>
    .ParseAndValidateAsync(
        raw: sb.ToString(),
        validators: new Func<WeatherReport, bool>[]
        {
            r => r.Temperature is >= -90 and <= 60,
            r => !string.IsNullOrWhiteSpace(r.Location)
        });
```

---

## Error Semantics

A `StreamChunk.Error` carries an `AiError` using the same cases as the rest of MonadicSharp.AI:

```csharp
chunk.Match(
    onToken: t => buffer.Append(t),
    onError: err =>
    {
        switch (err)
        {
            case AiError.Timeout t:
                logger.LogWarning("Stream timed out after {Elapsed}s", t.Elapsed.TotalSeconds);
                break;
            case AiError.ContentFiltered cf:
                logger.LogWarning("Content filtered mid-stream: {Reason}", cf.Reason);
                break;
            default:
                logger.LogError("Unexpected stream error: {Err}", err);
                break;
        }
    });
```

There is no retry logic inside `StreamResult` itself. To retry a stream from the beginning on a transient error, wrap the entire `foreach` loop inside `RetryResult<string>.ExecuteAsync` using the collect-then-return pattern.
