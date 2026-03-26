# MonadicSharp.Azure.Messaging

`MonadicSharp.Azure.Messaging` wraps the Azure Service Bus SDK. Every send, receive, and settle operation returns `Result<T>` — no `ServiceBusException` escapes.

## Install

```bash
dotnet add package MonadicSharp.Azure.Messaging
```

## Methods

```csharp
// Send a single message
Task<Result<Unit>> SendMessageMonadicAsync(
    this ServiceBusSender sender,
    ServiceBusMessage message,
    CancellationToken ct = default);

// Send a batch
Task<Result<Unit>> SendBatchMonadicAsync(
    this ServiceBusSender sender,
    IEnumerable<ServiceBusMessage> messages,
    CancellationToken ct = default);

// Receive messages
Task<Result<IReadOnlyList<ServiceBusReceivedMessage>>> ReceiveMessagesMonadicAsync(
    this ServiceBusReceiver receiver,
    int maxMessages = 10,
    TimeSpan? maxWaitTime = null,
    CancellationToken ct = default);

// Settle
Task<Result<Unit>> CompleteMessageMonadicAsync(
    this ServiceBusReceiver receiver,
    ServiceBusReceivedMessage message,
    CancellationToken ct = default);

Task<Result<Unit>> DeadLetterMessageMonadicAsync(
    this ServiceBusReceiver receiver,
    ServiceBusReceivedMessage message,
    string reason,
    CancellationToken ct = default);

Task<Result<Unit>> AbandonMessageMonadicAsync(
    this ServiceBusReceiver receiver,
    ServiceBusReceivedMessage message,
    CancellationToken ct = default);
```

## Producer example

```csharp
public class OrderEventProducer(ServiceBusSender sender)
{
    public Task<Result<Unit>> PublishOrderCreatedAsync(
        Order order,
        CancellationToken ct)
    {
        var body    = JsonSerializer.SerializeToUtf8Bytes(new OrderCreatedEvent(order.Id, order.Total));
        var message = new ServiceBusMessage(body)
        {
            MessageId            = order.Id.ToString(),
            ContentType          = "application/json",
            Subject              = "OrderCreated",
            ApplicationProperties =
            {
                ["version"] = "1.0",
                ["userId"]  = order.UserId.ToString()
            }
        };

        return sender.SendMessageMonadicAsync(message, ct);
    }

    public Task<Result<Unit>> PublishBatchAsync(
        IEnumerable<Order> orders,
        CancellationToken ct) =>
        sender.SendBatchMonadicAsync(
            orders.Select(o =>
            {
                var body = JsonSerializer.SerializeToUtf8Bytes(new OrderCreatedEvent(o.Id, o.Total));
                return new ServiceBusMessage(body) { MessageId = o.Id.ToString() };
            }),
            ct);
}
```

## Consumer example

```csharp
public class OrderProcessor(ServiceBusReceiver receiver, IOrderService orders)
{
    public async Task<Result<int>> ProcessBatchAsync(CancellationToken ct)
    {
        var messagesResult = await receiver.ReceiveMessagesMonadicAsync(
            maxMessages: 20,
            maxWaitTime: TimeSpan.FromSeconds(5),
            ct: ct);

        if (messagesResult.IsFailure)
            return Result.Failure<int>(messagesResult.Error);

        var processed = 0;

        foreach (var msg in messagesResult.Value)
        {
            var result = await ProcessSingleAsync(msg, ct);

            if (result.IsSuccess)
            {
                await receiver.CompleteMessageMonadicAsync(msg, ct);
                processed++;
            }
            else
            {
                // Abandon — message returns to queue for retry
                await receiver.AbandonMessageMonadicAsync(msg, ct);
            }
        }

        return Result.Success(processed);
    }

    private async Task<Result<Unit>> ProcessSingleAsync(
        ServiceBusReceivedMessage msg,
        CancellationToken ct)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(msg.Body);
            if (evt is null)
                return Result.Failure<Unit>(Error.Validation("Message.Body", "Could not deserialize event."));

            return await orders.ProcessCreatedEventAsync(evt, ct);
        }
        catch (JsonException ex)
        {
            return Result.Failure<Unit>(Error.Unexpected("Message.Deserialize", ex.Message));
        }
    }
}
```

## Dead-letter handling

Move a message to the dead-letter sub-queue with a human-readable reason:

```csharp
var dlqResult = await receiver.DeadLetterMessageMonadicAsync(
    message,
    reason: "InvalidPayload",
    ct: ct);

if (dlqResult.IsFailure)
    logger.LogError("Failed to dead-letter message {MessageId}: {Error}",
        message.MessageId, dlqResult.Error);
```

## Integration with MonadicSharp.Recovery for retry

```csharp
using MonadicSharp.Recovery;

var result = await Policy
    .HandleResult<Result<Unit>>(r => r.Error is AzureError.ServiceUnavailable or AzureError.Timeout)
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)))
    .ExecuteAsync(ct => sender.SendMessageMonadicAsync(message, ct));
```

Or using the built-in `RetryMonadicAsync` extension:

```csharp
var result = await sender
    .SendMessageMonadicAsync(message, ct)
    .RetryMonadicAsync(
        maxAttempts: 3,
        delay: TimeSpan.FromSeconds(1),
        shouldRetry: error => error is AzureError.RateLimit or AzureError.Timeout);
```

## Registration

```csharp
builder.Services.AddAzureServiceBus(options =>
{
    options.ConnectionString = configuration["ServiceBus:ConnectionString"];
    options.QueueName        = "orders";
});
```
