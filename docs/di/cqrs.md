# CQRS with MonadicSharp.DI

CQRS (Command Query Responsibility Segregation) separates read operations (queries) from write operations (commands). MonadicSharp.DI implements this pattern using `Result<T>` as the universal return type — no exceptions, no nulls.

## Why `Result<T>` instead of exceptions

Classic MediatR handlers throw exceptions on failure. The caller must catch them (or forget to). MonadicSharp.DI inverts this: every handler returns `Result<T>`, making failure a first-class value in the type system.

```csharp
// MediatR — failure is hidden, exception may propagate unhandled
public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken ct)
    {
        var user = await _repo.FindByIdAsync(request.UserId, ct);
        if (user is null) throw new NotFoundException($"User {request.UserId} not found");
        return user;
    }
}

// MonadicSharp.DI — failure is explicit in the return type
public class GetUserHandler(IUserRepository repo) : IQueryHandler<GetUserQuery, User>
{
    public Task<Result<User>> HandleAsync(GetUserQuery query, CancellationToken ct) =>
        repo.FindByIdAsync(query.UserId, ct); // returns Result<User> directly
}
```

The controller or function host calls `.Match()` and decides the HTTP response — the handler never makes that decision.

## IQuery\<T\> — read-only operations

Use `IQuery<T>` for operations that:
- Return data without side effects
- Are safe to retry
- Can potentially be cached

```csharp
// Query definition — a record is idiomatic
public record GetOrderQuery(Guid OrderId) : IQuery<OrderDto>;

// Handler — reads, never writes
public class GetOrderHandler(IOrderRepository orders) : IQueryHandler<GetOrderQuery, OrderDto>
{
    public async Task<Result<OrderDto>> HandleAsync(GetOrderQuery query, CancellationToken ct)
    {
        var order = await orders.FindAsync(query.OrderId, ct);

        return order.Map(o => new OrderDto(o.Id, o.Status, o.Total));
    }
}
```

### Dispatching a query

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetOrder(Guid id, IMediator mediator)
{
    var result = await mediator.QueryAsync(new GetOrderQuery(id));

    return result.Match(
        onSuccess: dto   => Ok(dto),
        onFailure: error => error.ToActionResult());
}
```

## ICommand\<T\> — write operations with side effects

Use `ICommand<T>` for operations that:
- Mutate state (database, external service, file system)
- Should not be retried blindly (idempotency must be explicit)
- Produce a meaningful result (the created or updated entity)

Use `ICommand<Unit>` when the operation succeeds or fails but returns no value.

```csharp
// Command definition
public record CreateOrderCommand(
    Guid UserId,
    List<OrderItem> Items,
    string ShippingAddress) : ICommand<Order>;

// Handler — has side effects
public class CreateOrderHandler(
    IOrderRepository orders,
    IInventoryService inventory,
    IEventBus events) : ICommandHandler<CreateOrderCommand, Order>
{
    public Task<Result<Order>> HandleAsync(CreateOrderCommand cmd, CancellationToken ct) =>
        inventory.ReserveAsync(cmd.Items, ct)
            .BindAsync(reserved => orders.CreateAsync(cmd.UserId, reserved, cmd.ShippingAddress, ct))
            .TapAsync(order    => events.PublishAsync(new OrderCreatedEvent(order.Id), ct));
}
```

### Dispatching a command

```csharp
[HttpPost]
public async Task<IActionResult> CreateOrder(
    CreateOrderRequest req,
    IMediator mediator)
{
    var result = await mediator.CommandAsync(
        new CreateOrderCommand(req.UserId, req.Items, req.ShippingAddress));

    return result.Match(
        onSuccess: order => CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order),
        onFailure: error => error.ToActionResult());
}
```

## Query vs Command — decision table

| Criterion | IQuery\<T\> | ICommand\<T\> |
|---|---|---|
| Reads state | Yes | Optionally |
| Mutates state | No | Yes |
| Safe to retry | Yes | Depends on idempotency |
| Cacheable | Yes | No |
| Result type | `Result<T>` (T = DTO) | `Result<T>` or `Result<Unit>` |
| Pipeline behaviors | Typically logging, caching | Typically validation, transaction |

## Comparison with classic MediatR

| Aspect | MediatR | MonadicSharp.DI |
|---|---|---|
| Failure model | Exceptions | `Result<T>` return value |
| Not found pattern | `throw NotFoundException` | `Result.Failure(Error.NotFound(...))` |
| Caller contract | Implicit (must catch) | Explicit (must handle both branches) |
| Pipeline | `IPipelineBehavior<TReq, TRes>` | `IPipelineBehavior<TRequest, TResult>` (same shape) |
| Registration | `AddMediatR(...)` | `AddMonadicMediator(...)` |
| Notifications | `INotification` + `INotificationHandler` | `INotification` + `INotificationHandler` |

The surface area is intentionally similar so migration is mechanical: replace `throw` with `Result.Failure(...)` and remove try/catch from callers.
