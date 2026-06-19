// Examples — documentation only, not compiled.
// ReSharper disable All
#pragma warning disable CS0168, CS8019, CS1998
using MonadicSharp;
using MonadicSharp.Extensions;

namespace MonadicSharp.Examples;

// ============================================================
// 01 — RESULT<T>  ·  Replace try/catch with typed outcomes
// ============================================================
//
//  Result<T> is a discriminated union of:
//    • Success(T value)   — operation completed, value is available
//    • Failure(Error)     — operation failed, error carries code/type/metadata
//
//  Key properties:
//    IsSuccess / IsFailure
//    Value   — throws if failure  (guard with IsSuccess)
//    Error   — throws if success  (guard with IsFailure)
//
//  Key operators:
//    Map<TOut>(Func<T, TOut>)             — transform value, propagate failure
//    Bind<TOut>(Func<T, Result<TOut>>)    — chain fallible step
//    MapError(Func<Error, Error>)         — transform error, propagate success
//    Match<TOut>(onSuccess, onFailure)    — exhaustive pattern match
//    Do(Action<T>)                        — side-effect on success
//    DoError(Action<Error>)               — side-effect on failure
//    GetValueOrDefault(T fallback)        — safe unwrap
// ============================================================

static class BasicResultExamples
{
    // ── 1a. Creating results ───────────────────────────────────────────────

    static Result<int> Divide(int a, int b) =>
        b == 0
            ? Result<int>.Failure(Error.Validation("Division by zero", field: "b"))
            : Result<int>.Success(a / b);

    // Implicit conversions (T → Result<T>, Error → Result<T>)
    static Result<string> Validate(string input) =>
        string.IsNullOrWhiteSpace(input)
            ? Error.Validation("Input is required")   // implicitly wraps Error
            : input.Trim();                           // implicitly wraps string

    // ── 1b. Map — transform without changing the track ────────────────────

    static void MapExample()
    {
        var result = Divide(10, 2)
            .Map(x => x * 2)          // 10 → only runs on success
            .Map(x => $"Result: {x}"); // "Result: 10"

        // Failure short-circuits through the chain
        var failed = Divide(10, 0)
            .Map(x => x * 2)           // skipped
            .Map(x => $"Result: {x}"); // skipped → still Failure
    }

    // ── 1c. Bind — chain steps that can each fail ─────────────────────────

    record User(int Id, string Email);
    record Order(int UserId, decimal Amount);

    static Result<User> FindUser(int id) =>
        id > 0 ? new User(id, "user@example.com") : Error.NotFound("User", id.ToString());

    static Result<Order> GetLatestOrder(User user) =>
        user.Id == 1
            ? new Order(1, 99.90m)
            : Error.NotFound("Order", $"user:{user.Id}");

    static Result<string> GetOrderSummary(int userId) =>
        FindUser(userId)
            .Bind(GetLatestOrder)
            .Map(o => $"Order #{o.UserId}: {o.Amount:C}");

    // ── 1d. Match — exhaustive pattern matching ────────────────────────────

    static string Describe(Result<int> r) =>
        r.Match(
            onSuccess: v  => $"Got {v}",
            onFailure: e  => $"[{e.Type}] {e.Code}: {e.Message}"
        );

    // ── 1e. Do / DoError — side-effects in a pipeline ─────────────────────

    static async Task LoggingPipeline(int userId)
    {
        var result = await Task.FromResult(FindUser(userId))
            .Do(u =>
            {
                Console.WriteLine($"Processing user {u.Id}");
                return Task.CompletedTask;
            })
            .Bind(u => Task.FromResult(GetLatestOrder(u)))
            .DoAsync(o => LogOrderAsync(o));

        result
            .Do(o    => Console.WriteLine($"  Order amount: {o.Amount:C}"))
            .DoError(e => Console.WriteLine($"  Failed: {e.Message}"));
    }

    static Task LogOrderAsync(Order o) { Console.WriteLine($"Order saved: {o.UserId}"); return Task.CompletedTask; }

    // ── 1f. Try — wrapping exception-prone code ────────────────────────────

    static Result<int> ParseInt(string s) =>
        Try.Execute(() => int.Parse(s));   // FormatException → Failure(Error.Exception)

    static async Task<Result<byte[]>> ReadFileAsync(string path) =>
        await Try.ExecuteAsync(() => File.ReadAllBytesAsync(path));

    // ── 1g. Combine — all-or-nothing aggregation ──────────────────────────

    static Result<string[]> ValidateAll(string name, string email, int age)
    {
        return Result.Combine(
            Validate(name),
            Validate(email),
            age >= 0 ? age.ToString() : Error.Validation("Age must be non-negative")
        );
        // If any fail, returns Failure(Error.Combine(allFailures))
        // If all succeed, returns Success(string[])
    }

    // ── 1h. GetValueOrDefault — safe unwrapping ───────────────────────────

    static void SafeUnwrap()
    {
        var r = Divide(10, 0);

        int value1 = r.GetValueOrDefault(0);        // 0
        int value2 = r.GetValueOrDefault(() => -1); // -1 from factory
    }

    // ── 1i. MapError — enriching errors ───────────────────────────────────

    static Result<User> FindUserWithContext(int id) =>
        FindUser(id)
            .MapError(e => e
                .WithMetadata("requestedId", id)
                .WithMetadata("timestamp", DateTimeOffset.UtcNow));
}
