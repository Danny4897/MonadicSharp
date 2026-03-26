# MonadicSharp.Azure.Functions

`MonadicSharp.Azure.Functions` provides helpers for writing Azure Functions v4 HTTP triggers that return `Result<T>`. Deserialization, validation, and error mapping all produce typed failures instead of exceptions.

## Install

```bash
dotnet add package MonadicSharp.Azure.Functions
```

**Requires**: Azure Functions v4 (isolated worker model), .NET 8.0+.

## Core methods

```csharp
// Deserialize JSON body — returns Result<T>, never throws
Task<Result<T>> DeserializeBodyAsync<T>(this HttpRequest req, CancellationToken ct = default);

// Map Result<T> to IActionResult with ProblemDetails on failure
Task<IActionResult> ToActionResultAsync<T>(this Task<Result<T>> resultTask);
IActionResult ToActionResult<T>(this Result<T> result);
```

## Minimal HTTP trigger

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using MonadicSharp.Azure.Functions;

public class CreateOrderFunction(IOrderService orderService)
{
    [Function("CreateOrder")]
    public Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequest req,
        CancellationToken ct) =>
        req.DeserializeBodyAsync<CreateOrderRequest>(ct)
            .BindAsync(r => orderService.CreateAsync(r, ct))
            .ToActionResultAsync();
}
```

`ToActionResultAsync` maps:
- `Result.Success(order)` → `200 OK` with JSON body
- `Result.Failure(Error.Validation(...))` → `422 Unprocessable Entity` (ProblemDetails)
- `Result.Failure(AzureError.NotFound(...))` → `404 Not Found` (ProblemDetails)
- Any other failure → `500 Internal Server Error` (ProblemDetails)

## Full example — with validation and binding

```csharp
public class UpdateProductFunction(
    IProductService products,
    IValidator<UpdateProductRequest> validator)
{
    [Function("UpdateProduct")]
    public Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "products/{id:guid}")] HttpRequest req,
        Guid id,
        CancellationToken ct) =>
        req.DeserializeBodyAsync<UpdateProductRequest>(ct)
            .BindAsync(r => ValidateAsync(r, ct))
            .BindAsync(r => products.UpdateAsync(id, r, ct))
            .ToActionResultAsync();

    private async Task<Result<UpdateProductRequest>> ValidateAsync(
        UpdateProductRequest req, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(req, ct);

        return validation.IsValid
            ? Result.Success(req)
            : Result.Failure<UpdateProductRequest>(
                Error.Validation(validation.Errors
                    .Select(e => new FieldError(e.PropertyName, e.ErrorMessage))));
    }
}
```

## Reading route and query parameters

Route parameters arrive as method parameters and are already parsed by the Functions runtime. Query string parameters require reading from `req.Query`:

```csharp
[Function("SearchOrders")]
public Task<IActionResult> RunAsync(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders")] HttpRequest req,
    CancellationToken ct)
{
    var userId = req.Query["userId"].FirstOrDefault();
    if (!Guid.TryParse(userId, out var parsedId))
        return Task.FromResult(
            Result.Failure<List<OrderDto>>(Error.Validation("UserId", "Invalid GUID format."))
                .ToActionResult());

    return orderService.GetByUserAsync(parsedId, ct).ToActionResultAsync();
}
```

## Returning 201 Created

`ToActionResultAsync` returns 200 by default for success. Override with `ToCreatedResultAsync`:

```csharp
public Task<IActionResult> RunAsync(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "products")] HttpRequest req,
    CancellationToken ct) =>
    req.DeserializeBodyAsync<CreateProductRequest>(ct)
        .BindAsync(r => productService.CreateAsync(r, ct))
        .ToCreatedResultAsync(product => $"/products/{product.Id}");
        // → 201 Created with Location header
```

## Error handling in practice

Because every step returns `Result<T>`, a failure at any point in the chain short-circuits the rest. The final `ToActionResultAsync` converts the failure to the appropriate HTTP response.

```csharp
// Chain: deserialize → validate → authorize → execute
req.DeserializeBodyAsync<PlaceOrderRequest>(ct)        // fails → 400/422
    .BindAsync(r  => validator.ValidateAsync(r, ct))   // fails → 422
    .BindAsync(r  => authService.AuthorizeAsync(r, ct))// fails → 403
    .BindAsync(r  => orderService.PlaceAsync(r, ct))   // fails → domain error
    .ToActionResultAsync();                             // all failures → ProblemDetails
```
