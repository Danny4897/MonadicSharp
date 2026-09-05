// Examples — documentation only, not compiled.
// ReSharper disable All
#pragma warning disable CS0168, CS8019, CS1998
using MonadicSharp;
using MonadicSharp.Extensions;

namespace MonadicSharp.Examples;

// ============================================================
// 02 — OPTION<T>  ·  Null-safety without null
// ============================================================
//
//  Option<T> = Some(T value) | None
//
//  Eliminates NullReferenceException at the type system level.
//  Use when absence is a valid business concept (not an error).
//  Use Result<T> when absence means something went wrong.
//
//  Key operators:
//    Map<TOut>(Func<T, TOut>)          — transform value, propagate None
//    Bind<TOut>(Func<T, Option<TOut>>) — chain optional step
//    Filter(Func<T, bool>)             — None if predicate fails
//    GetValueOrDefault(T fallback)     — safe unwrap
//    Match(onSome, onNone)             — exhaustive match
//    Do(Action<T>)                     — side-effect when Some
//    ToResult(Error)                   — convert to Result<T> when None = error
// ============================================================

static class OptionExamples
{
    record User(int Id, string Name, string? Phone, Address? Address);
    record Address(string City, string? PostalCode);
    record Product(int Id, string Name, decimal? DiscountPercent);

    // ── 2a. Creating options ───────────────────────────────────────────────

    static Option<User> FindUser(int id)
    {
        var db = new Dictionary<int, User>
        {
            [1] = new(1, "Alice", "+39 02 1234567", new("Milano", "20121")),
            [2] = new(2, "Bob",   null,             null)
        };
        return Option<User>.From(db.GetValueOrDefault(id));  // null-safe From
    }

    // Implicit conversion T → Option<T>
    static Option<string> GetConfigValue(string key) =>
        Environment.GetEnvironmentVariable(key);  // null → None automatically

    // ── 2b. Map — safe property access chains ─────────────────────────────

    static void MapChain()
    {
        var city = FindUser(1)
            .Map(u   => u.Address)          // Option<Address?>
            .Bind(a  => Option<Address>.From(a))  // collapse nullable
            .Map(a   => a.City);            // Option<string>

        string display = city.GetValueOrDefault("Unknown city");
        // Output: "Milano" for user 1, "Unknown city" for user 2
    }

    // ── 2c. Bind — chaining optional lookups ──────────────────────────────

    static readonly Dictionary<string, int> _emailIndex = new()
    {
        ["alice@example.com"] = 1
    };

    static Option<User> FindByEmail(string email) =>
        _emailIndex.TryGetValue(email, out var id) ? FindUser(id) : Option<User>.None;

    static Option<string> GetUserCity(string email) =>
        FindByEmail(email)
            .Bind(u => Option<Address>.From(u.Address))
            .Map(a => a.City);

    // ── 2d. Filter — conditional options ──────────────────────────────────

    static Option<Product> GetProductWithDiscount(int id)
    {
        var products = new Dictionary<int, Product>
        {
            [1] = new(1, "Widget", 15m),
            [2] = new(2, "Gadget", null)
        };

        return Option<Product>.From(products.GetValueOrDefault(id))
            .Filter(p => p.DiscountPercent.HasValue);  // None if no discount
    }

    // ── 2e. Match — exhaustive handling ───────────────────────────────────

    static string DescribeUser(int id) =>
        FindUser(id).Match(
            onSome: u    => $"{u.Name} ({(u.Phone ?? "no phone")})",
            onNone: ()   => "User not found"
        );

    // ── 2f. ToResult — convert absence to an error ────────────────────────

    static Result<User> RequireUser(int id) =>
        FindUser(id).ToResult(Error.NotFound("User", id.ToString()));

    // Compose Option chains then convert at the boundary
    static Result<string> GetCityOrFail(string email) =>
        GetUserCity(email)
            .ToResult($"No city found for {email}");

    // ── 2g. Option in repository pattern ──────────────────────────────────

    interface IUserRepository
    {
        Option<User>         FindById(int id);
        Option<User>         FindByEmail(string email);
        IEnumerable<User>    FindAll();
    }

    // Usage: callers decide what "None" means in their context
    static async Task<Result<string>> GetWelcomeMessage(int id, IUserRepository repo) =>
        repo.FindById(id)
            .Map(u => $"Welcome back, {u.Name}!")
            .ToResult(Error.NotFound("User", id.ToString()))
            .Map(msg => msg.ToUpper())
            .AsTask();

    // ── 2h. FirstOrNone — LINQ-style with Option ──────────────────────────

    static Option<User> FindFirstWithPhone(IEnumerable<User> users) =>
        users
            .Select(u => Option<User>.From(u.Phone != null ? u : null))
            .FirstOrNone();  // from OptionExtensions

    // ── 2i. Sequence — all-or-nothing over a list of Options ──────────────

    static void SequenceExample()
    {
        var ids  = new[] { 1, 2 };
        var opts = ids.Select(FindUser);

        Option<IEnumerable<User>> all = opts.Sequence(); // None if any id missing
        all.Match(
            onSome: users => Console.WriteLine($"All found: {users.Count()}"),
            onNone: ()    => Console.WriteLine("At least one user missing")
        );
    }

    // ── 2j. Async Option chains ────────────────────────────────────────────

    static async Task<Option<User>> FindUserAsync(int id) =>
        await Task.FromResult(FindUser(id));

    static async Task<string> GetCityAsync(int id) =>
        await FindUserAsync(id)
            .Map(u => Option<Address>.From(u.Address))
            .BindAsync(async a =>
            {
                await Task.Delay(1); // e.g. geocode lookup
                return Option<string>.Some(a.City);
            })
            .Map(c => c.GetValueOrDefault("Unknown"));
}
