# MonadicSharp.Azure.Core

`MonadicSharp.Azure.Core` is required by all other MonadicSharp.Azure packages. It provides the `AzureError` hierarchy, HTTP status mapping, RFC 9457 ProblemDetails integration, and `IActionResult` extensions.

## Install

```bash
dotnet add package MonadicSharp.Azure.Core
```

## AzureError hierarchy

`AzureError` extends the base MonadicSharp `Error` type with Azure-specific properties and HTTP status codes baked in.

```csharp
// Every AzureError carries an HTTP status code
public abstract record AzureError(string Code, string Message) : Error(Code, Message)
{
    public abstract int HttpStatusCode { get; }
}

// Concrete types
public record NotFoundError(string ResourceType, string ResourceId)
    : AzureError($"{ResourceType}.NotFound", $"{ResourceType} '{ResourceId}' was not found.")
{
    public override int HttpStatusCode => 404;
}

public record RateLimitError(TimeSpan RetryAfter)
    : AzureError("Azure.RateLimit", "Too many requests.")
{
    public override int HttpStatusCode => 429;
}
```

You never instantiate these directly — they are produced by the extension methods on Azure SDK clients.

## HTTP status mapping

`AzureError` implements `IHttpStatusProvider`, which `ResultExtensions.ToActionResult()` uses to determine the correct HTTP response code.

```csharp
public static class ResultExtensions
{
    // Synchronous
    public static IActionResult ToActionResult<T>(this Result<T> result);

    // Asynchronous — use in controllers and Functions
    public static Task<IActionResult> ToActionResultAsync<T>(this Task<Result<T>> resultTask);
}
```

Mapping table (default, overridable):

| AzureError type | HTTP status |
|---|---|
| `AzureError.NotFound` | 404 |
| `AzureError.Conflict` | 409 |
| `AzureError.ConcurrencyConflict` | 412 |
| `AzureError.Unauthorized` | 401 |
| `AzureError.Forbidden` | 403 |
| `AzureError.RateLimit` | 429 |
| `AzureError.Timeout` | 408 |
| `AzureError.ServiceUnavailable` | 503 |
| `AzureError.Unexpected` | 500 |
| `Error.Validation` | 422 |

## ProblemDetails integration

`ToActionResult` serialises failures as RFC 9457 `ProblemDetails` automatically:

```json
{
  "type": "https://errors.monadic.dev/azure/not-found",
  "title": "Resource not found",
  "status": 404,
  "detail": "Order '3fa85f64-5717-4562-b3fc-2c963f66afa6' was not found.",
  "instance": "/orders/3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

For validation errors, `extensions.errors` contains field-level details:

```json
{
  "type": "https://errors.monadic.dev/validation",
  "title": "One or more validation errors occurred.",
  "status": 422,
  "extensions": {
    "errors": {
      "UserId": ["UserId is required."],
      "Items": ["Order must contain at least one item."]
    }
  }
}
```

## Minimal API example

```csharp
using MonadicSharp.Azure.Core;

var app = builder.Build();

app.MapGet("/orders/{id}", async (
    Guid id,
    IOrderService orderService,
    CancellationToken ct) =>
{
    return await orderService.GetOrderAsync(id, ct)
        .ToActionResultAsync();
    // Result<Order> → 200 OK with order body, or ProblemDetails on failure
});

app.MapPost("/orders", async (
    CreateOrderRequest req,
    IOrderService orderService,
    CancellationToken ct) =>
{
    return await orderService.CreateOrderAsync(req, ct)
        .ToActionResultAsync();
    // Result<Order> → 201 Created or 422/409/500 ProblemDetails
});
```

## Custom ProblemDetails factory

Override the default factory to add correlation IDs, custom extensions, or company-specific type URIs:

```csharp
builder.Services.AddSingleton<IProblemDetailsFactory, MyProblemDetailsFactory>();

public class MyProblemDetailsFactory : IProblemDetailsFactory
{
    public ProblemDetails Create(Error error, HttpContext? context = null)
    {
        var pd = DefaultProblemDetailsFactory.Instance.Create(error, context);
        pd.Extensions["correlationId"] = Activity.Current?.Id ?? context?.TraceIdentifier;
        pd.Extensions["environment"]   = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return pd;
    }
}
```

## Registration

```csharp
builder.Services.AddMonadicAzureCore();
// Registers IProblemDetailsFactory, ResultExtensions, AzureError mappings
```

Most packages call this internally — you only need it explicitly if using Core without any sub-package.
