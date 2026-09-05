// Examples — documentation only, not compiled.
// ReSharper disable All
#pragma warning disable CS0168, CS8019, CS1998
using MonadicSharp;
using MonadicSharp.Extensions;

namespace MonadicSharp.Examples;

// ============================================================
// 04 — REPOSITORY + DDD PATTERN
//      Clean domain layer with Option / Result at boundaries
// ============================================================
//
//  Contract:
//    - Repository methods return Option<T>   when absence is valid
//    - Service methods return Result<T>      when absence is an error
//    - Domain methods return Result<T>       for business rule violations
//    - No exceptions cross domain boundaries
// ============================================================

static class RepositoryDddExamples
{
    // ── Domain ────────────────────────────────────────────────────────────

    sealed class Money
    {
        public decimal Amount { get; }
        public string  Currency { get; }

        private Money(decimal amount, string currency) { Amount = amount; Currency = currency; }

        public static Result<Money> Create(decimal amount, string currency) =>
            amount < 0
                ? Error.Validation($"Amount cannot be negative: {amount}", field: "amount")
                : string.IsNullOrWhiteSpace(currency)
                    ? Error.Validation("Currency is required", field: "currency")
                    : new Money(amount, currency.ToUpperInvariant());

        public Result<Money> Add(Money other) =>
            Currency != other.Currency
                ? Error.Validation($"Cannot add {Currency} to {other.Currency}")
                : new Money(Amount + other.Amount, Currency);

        public override string ToString() => $"{Amount:F2} {Currency}";
    }

    sealed class Account
    {
        public Guid   Id       { get; }
        public string OwnerId  { get; }
        public Money  Balance  { get; private set; }
        public bool   IsActive { get; private set; }

        private Account(Guid id, string ownerId, Money balance)
        {
            Id       = id;
            OwnerId  = ownerId;
            Balance  = balance;
            IsActive = true;
        }

        public static Result<Account> Open(string ownerId, Money initialBalance) =>
            string.IsNullOrWhiteSpace(ownerId)
                ? Error.Validation("OwnerId is required")
                : new Account(Guid.NewGuid(), ownerId, initialBalance);

        public Result<Account> Deposit(Money amount)
        {
            if (!IsActive)
                return Error.Validation("Cannot deposit to a closed account");

            return Balance.Add(amount)
                .Map(newBalance => { Balance = newBalance; return this; });
        }

        public Result<Account> Withdraw(Money amount)
        {
            if (!IsActive)
                return Error.Validation("Cannot withdraw from a closed account");

            if (amount.Amount > Balance.Amount)
                return Error.Validation($"Insufficient funds: balance {Balance}, requested {amount}");

            return Balance.Add(new Money(-amount.Amount, amount.Currency) as Money ?? throw new())
                .Map(b => { Balance = b; return this; });
        }

        public Result<Account> Close() =>
            IsActive
                ? Result<Account>.Success(this).Do(_ => IsActive = false)
                : Error.Conflict("Account is already closed");
    }

    // ── Repository contracts (return Option) ──────────────────────────────

    interface IAccountRepository
    {
        Option<Account>           FindById(Guid id);
        Option<Account>           FindByOwner(string ownerId);
        IEnumerable<Account>      FindAll();
        Task<Option<Account>>     FindByIdAsync(Guid id);
        Result<Unit>              Save(Account account);
    }

    // ── Application service (converts Option → Result at the boundary) ────

    class AccountService
    {
        private readonly IAccountRepository _repo;

        public AccountService(IAccountRepository repo) => _repo = repo;

        public Result<Account> OpenAccount(string ownerId, decimal amount, string currency) =>
            Money.Create(amount, currency)
                .Bind(money => Account.Open(ownerId, money))
                .Do(account  => _repo.Save(account));

        public Result<Account> Deposit(Guid accountId, decimal amount, string currency) =>
            _repo.FindById(accountId)
                 .ToResult(Error.NotFound("Account", accountId.ToString()))
                 .Bind(account => Money.Create(amount, currency)
                     .Bind(account.Deposit))
                 .Do(account => _repo.Save(account));

        public Result<Account> Withdraw(Guid accountId, decimal amount, string currency) =>
            _repo.FindById(accountId)
                 .ToResult(Error.NotFound("Account", accountId.ToString()))
                 .Bind(account => Money.Create(amount, currency)
                     .Bind(account.Withdraw))
                 .Do(account => _repo.Save(account));

        // Async pipeline variant
        public async Task<Result<Account>> TransferAsync(Guid fromId, Guid toId, decimal amount, string currency)
        {
            var moneyResult = Money.Create(amount, currency);
            if (moneyResult.IsFailure) return Result<Account>.Failure(moneyResult.Error);
            var money = moneyResult.Value;

            var from = await _repo.FindByIdAsync(fromId);
            if (from.IsNone) return Result<Account>.Failure(Error.NotFound("Source account", fromId.ToString()));

            var to = await _repo.FindByIdAsync(toId);
            if (to.IsNone) return Result<Account>.Failure(Error.NotFound("Destination account", toId.ToString()));

            return from.GetValueOrDefault(null!)
                .Withdraw(money)
                .Bind(src => to.GetValueOrDefault(null!)
                    .Deposit(money)
                    .Do(_ => _repo.Save(src))
                    .Do(dst => _repo.Save(dst)));
        }
    }

    // ── Controller / API adapter ───────────────────────────────────────────

    static string HandleDeposit(AccountService svc, Guid id, decimal amount)
    {
        return svc.Deposit(id, amount, "EUR")
            .Match(
                onSuccess: acc => $"New balance: {acc.Balance}",
                onFailure: err => err.Type switch
                {
                    ErrorType.NotFound   => $"404: {err.Message}",
                    ErrorType.Validation => $"400: {err.Message}",
                    ErrorType.Conflict   => $"409: {err.Message}",
                    _                    => $"500: {err.Message}"
                }
            );
    }
}
