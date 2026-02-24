// ReSharper disable All
// These examples are for documentation purposes only — not meant to be run directly.
#pragma warning disable CS8019

using MonadicSharp;
using MonadicSharp.Extensions;

namespace MonadicSharp.Examples;

/// <summary>
/// Real-world usage examples for MonadicSharp.
/// Each region is a self-contained scenario.
/// </summary>
public static class MonadicSharpExamples
{
    // ── 1. Basic Result — replacing try/catch ────────────────────────────────

    record User(int Id, string Name, string Email);

    static Result<User> FindUser(int id)
    {
        if (id <= 0)
            return Error.Validation("Id must be positive", field: "id");

        if (id == 404)
            return Error.NotFound("User", identifier: id.ToString());

        return new User(id, "Dany", "dany@example.com");
    }

    static string HandleUser(int id)
    {
        return FindUser(id).Match(
            user => $"Found: {user.Name}",
            error => $"Error [{error.Type}]: {error.Message}"
        );
    }

    // ── 2. Chaining with Bind — multi-step validation ────────────────────────

    static Result<string> ValidateName(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? Result<string>.Failure(Error.Validation("Name cannot be empty"))
            : Result<string>.Success(name.Trim());

    static Result<string> ValidateEmail(string email) =>
        email.Contains('@')
            ? Result<string>.Success(email.ToLower())
            : Result<string>.Failure(Error.Validation("Invalid email format", field: "email"));

    static Result<User> CreateUser(string name, string email)
    {
        return ValidateName(name)
            .Bind(validName => ValidateEmail(email)
                .Map(validEmail => new User(1, validName, validEmail)));
    }

    // ── 3. Option — null-safe repository pattern ────────────────────────────

    static readonly Dictionary<int, User> _db = new() { [1] = new User(1, "Dany", "dany@example.com") };

    static Option<User> GetById(int id) =>
        _db.TryGetValue(id, out var user) ? Option<User>.Some(user) : Option<User>.None;

    static string GetUserDisplayName(int id) =>
        GetById(id)
            .Map(u => u.Name.ToUpper())
            .GetValueOrDefault("Anonymous");

    // ── 4. Try — wrapping exception-prone operations ─────────────────────────

    static Result<int> ParseSafely(string input) =>
        Try.Execute(() => int.Parse(input));

    static async Task<Result<string>> FetchDataSafely(string url) =>
        await Try.ExecuteAsync(async () =>
        {
            using var client = new HttpClient();
            return await client.GetStringAsync(url);
        });

    // ── 5. Pipeline — async multi-step processing ────────────────────────────

    static async Task<Result<int>> GetOrderAsync(int orderId)
    {
        await Task.Delay(1);
        return orderId > 0
            ? Result<int>.Success(orderId)
            : Result<int>.Failure(Error.NotFound("Order", orderId.ToString()));
    }

    static async Task<Result<int>> ValidateInventoryAsync(int orderId)
    {
        await Task.Delay(1);
        return Result<int>.Success(orderId);
    }

    static async Task<Result<int>> ReserveStockAsync(int orderId)
    {
        await Task.Delay(1);
        return Result<int>.Success(orderId);
    }

    static Task<Result<int>> ProcessOrderPipeline(int orderId) =>
        GetOrderAsync(orderId)
            .Then(ValidateInventoryAsync)
            .Then(ReserveStockAsync)
            .ExecuteAsync();

    // ── 6. Combine — collecting all errors from multiple validations ─────────

    static Result<string> ValidateAge(int age) =>
        age is >= 18 and <= 120
            ? Result<string>.Success($"{age}")
            : Result<string>.Failure(Error.Validation("Age must be between 18 and 120", field: "age"));

    static void CombineExample()
    {
        var allValid = new[]
        {
            ValidateName("Dany"),
            ValidateEmail("dany@example.com"),
            ValidateAge(28)
        }.Sequence();

        allValid.Match(
            values => Console.WriteLine($"All valid: {string.Join(", ", values)}"),
            error => Console.WriteLine($"{error.GetAllErrors().Count()} validation issues")
        );
    }

    // ── 7. Either — dual-track processing ───────────────────────────────────

    static Either<Error, User> AuthenticateUser(string token) =>
        token == "valid-token"
            ? Either<Error, User>.FromRight(new User(1, "Dany", "dany@example.com"))
            : Either<Error, User>.FromLeft(Error.Forbidden("Invalid token"));

    static void EitherExample()
    {
        var outcome = AuthenticateUser("valid-token")
            .Map(user => user.Name.ToUpper())
            .Match(
                left => $"Auth failed: {left.Message}",
                right => $"Welcome, {right}!"
            );

        Console.WriteLine(outcome); // "Welcome, DANY!"
    }

    // ── 8. ThenWithRetry — resilient external calls ──────────────────────────

    static int _callCount = 0;

    static async Task RetryExample()
    {
        var result = await Task.FromResult(Result<int>.Success(1))
            .ThenWithRetry(
                operation: async x =>
                {
                    _callCount++;
                    await Task.Delay(1);
                    return _callCount < 3
                        ? Result<int>.Failure("Temporary failure")
                        : Result<int>.Success(x * 10);
                },
                maxAttempts: 3,
                delay: TimeSpan.FromMilliseconds(100));

        result.Match(
            value => Console.WriteLine($"Success after retries: {value}"),
            error => Console.WriteLine($"Failed after 3 attempts: {error.Message}")
        );
    }
}
