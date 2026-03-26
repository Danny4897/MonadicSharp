# MonadicSharp.Azure.OpenAI

Railway-Oriented Programming wrapper for Azure OpenAI — typed errors, composable pipelines, built-in integration with MonadicSharp.AI.

## Install

```bash
dotnet add package MonadicSharp.Azure.OpenAI
```

**Requires**: MonadicSharp.Azure.Core, MonadicSharp.AI, Azure.AI.OpenAI.

## Chat completion

```csharp
using MonadicSharp.Azure.OpenAI;

var client = new MonadicAzureOpenAiClient(endpoint, new DefaultAzureCredential());

Result<ChatCompletion> result = await client.ChatCompletionMonadicAsync(
    deploymentName: "gpt-4o",
    messages: [
        new SystemChatMessage("You are a helpful assistant."),
        new UserChatMessage(userInput)
    ]);

result.Match(
    onSuccess: completion => Console.WriteLine(completion.Content[0].Text),
    onFailure: err        => logger.LogError("OpenAI call failed: {Err}", err));
```

## Streaming

```csharp
await foreach (var chunk in client.ChatCompletionStreamMonadicAsync("gpt-4o", messages))
{
    chunk.Match(
        onToken: token => Console.Write(token),
        onError: err   => logger.LogWarning("Stream interrupted: {Err}", err));
}
```

## Embeddings

```csharp
Result<ReadOnlyMemory<float>> embedding = await client.EmbeddingMonadicAsync(
    deploymentName: "text-embedding-3-small",
    input: documentChunk);

embedding.Match(
    onSuccess: vector => vectorDb.StoreAsync(documentId, vector),
    onFailure: err    => logger.LogError("Embedding failed: {Err}", err));
```

## Compose with MonadicSharp.AI retry

```csharp
using MonadicSharp.AI;

// Retry on rate-limit and timeout — up to 3 attempts with exponential backoff
var result = await RetryResult<ChatCompletion>
    .ExecuteAsync(
        maxAttempts: 3,
        action:      () => client.ChatCompletionMonadicAsync("gpt-4o", messages),
        shouldRetry: err => err is AzureOpenAiError.RateLimit or AzureOpenAiError.Timeout,
        delay:       attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
```

## Trace with AgentResult

```csharp
var traced = await AgentResult<string>
    .StartAsync("answer-query", userInput)
    .StepAsync("embed",    input => client.EmbeddingMonadicAsync("text-embedding-3-small", input))
    .StepAsync("retrieve", vector => vectorDb.SearchAsync(vector, topK: 5))
    .StepAsync("complete", context => client.ChatCompletionMonadicAsync("gpt-4o",
        BuildMessages(userInput, context)));
```

## Error types

| Scenario | Error type |
|---|---|
| Rate limit (429) | `AzureOpenAiError.RateLimit` with `RetryAfter` |
| Content filtered | `AzureOpenAiError.ContentFiltered` with `Reason` |
| Token limit exceeded | `AzureOpenAiError.TokenLimitExceeded` with `TokensUsed` |
| Model unavailable | `AzureOpenAiError.ModelUnavailable` |
| Timeout | `AzureOpenAiError.Timeout` |
| Auth failure | `AzureError.Unauthorized` |

See [Error Mapping](/error-mapping) for the full reference.
