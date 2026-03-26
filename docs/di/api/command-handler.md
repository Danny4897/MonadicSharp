# ICommandHandler\<TCommand, TResult\>

`ICommandHandler<TCommand, TResult>` is the interface for operations with side effects. Implement it to handle a specific `ICommand<TResult>`.

## Interface signature

```csharp
public interface ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct);
}
```

Use `ICommand<Unit>` (and `ICommandHandler<TCommand, Unit>`) when the operation returns no value — only success or failure.

## Differences from IQueryHandler

| | IQueryHandler | ICommandHandler |
|---|---|---|
| Side effects | None | Expected (DB writes, events, emails…) |
| Retryable | Yes | Depends on idempotency design |
| Cacheable | Yes | No |
| Typical result type | DTO / domain object | Created entity or `Unit` |
| Typical pipeline | Logging, Caching | Logging, Validation, Transaction |

## Example — create with Unit of Work

```csharp
public record CreateProductCommand(
    string Name,
    decimal Price,
    string Sku) : ICommand<Product>;

public class CreateProductHandler(
    IProductRepository products,
    IUnitOfWork uow) : ICommandHandler<CreateProductCommand, Product>
{
    public async Task<Result<Product>> HandleAsync(CreateProductCommand cmd, CancellationToken ct)
    {
        // Check for duplicate SKU
        var existing = await products.FindBySkuAsync(cmd.Sku, ct);
        if (existing.IsSome)
            return Result.Failure<Product>(Error.Conflict("Product.DuplicateSku", cmd.Sku));

        var product = Product.Create(cmd.Name, cmd.Price, cmd.Sku);
        await products.AddAsync(product, ct);
        await uow.SaveChangesAsync(ct);

        return Result.Success(product);
    }
}
```

## Example — command that returns Unit

```csharp
public record DeleteProductCommand(Guid ProductId) : ICommand<Unit>;

public class DeleteProductHandler(
    IProductRepository products,
    IUnitOfWork uow) : ICommandHandler<DeleteProductCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(DeleteProductCommand cmd, CancellationToken ct)
    {
        var product = await products.FindByIdAsync(cmd.ProductId, ct);

        return await product
            .ToResult(Error.NotFound("Product", cmd.ProductId))
            .BindAsync(async p =>
            {
                products.Remove(p);
                await uow.SaveChangesAsync(ct);
                return Result.Success(Unit.Value);
            });
    }
}
```

## Composing with Bind

`Bind` chains fallible steps without nested if-statements. Each step only runs if the previous succeeded.

```csharp
public class TransferFundsHandler(
    IAccountRepository accounts,
    ITransactionRepository transactions,
    IUnitOfWork uow) : ICommandHandler<TransferFundsCommand, Transaction>
{
    public Task<Result<Transaction>> HandleAsync(TransferFundsCommand cmd, CancellationToken ct) =>
        accounts.FindByIdAsync(cmd.SourceAccountId, ct)
            .BindAsync(source  => ValidateFunds(source, cmd.Amount))
            .BindAsync(source  => accounts.FindByIdAsync(cmd.DestinationAccountId, ct)
                                    .MapAsync(dest => (source, dest)))
            .BindAsync(pair    => ExecuteTransferAsync(pair.source, pair.dest, cmd.Amount, ct))
            .TapAsync(_        => uow.SaveChangesAsync(ct));

    private static Result<Account> ValidateFunds(Account account, decimal amount) =>
        account.Balance >= amount
            ? Result.Success(account)
            : Result.Failure<Account>(Error.BusinessRule("Account.InsufficientFunds"));

    private async Task<Result<Transaction>> ExecuteTransferAsync(
        Account source, Account destination, decimal amount, CancellationToken ct)
    {
        source.Debit(amount);
        destination.Credit(amount);
        var tx = Transaction.Create(source.Id, destination.Id, amount);
        await transactions.AddAsync(tx, ct);
        return Result.Success(tx);
    }
}
```

## Dispatching via IMediator

```csharp
[HttpPost]
public async Task<IActionResult> CreateProduct(
    CreateProductRequest req,
    IMediator mediator,
    CancellationToken ct)
{
    var result = await mediator.CommandAsync(
        new CreateProductCommand(req.Name, req.Price, req.Sku), ct);

    return result.Match(
        onSuccess: product => CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product),
        onFailure: error   => error.ToActionResult());
}
```
