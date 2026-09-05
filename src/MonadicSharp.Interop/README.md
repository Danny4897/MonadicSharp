# MonadicSharp.Interop

Bridge MonadicSharp primitives with HTTP clients and ASP.NET Core minimal APIs.
On **.NET 10+**, extension members are surfaced via C# 14 extension blocks directly on `HttpResponseMessage`.

```bash
dotnet add package MonadicSharp.Interop
```

## HttpResponseMessage → Result

```csharp
using MonadicSharp.Interop;

// .NET 10+ (C# 14 extension member — no static class prefix needed)
var result = await client.GetAsync("/api/users/1");
var typed  = await result.ReadAsResult<User>();

// All targets (standard extension method — identical API)
var typed = await response.ReadAsResult<User>();
```

## HttpClient wrappers

```csharp
// Zero-boilerplate HTTP calls that never throw
Result<User> user = await httpClient.GetAsResult<User>("/api/users/1");
Result<Order> order = await httpClient.PostAsResult<CreateOrderRequest, Order>("/api/orders", req);
```

## ASP.NET Core Minimal APIs

```csharp
using MonadicSharp.Interop.AspNetCore;

app.MapGet("/users/{id}", async (int id, UserService svc) =>
    await svc.GetByIdAsync(id).ToOkResult());

// With custom success shape
app.MapPost("/users", async (CreateUserRequest req, UserService svc) =>
    await svc.CreateAsync(req)
             .ToMinimalApiResult(user => Results.Created($"/users/{user.Id}", user)));
```

## Status code → Error type mapping

| HTTP Status | `ErrorType` |
|---|---|
| 400 | `Validation` |
| 401, 403 | `Forbidden` |
| 404 | `NotFound` |
| 409 | `Conflict` |
| 5xx | `Failure` |
