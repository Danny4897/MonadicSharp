# Error Mapping Reference

MonadicSharp.Azure maps every Azure SDK exception to a typed `AzureError` value. No exception ever escapes the library boundary — callers always receive `Result<T>` or `Option<T>`.

## CosmosDB

| Azure Exception | HTTP Status | MonadicSharp Error | Additional Properties |
|---|---|---|---|
| `CosmosException` (404) | 404 | `AzureError.NotFound` | `ResourceType`, `ResourceId` |
| `CosmosException` (409) | 409 | `AzureError.Conflict` | `ResourceId` |
| `CosmosException` (412) | 412 | `AzureError.ConcurrencyConflict` | `CurrentETag`, `ExpectedETag` |
| `CosmosException` (429) | 429 | `AzureError.RateLimit` | `RetryAfter` |
| `CosmosException` (503) | 503 | `AzureError.ServiceUnavailable` | — |
| `CosmosException` (408) | 408 | `AzureError.Timeout` | — |
| Any other `CosmosException` | varies | `AzureError.Unexpected` | `StatusCode`, `ActivityId` |

## Service Bus

| Azure Exception | HTTP Status | MonadicSharp Error | Additional Properties |
|---|---|---|---|
| `ServiceBusException` (MessageLockLost) | 410 | `AzureError.LockLost` | `EntityPath` |
| `ServiceBusException` (MessageSizeExceeded) | 413 | `AzureError.MessageTooLarge` | `MaxSizeInBytes` |
| `ServiceBusException` (QuotaExceeded) | 429 | `AzureError.QuotaExceeded` | `EntityPath` |
| `ServiceBusException` (ServiceTimeout) | 408 | `AzureError.Timeout` | — |
| `ServiceBusException` (MessagingEntityNotFound) | 404 | `AzureError.NotFound` | `EntityPath` |
| `ServiceBusException` (Unauthorized) | 401 | `AzureError.Unauthorized` | — |
| `ServiceBusException` (other) | 500 | `AzureError.Unexpected` | `Reason` |

## Blob Storage

| Azure Exception | HTTP Status | MonadicSharp Error | Additional Properties |
|---|---|---|---|
| `RequestFailedException` (404) | 404 | `AzureError.NotFound` | `BlobName`, `ContainerName` |
| `RequestFailedException` (409 — BlobAlreadyExists) | 409 | `AzureError.Conflict` | `BlobName` |
| `RequestFailedException` (412) | 412 | `AzureError.ConcurrencyConflict` | `CurrentETag` |
| `RequestFailedException` (403) | 403 | `AzureError.Forbidden` | — |
| `RequestFailedException` (401) | 401 | `AzureError.Unauthorized` | — |
| `RequestFailedException` (429) | 429 | `AzureError.RateLimit` | `RetryAfter` |
| `RequestFailedException` (other) | varies | `AzureError.Unexpected` | `ErrorCode`, `Status` |

## Key Vault

| Azure Exception | HTTP Status | MonadicSharp Error | Additional Properties |
|---|---|---|---|
| `RequestFailedException` (404) | 404 | `AzureError.NotFound` | `SecretName` |
| `RequestFailedException` (403) | 403 | `AzureError.Forbidden` | `SecretName` |
| `RequestFailedException` (401) | 401 | `AzureError.Unauthorized` | — |
| `RequestFailedException` (429) | 429 | `AzureError.RateLimit` | `RetryAfter` |
| `RequestFailedException` (409 — SecretDisabled) | 409 | `AzureError.Conflict` | `SecretName` |
| `RequestFailedException` (other) | varies | `AzureError.Unexpected` | `ErrorCode` |

## Azure OpenAI

| Azure Exception | HTTP Status | MonadicSharp Error | Additional Properties |
|---|---|---|---|
| `RequestFailedException` (429) | 429 | `AzureOpenAiError.RateLimit` | `RetryAfter`, `DeploymentName` |
| `RequestFailedException` (404) | 404 | `AzureOpenAiError.DeploymentNotFound` | `DeploymentName` |
| `RequestFailedException` (400 — content_filter) | 400 | `AzureOpenAiError.ContentFiltered` | `FilterCategory` |
| `RequestFailedException` (400 — context_length_exceeded) | 400 | `AzureOpenAiError.ContextLengthExceeded` | `MaxTokens` |
| `RequestFailedException` (401) | 401 | `AzureOpenAiError.Unauthorized` | — |
| `RequestFailedException` (503) | 503 | `AzureOpenAiError.ServiceUnavailable` | — |
| `RequestFailedException` (other) | varies | `AzureOpenAiError.Unexpected` | `ErrorCode` |

## AzureError hierarchy

```
Error (MonadicSharp base)
└── AzureError
    ├── AzureError.NotFound
    ├── AzureError.Conflict
    ├── AzureError.ConcurrencyConflict
    ├── AzureError.RateLimit
    ├── AzureError.Timeout
    ├── AzureError.Unauthorized
    ├── AzureError.Forbidden
    ├── AzureError.ServiceUnavailable
    ├── AzureError.QuotaExceeded
    ├── AzureError.LockLost
    ├── AzureError.MessageTooLarge
    └── AzureError.Unexpected
        └── AzureOpenAiError (extends AzureError.Unexpected)
            ├── AzureOpenAiError.RateLimit
            ├── AzureOpenAiError.DeploymentNotFound
            ├── AzureOpenAiError.ContentFiltered
            └── AzureOpenAiError.ContextLengthExceeded
```

## Handling specific error types

```csharp
var result = await container.ReadItemMonadicAsync<Order>(id, partitionKey);

return result.Match(
    onSuccess: order => Ok(order),
    onFailure: error => error switch
    {
        AzureError.NotFound             => NotFound(),
        AzureError.ConcurrencyConflict  => Conflict("Item was modified by another process."),
        AzureError.RateLimit r          => StatusCode(429, $"Retry after {r.RetryAfter.TotalSeconds}s"),
        _                               => StatusCode(500, error.Message)
    });
```

## Customising the mapping

Provide a custom `IAzureErrorMapper` to override or extend the default behavior:

```csharp
public class MyCosmosErrorMapper : IAzureErrorMapper<CosmosException>
{
    public Error Map(CosmosException ex) => ex.StatusCode switch
    {
        HttpStatusCode.NotFound    => AzureError.NotFound with { ResourceId = ex.ActivityId },
        HttpStatusCode.Conflict    => new MyDomainConflictError(ex.Message),
        _                          => AzureError.Unexpected.From(ex)
    };
}

// Registration
builder.Services.AddSingleton<IAzureErrorMapper<CosmosException>, MyCosmosErrorMapper>();
```

The custom mapper is injected into the relevant extension methods automatically via DI.
