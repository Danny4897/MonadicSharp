# MonadicSharp.Azure.KeyVault

Railway-Oriented Programming wrapper for Azure Key Vault — every secret read returns `Result<T>` or `Option<T>`, never throws.

## Install

```bash
dotnet add package MonadicSharp.Azure.KeyVault
```

**Requires**: MonadicSharp.Azure.Core, Azure.Security.KeyVault.Secrets.

## Read a secret

```csharp
using MonadicSharp.Azure.KeyVault;

var client = new MonadicSecretClient(keyVaultUri, new DefaultAzureCredential());

// Returns Option<string> — None if secret does not exist, Failure on auth/network errors
Option<string> secret = await client.GetSecretMonadicAsync("ConnectionStrings--Database");

string connStr = secret.Match(
    onSome: value => value,
    onNone: ()    => throw new InvalidOperationException("Missing secret"));
```

## Set a secret

```csharp
Result<KeyVaultSecret> result = await client.SetSecretMonadicAsync(
    name:  "Api--ExternalService--Key",
    value: newApiKey);

result.Match(
    onSuccess: secret => logger.LogInformation("Secret updated, version {V}", secret.Properties.Version),
    onFailure: err    => logger.LogError("Failed to update secret: {Err}", err));
```

## App startup — fail fast if secrets are missing

```csharp
// Program.cs
var vault = new MonadicSecretClient(
    new Uri(builder.Configuration["KeyVault:Uri"]!),
    new DefaultAzureCredential());

var secrets = await Result.CombineAsync(
    vault.GetSecretMonadicAsync("Db--ConnectionString").ToResultAsync("Db--ConnectionString"),
    vault.GetSecretMonadicAsync("Auth--JwtSecret").ToResultAsync("Auth--JwtSecret"),
    vault.GetSecretMonadicAsync("Email--ApiKey").ToResultAsync("Email--ApiKey"));

secrets.Match(
    onSuccess: (db, jwt, email) =>
    {
        builder.Configuration["ConnectionStrings:Default"] = db;
        builder.Configuration["Auth:JwtSecret"]            = jwt;
        builder.Configuration["Email:ApiKey"]              = email;
    },
    onFailure: err =>
    {
        logger.LogCritical("Startup failed — missing secret: {Err}", err);
        Environment.Exit(1);
    });
```

## Secret caching

Avoid hitting Key Vault on every request by caching secrets with a TTL:

```csharp
builder.Services.AddMonadicKeyVault(opts =>
{
    opts.VaultUri       = new Uri(builder.Configuration["KeyVault:Uri"]!);
    opts.CacheDuration  = TimeSpan.FromMinutes(15);
    opts.Credential     = new DefaultAzureCredential();
});

// Then inject IMonadicSecretCache
public class MyService(IMonadicSecretCache secrets)
{
    public async Task<Result<string>> GetApiKeyAsync() =>
        await secrets.GetSecretMonadicAsync("Api--ExternalService--Key")
                     .ToResultAsync("Api--ExternalService--Key");
}
```

## Error types

| Scenario | Error type |
|---|---|
| Secret not found | `Option.None` |
| Unauthorized (403) | `AzureError.Unauthorized` |
| Rate limit (429) | `AzureError.RateLimit` |
| Network failure | `AzureError.Unavailable` |
| Invalid secret name | `AzureError.InvalidInput` |

See [Error Mapping](/error-mapping) for the full reference.
