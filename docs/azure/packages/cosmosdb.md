# MonadicSharp.Azure.CosmosDb

`MonadicSharp.Azure.CosmosDb` extends `Container` with `*MonadicAsync` methods that return `Result<T>` or `Option<T>` instead of throwing `CosmosException`.

## Install

```bash
dotnet add package MonadicSharp.Azure.CosmosDb
```

## Methods

```csharp
// Read — returns Option<T> because NotFound is not an error
Task<Option<T>>     ReadItemMonadicAsync<T>(string id, PartitionKey pk, CancellationToken ct = default);

// Upsert — returns Result<T> because failure is unexpected
Task<Result<T>>     UpsertItemMonadicAsync<T>(T item, PartitionKey pk, CancellationToken ct = default);

// Create
Task<Result<T>>     CreateItemMonadicAsync<T>(T item, PartitionKey pk, CancellationToken ct = default);

// Replace (with optional ETag check)
Task<Result<T>>     ReplaceItemMonadicAsync<T>(T item, string id, PartitionKey pk,
                        string? etag = null, CancellationToken ct = default);

// Delete
Task<Result<Unit>>  DeleteItemMonadicAsync(string id, PartitionKey pk, CancellationToken ct = default);

// Query — returns Result<List<T>>
Task<Result<List<T>>> QueryMonadicAsync<T>(QueryDefinition query, CancellationToken ct = default);
```

## Option\<T\> vs Result\<T\> for NotFound

`ReadItemMonadicAsync` returns `Option<T>` because "item not found" is a normal, expected outcome — not an error. The calling code handles both cases explicitly.

```csharp
var item = await container.ReadItemMonadicAsync<Order>(orderId, new PartitionKey(userId));

// Option pattern
return item.Match(
    onSome: order => Result.Success(order),
    onNone: ()    => Result.Failure<Order>(Error.NotFound("Order", orderId)));

// Or use the Option → Result conversion
return item.ToResult(Error.NotFound("Order", orderId));
```

In contrast, `UpsertItemMonadicAsync` returns `Result<T>` because an upsert failure (conflict, rate limit, network error) is always unexpected.

## CRUD example

```csharp
public class OrderRepository(Container container)
{
    public Task<Option<Order>> GetAsync(string orderId, string userId) =>
        container.ReadItemMonadicAsync<Order>(orderId, new PartitionKey(userId));

    public Task<Result<Order>> CreateAsync(Order order) =>
        container.CreateItemMonadicAsync(order, new PartitionKey(order.UserId));

    public Task<Result<Order>> UpdateAsync(Order order) =>
        container.UpsertItemMonadicAsync(order, new PartitionKey(order.UserId));

    public Task<Result<Unit>> DeleteAsync(string orderId, string userId) =>
        container.DeleteItemMonadicAsync(orderId, new PartitionKey(userId));

    public Task<Result<List<Order>>> GetByUserAsync(string userId, CancellationToken ct) =>
        container.QueryMonadicAsync<Order>(
            new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
                .WithParameter("@userId", userId),
            ct);
}
```

## Optimistic concurrency with ETag

CosmosDB uses ETags for optimistic concurrency control. Pass the ETag from the read to the replace:

```csharp
public async Task<Result<Order>> UpdateStatusAsync(
    string orderId,
    string userId,
    OrderStatus newStatus,
    CancellationToken ct)
{
    // Read with ETag
    var option = await container.ReadItemMonadicAsync<Order>(
        orderId, new PartitionKey(userId), ct);

    var order = option.ToResult(Error.NotFound("Order", orderId));

    return await order.BindAsync(async o =>
    {
        var etag    = o._etag; // populated by CosmosDB response
        var updated = o with { Status = newStatus };

        // Replace only succeeds if ETag matches (412 → AzureError.ConcurrencyConflict otherwise)
        return await container.ReplaceItemMonadicAsync(
            updated, orderId, new PartitionKey(userId), etag: etag, ct: ct);
    });
}
```

If another process modifies the item between the read and the replace, `ReplaceItemMonadicAsync` returns `Result.Failure(AzureError.ConcurrencyConflict(...))` — no exception, no catch block needed.

## Handling ConcurrencyConflict at the controller

```csharp
var result = await orderRepo.UpdateStatusAsync(id, userId, OrderStatus.Shipped, ct);

return result.Match(
    onSuccess: order => Ok(order),
    onFailure: error => error switch
    {
        AzureError.ConcurrencyConflict => Conflict("Order was modified concurrently. Please retry."),
        AzureError.NotFound            => NotFound(),
        _                              => StatusCode(500)
    });
```

## Batch operations

```csharp
public async Task<Result<List<Order>>> CreateBatchAsync(
    List<Order> orders,
    string partitionKeyValue,
    CancellationToken ct)
{
    var batch = container.CreateTransactionalBatch(new PartitionKey(partitionKeyValue));
    foreach (var order in orders)
        batch.CreateItem(order);

    using var response = await batch.ExecuteAsync(ct);

    return response.IsSuccessStatusCode
        ? Result.Success(orders)
        : Result.Failure<List<Order>>(AzureError.Unexpected.FromStatus((int)response.StatusCode));
}
```
