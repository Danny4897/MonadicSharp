# Getting Started

[![NuGet](https://img.shields.io/nuget/v/MonadicSharp.Azure.svg?style=flat-square)](https://www.nuget.org/packages/MonadicSharp.Azure/) [![NuGet Downloads](https://img.shields.io/nuget/dt/MonadicSharp.Azure.svg?style=flat-square)](https://www.nuget.org/packages/MonadicSharp.Azure/)


MonadicSharp.Azure wraps the Azure SDK in Railway-Oriented Programming patterns — every call returns `Result<T>` or `Option<T>`, never throws.

## Install

Install only the packages you need:

```bash
# Core (RFC 9457 ProblemDetails + HTTP mapping) — required by all others
dotnet add package MonadicSharp.Azure.Core

# Azure Functions v4
dotnet add package MonadicSharp.Azure.Functions

# CosmosDb
dotnet add package MonadicSharp.Azure.CosmosDb

# Service Bus
dotnet add package MonadicSharp.Azure.Messaging

# Blob Storage
dotnet add package MonadicSharp.Azure.Storage

# Key Vault
dotnet add package MonadicSharp.Azure.KeyVault

# Azure OpenAI
dotnet add package MonadicSharp.Azure.OpenAI
```

**Requires**: .NET 8.0+, MonadicSharp ≥ 1.5.

## Quick example — CosmosDb

```csharp
using MonadicSharp.Azure.CosmosDb;

// Before: try/catch with CosmosException
public async Task<User?> GetUserAsync(string id)
{
    try { return await container.ReadItemAsync<User>(id, new PartitionKey(id)); }
    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { return null; }
}

// After: Result<T> / Option<T>
public Task<Option<User>> GetUserAsync(string id) =>
    container.ReadItemMonadicAsync<User>(id, new PartitionKey(id));
```

## Quick example — Azure Functions

```csharp
using MonadicSharp.Azure.Functions;

[Function("CreateOrder")]
public async Task<IActionResult> RunAsync([HttpTrigger(...)] HttpRequest req)
{
    return await req.DeserializeBodyAsync<CreateOrderRequest>()
        .BindAsync(ValidateAsync)
        .BindAsync(orderService.CreateAsync)
        .ToActionResultAsync();  // Result<T> → IActionResult with correct HTTP status
}
```

## Error mapping

Every Azure error maps to a typed MonadicSharp error with an HTTP status code:

| Azure error | MonadicSharp error | HTTP |
|-------------|-------------------|------|
| 404 NotFound | `AzureError.NotFound` | 404 |
| 409 Conflict | `AzureError.Conflict` | 409 |
| 429 TooManyRequests | `AzureError.RateLimit` | 429 |
| 412 PreconditionFailed | `AzureError.ConcurrencyConflict` | 412 |

See [Error Mapping](./error-mapping) for the complete reference.

## Next steps

- [Functions package](./packages/functions)
- [CosmosDb package](./packages/cosmosdb)
- [Error mapping reference](./error-mapping)
