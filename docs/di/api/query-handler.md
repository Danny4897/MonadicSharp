# IQueryHandler\<TQuery, TResult\>

`IQueryHandler<TQuery, TResult>` is the interface for read-only operations. Implement it to handle a specific `IQuery<TResult>`.

## Interface signature

```csharp
public interface IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken ct);
}
```

`TQuery` must implement `IQuery<TResult>`. The handler returns `Task<Result<TResult>>` — never throws, never returns null.

## DI registration and scoping

Handlers are registered automatically by `AddMonadicMediator`:

```csharp
builder.Services.AddMonadicMediator(typeof(Program).Assembly);
```

**Recommended lifetime: Scoped.** This matches the default HTTP request scope and allows injecting scoped services (DbContext, repositories). If you need a singleton handler, ensure all its dependencies are also singleton-safe.

```csharp
// Scoped is the default — no attribute or explicit registration needed
public class GetUserHandler(AppDbContext db) : IQueryHandler<GetUserQuery, UserDto>
{
    // db is injected as Scoped, same lifetime as this handler
}
```

## Example with repository

```csharp
public record GetUserQuery(Guid UserId) : IQuery<UserDto>;

public class GetUserHandler(IUserRepository repo) : IQueryHandler<GetUserQuery, UserDto>
{
    public async Task<Result<UserDto>> HandleAsync(GetUserQuery query, CancellationToken ct)
    {
        var user = await repo.FindByIdAsync(query.UserId, ct);

        return user.Map(u => new UserDto(u.Id, u.Name, u.Email));
        // user is Option<User> — Map returns Result<UserDto>
        // Option.None becomes Result.Failure(Error.NotFound(...))
    }
}
```

### Projecting with LINQ (EF Core)

```csharp
public class GetOrderSummaryHandler(AppDbContext db)
    : IQueryHandler<GetOrderSummaryQuery, OrderSummaryDto>
{
    public async Task<Result<OrderSummaryDto>> HandleAsync(
        GetOrderSummaryQuery query,
        CancellationToken ct)
    {
        var dto = await db.Orders
            .Where(o => o.Id == query.OrderId && o.UserId == query.UserId)
            .Select(o => new OrderSummaryDto(o.Id, o.Status, o.Total, o.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return dto is null
            ? Result.Failure<OrderSummaryDto>(Error.NotFound("Order", query.OrderId))
            : Result.Success(dto);
    }
}
```

## Dispatching via IMediator

```csharp
// In a controller
public async Task<IActionResult> Get(Guid id, IMediator mediator, CancellationToken ct)
{
    var result = await mediator.QueryAsync(new GetUserQuery(id), ct);

    return result.Match(
        onSuccess: dto   => Ok(dto),
        onFailure: error => error.ToActionResult());
}
```

## Testing

Query handlers have no side effects — unit tests are straightforward. Inject a mock or in-memory repository.

```csharp
public class GetUserHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingUser_ReturnsDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user   = new User(userId, "Alice", "alice@example.com");

        var repo = Substitute.For<IUserRepository>();
        repo.FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Option.Some(user));

        var handler = new GetUserHandler(repo);

        // Act
        var result = await handler.HandleAsync(new GetUserQuery(userId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task HandleAsync_MissingUser_ReturnsNotFound()
    {
        var repo = Substitute.For<IUserRepository>();
        repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Option.None<User>());

        var handler = new GetUserHandler(repo);
        var result  = await handler.HandleAsync(new GetUserQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.NotFound);
    }
}
```
