# MonadicSharp.Framework.Persistence

`MonadicSharp.Framework.Persistence` provides result-oriented repository and Unit of Work abstractions over EF Core 8. Database errors are typed `Result` failures — no try/catch required in application code.

## Install

```bash
dotnet add package MonadicSharp.Framework.Persistence
```

## Core types

### `IMonadicRepository<T>`

```csharp
public interface IMonadicRepository<T> where T : class
{
    Task<Option<T>> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken ct = default);
    Task<Result<T>> SaveAsync(T entity, CancellationToken ct = default);
    Task<Result<Unit>> DeleteAsync(Guid id, CancellationToken ct = default);
}
```

`FindByIdAsync` returns `Option<T>` — the absence of a record is not an error, it is an empty option. `SaveAsync` returns `Result<T>` because a write operation can fail with a typed persistence error.

### `MonadicDbContext`

Base class for EF Core 8 `DbContext`. Inheriting from it gives you `SaveChangesMonadicAsync`, which wraps `SaveChangesAsync` and maps EF exceptions to typed errors.

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : MonadicDbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
}
```

### `IUnitOfWork`

Coordinates multiple repository operations under a single transaction.

```csharp
public interface IUnitOfWork
{
    Task<Result<Unit>> CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
```

## Error types

| Error | When |
|---|---|
| `PersistenceError.NotFound` | Record not found during a write that requires existence |
| `PersistenceError.ConcurrencyConflict` | `DbUpdateConcurrencyException` |
| `PersistenceError.Duplicate` | Unique constraint violation |
| `PersistenceError.DatabaseUnavailable` | Connection failure |

## Example: UserRepository

```csharp
public interface IUserRepository : IMonadicRepository<User>
{
    Task<Option<User>> FindByEmailAsync(string email, CancellationToken ct = default);
}

public sealed class UserRepository(AppDbContext db)
    : MonadicRepository<User>(db), IUserRepository
{
    public async Task<Option<User>> FindByEmailAsync(
        string email,
        CancellationToken ct = default)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        return user is null ? Option.None<User>() : Option.Some(user);
    }
}
```

### Using the repository in a service

```csharp
public sealed class UserService(IUserRepository users, IUnitOfWork uow)
{
    public async Task<Result<User>> RegisterAsync(
        string email,
        string name,
        CancellationToken ct = default)
    {
        var existing = await users.FindByEmailAsync(email, ct);

        if (existing.HasValue)
            return Result.Fail<User>(
                new PersistenceError.Duplicate($"Email {email} is already registered"));

        var user = new User(Guid.NewGuid(), email, name, DateTimeOffset.UtcNow);

        return await users.SaveAsync(user, ct)
            .BindAsync(_ => uow.CommitAsync(ct))
            .MapAsync(_ => user);
    }

    public async Task<Result<User>> UpdateNameAsync(
        Guid userId,
        string newName,
        CancellationToken ct = default)
    {
        var option = await users.FindByIdAsync(userId, ct);

        return await option
            .ToResult(new PersistenceError.NotFound(userId.ToString()))
            .BindAsync(user =>
            {
                user.Name = newName;
                return users.SaveAsync(user, ct);
            })
            .BindAsync(_ => uow.CommitAsync(ct))
            .MapAsync(_ => option.Value);
    }
}
```

## Handling concurrency conflicts

`SaveAsync` maps `DbUpdateConcurrencyException` to `PersistenceError.ConcurrencyConflict`. Handle it explicitly in calling code:

```csharp
var result = await users.SaveAsync(entity, ct);

if (result.IsFailure && result.Error is PersistenceError.ConcurrencyConflict)
{
    // reload from database and retry
    var fresh = await users.FindByIdAsync(entity.Id, ct);
    // ...
}
```

## Registration

```csharp
builder.Services.AddPersistence<AppDbContext>(opts =>
{
    opts.UseNpgsql(connectionString);
})
.AddRepository<IUserRepository, UserRepository>();
```

The `AddPersistence` call registers `MonadicDbContext`, `IUnitOfWork`, and the base `IMonadicRepository<T>` open-generic implementation.
