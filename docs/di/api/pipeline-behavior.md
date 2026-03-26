# IPipelineBehavior\<TRequest, TResult\>

`IPipelineBehavior<TRequest, TResult>` is the extension point for cross-cutting concerns. Behaviors wrap every handler invocation in a chain.

## Interface signature

```csharp
public interface IPipelineBehavior<TRequest, TResult>
{
    Task<Result<TResult>> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken ct);
}

// next is:
public delegate Task<Result<TResult>> RequestHandlerDelegate<TResult>();
```

Calling `await next()` continues to the next behavior or the handler. Not calling `next()` short-circuits the pipeline and returns immediately.

## Short-circuiting

Return a failure (or even a success) without calling `next()` to stop the pipeline. The handler is never invoked.

```csharp
// Early return — handler is never called
if (isInvalid)
    return Result.Failure<TResult>(Error.Validation(...));

// Continue normally
return await next();
```

## Complete example — ValidationBehavior with FluentValidation

This behavior applies to all commands (`ICommand<TResult>`). It runs all registered validators and aggregates errors into a single `ValidationError`.

```csharp
using FluentValidation;
using MonadicSharp;

public class ValidationBehavior<TRequest, TResult>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResult>
    where TRequest : ICommand<TResult>
{
    public async Task<Result<TResult>> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken ct)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, ct)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
        {
            var details = failures
                .Select(f => new FieldError(f.PropertyName, f.ErrorMessage))
                .ToList();

            return Result.Failure<TResult>(Error.Validation(details));
        }

        return await next();
    }
}
```

### Corresponding FluentValidation validator

```csharp
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item.")
            .ForEach(item => item
                .ChildRules(i =>
                {
                    i.RuleFor(x => x.ProductId).NotEmpty();
                    i.RuleFor(x => x.Quantity).GreaterThan(0);
                }));

        RuleFor(x => x.ShippingAddress)
            .NotEmpty().MaximumLength(500);
    }
}
```

### Registration

```csharp
// Register FluentValidation validators
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Register the behavior (open-generic — covers all ICommand<T>)
builder.Services.AddBehavior(typeof(ValidationBehavior<,>));
```

## Example — async authorization behavior

Behaviors can inject scoped services, including `IHttpContextAccessor` or a custom `ICurrentUser`.

```csharp
public class AuthorizationBehavior<TRequest, TResult>(ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResult>
    where TRequest : IAuthorizedRequest
{
    public async Task<Result<TResult>> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken ct)
    {
        var authorized = await currentUser.HasPermissionAsync(request.RequiredPermission, ct);

        if (!authorized)
            return Result.Failure<TResult>(Error.Forbidden("User lacks required permission."));

        return await next();
    }
}
```

## Example — transaction behavior

Wraps a command in a database transaction. Commits on success, rolls back on failure.

```csharp
public class TransactionBehavior<TRequest, TResult>(AppDbContext db)
    : IPipelineBehavior<TRequest, TResult>
    where TRequest : ICommand<TResult>
{
    public async Task<Result<TResult>> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var result = await next();

        if (result.IsSuccess)
            await tx.CommitAsync(ct);
        else
            await tx.RollbackAsync(ct);

        return result;
    }
}
```
