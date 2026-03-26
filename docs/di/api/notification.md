# INotification and INotificationHandler\<T\>

Notifications implement a publish/subscribe pattern on top of the mediator. One notification can have multiple handlers — all of them run when the event is published.

## Interfaces

```csharp
// Marker interface — implement on your event record
public interface INotification { }

// Implement once per notification type per subscriber
public interface INotificationHandler<TNotification>
    where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken ct);
}
```

Notification handlers do not return `Result<T>`. They return `Task` — if a handler throws, the exception propagates to the publisher. If you need fault isolation between handlers, see the failure strategy section below.

## Registration

Notification handlers are scanned automatically by `AddMonadicMediator`:

```csharp
builder.Services.AddMonadicMediator(typeof(Program).Assembly);
```

## Defining a notification

```csharp
public record OrderCreatedNotification(
    Guid OrderId,
    Guid UserId,
    decimal Total,
    DateTimeOffset CreatedAt) : INotification;
```

## Publishing after a command

The most common pattern: a command handler (or a pipeline behavior) publishes a notification on success.

```csharp
public class CreateOrderHandler(
    IOrderRepository orders,
    IUnitOfWork uow,
    IMediator mediator) : ICommandHandler<CreateOrderCommand, Order>
{
    public async Task<Result<Order>> HandleAsync(CreateOrderCommand cmd, CancellationToken ct)
    {
        var order = Order.Create(cmd.UserId, cmd.Items, cmd.ShippingAddress);

        await orders.AddAsync(order, ct);
        await uow.SaveChangesAsync(ct);

        // Publish notification — handlers run in the same scope/transaction
        await mediator.PublishAsync(
            new OrderCreatedNotification(order.Id, order.UserId, order.Total, order.CreatedAt),
            ct);

        return Result.Success(order);
    }
}
```

## Example — two handlers for one notification

### Handler 1: Send confirmation email

```csharp
public class SendOrderConfirmationHandler(IEmailService email)
    : INotificationHandler<OrderCreatedNotification>
{
    public async Task HandleAsync(OrderCreatedNotification notification, CancellationToken ct)
    {
        var user = await email.ResolveUserAsync(notification.UserId, ct);

        await email.SendAsync(new EmailMessage
        {
            To      = user.Email,
            Subject = $"Order #{notification.OrderId} confirmed",
            Body    = $"Your order of {notification.Total:C} has been placed."
        }, ct);
    }
}
```

### Handler 2: Update inventory reservation

```csharp
public class ReserveInventoryHandler(IInventoryService inventory)
    : INotificationHandler<OrderCreatedNotification>
{
    public async Task HandleAsync(OrderCreatedNotification notification, CancellationToken ct)
    {
        await inventory.ConfirmReservationAsync(notification.OrderId, ct);
    }
}
```

Both handlers are discovered automatically. Publishing `OrderCreatedNotification` once invokes both.

## Execution semantics

By default, notification handlers run **sequentially** in registration order within the same scope. If you need parallel execution, wrap in `Task.WhenAll` at the publisher level or implement a custom `INotificationPublisher`.

```csharp
// Custom parallel publisher
public class ParallelNotificationPublisher : INotificationPublisher
{
    public Task PublishAsync<TNotification>(
        TNotification notification,
        IEnumerable<INotificationHandler<TNotification>> handlers,
        CancellationToken ct)
        where TNotification : INotification =>
        Task.WhenAll(handlers.Select(h => h.HandleAsync(notification, ct)));
}

// Registration
builder.Services.AddSingleton<INotificationPublisher, ParallelNotificationPublisher>();
```

## Isolating handler failures

If one handler must not block others on failure, catch internally:

```csharp
public class SendOrderConfirmationHandler(IEmailService email, ILogger<SendOrderConfirmationHandler> logger)
    : INotificationHandler<OrderCreatedNotification>
{
    public async Task HandleAsync(OrderCreatedNotification notification, CancellationToken ct)
    {
        try
        {
            await email.SendAsync(..., ct);
        }
        catch (Exception ex)
        {
            // Log and swallow — other handlers continue
            logger.LogError(ex, "Failed to send confirmation for order {OrderId}", notification.OrderId);
        }
    }
}
```

## Notifications vs commands

| | ICommand\<T\> | INotification |
|---|---|---|
| Handlers | Exactly one | Zero or more |
| Return value | `Result<T>` | `Task` (void) |
| Failure propagation | Caller sees `Result.Failure` | Exception propagates unless caught |
| Use when | You need the result of an action | You want to broadcast an event |
