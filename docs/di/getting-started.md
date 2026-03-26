# Getting Started

[![NuGet](https://img.shields.io/nuget/v/MonadicSharp.DI.svg?style=flat-square)](https://www.nuget.org/packages/MonadicSharp.DI/) [![NuGet Downloads](https://img.shields.io/nuget/dt/MonadicSharp.DI.svg?style=flat-square)](https://www.nuget.org/packages/MonadicSharp.DI/)


MonadicSharp.DI is a lightweight functional mediator for .NET — CQRS without MediatR's exception-based conventions, aligned with MonadicSharp's `Result<T>` primitives.

## Install

```bash
dotnet add package MonadicSharp.DI
```

**Requires**: .NET 6.0+, MonadicSharp.

## Register with DI

```csharp
builder.Services.AddMonadicMediator(typeof(Program).Assembly);
```

This scans the assembly for all `IQueryHandler` and `ICommandHandler` implementations and registers them automatically.

## Define a query

```csharp
// Query definition
public record GetUserQuery(Guid UserId) : IQuery<User>;

// Handler
public class GetUserHandler(IUserRepository repo) : IQueryHandler<GetUserQuery, User>
{
    public Task<Result<User>> HandleAsync(GetUserQuery query, CancellationToken ct) =>
        repo.FindByIdAsync(query.UserId, ct);
}
```

## Dispatch a query

```csharp
public class UserController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var result = await mediator.QueryAsync(new GetUserQuery(id));

        return result.Match(
            onSuccess: user  => Ok(user),
            onFailure: error => error.ToActionResult());
    }
}
```

## Define a command

```csharp
public record CreateOrderCommand(Guid UserId, List<OrderItem> Items) : ICommand<Order>;

public class CreateOrderHandler(
    IOrderRepository orders,
    IInventoryService inventory) : ICommandHandler<CreateOrderCommand, Order>
{
    public Task<Result<Order>> HandleAsync(CreateOrderCommand cmd, CancellationToken ct) =>
        inventory.ReserveAsync(cmd.Items, ct)
            .BindAsync(reserved => orders.CreateAsync(cmd.UserId, reserved, ct));
}
```

## Add a pipeline behavior

```csharp
// Validation behavior — runs before every command handler
public class ValidationBehavior<TRequest, TResult>
    : IPipelineBehavior<TRequest, TResult>
    where TRequest : ICommand<TResult>
{
    public async Task<Result<TResult>> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken ct)
    {
        var validation = ValidateRequest(request);
        if (validation.IsFailure) return validation.Error;
        return await next();
    }
}

// Register
builder.Services.AddBehavior(typeof(ValidationBehavior<,>));
```

## Standalone (no DI)

```csharp
var mediator = new Mediator(handler => new GetUserHandler(userRepo));
var result = await mediator.QueryAsync(new GetUserQuery(userId));
```

## Next steps

- [CQRS Pattern](./cqrs) — query vs command design decisions
- [Pipeline Behaviors](./pipeline-behaviors) — validation, logging, caching
- [Notifications](./api/notification) — pub/sub events
