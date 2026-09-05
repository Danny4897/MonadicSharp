#nullable enable
namespace MonadicSharp.Unions.Extensions;

/// <summary>
/// Bridge extensions from MonadicSharp Unions to core primitives
/// (<see cref="Result{T}"/>, <see cref="Option{T}"/>).
/// </summary>
public static class UnionExtensions
{
    // ── Union2 bridges ──────────────────────────────────────────────────────

    /// <summary>
    /// Converts a <c>Union2&lt;Error, T&gt;</c> (error-or-value) to <c>Result&lt;T&gt;</c>.
    /// </summary>
    public static Result<T> ToResult<T>(this Union2<Error, T> union) =>
        union.Match(
            onCase1: e => Result<T>.Failure(e),
            onCase2: v => Result<T>.Success(v));

    /// <summary>
    /// Converts a <c>Union2&lt;T, Unit&gt;</c> (value-or-nothing) to <c>Option&lt;T&gt;</c>.
    /// </summary>
    public static Option<T> ToOption<T>(this Union2<T, Unit> union) =>
        union.Match(
            onCase1: v => Option<T>.Some(v),
            onCase2: _ => Option<T>.None);

    /// <summary>
    /// Converts a <c>Union2&lt;T1, T2&gt;</c> to <c>Result&lt;T2&gt;</c> by mapping
    /// <typeparamref name="T1"/> to an <see cref="Error"/> via <paramref name="errorFactory"/>.
    /// </summary>
    public static Result<T2> ToResult<T1, T2>(this Union2<T1, T2> union,
        Func<T1, Error> errorFactory) =>
        union.Match(
            onCase1: v => Result<T2>.Failure(errorFactory(v)),
            onCase2: v => Result<T2>.Success(v));

    // ── Union3 bridges ──────────────────────────────────────────────────────

    /// <summary>
    /// Converts a <c>Union3&lt;T, Error, Error&gt;</c> to <c>Result&lt;T&gt;</c>,
    /// combining both error cases into a failure.
    /// </summary>
    public static Result<T> ToResult<T>(this Union3<T, Error, Error> union) =>
        union.Match(
            onCase1: v => Result<T>.Success(v),
            onCase2: e => Result<T>.Failure(e),
            onCase3: e => Result<T>.Failure(e));

    /// <summary>
    /// Converts a <c>Union3&lt;T1, T2, T3&gt;</c> to <c>Result&lt;T1&gt;</c> by mapping
    /// non-T1 cases to <see cref="Error"/> via <paramref name="case2Error"/> and
    /// <paramref name="case3Error"/>.
    /// </summary>
    public static Result<T1> ToResult<T1, T2, T3>(this Union3<T1, T2, T3> union,
        Func<T2, Error> case2Error,
        Func<T3, Error> case3Error) =>
        union.Match(
            onCase1: v  => Result<T1>.Success(v),
            onCase2: v2 => Result<T1>.Failure(case2Error(v2)),
            onCase3: v3 => Result<T1>.Failure(case3Error(v3)));

    // ── LLM result factory helper ───────────────────────────────────────────

    /// <summary>
    /// Wraps a value inside a <c>Union3</c> modelling the common LLM result shape:
    /// <list type="bullet">
    /// <item><typeparamref name="TValue"/> — successful model output (Case1)</item>
    /// <item><see cref="Error"/> — rate limit or recoverable failure (Case2)</item>
    /// <item><see cref="Error"/> — hard model error (Case3)</item>
    /// </list>
    /// </summary>
    public static Union3<TValue, Error, Error> LlmSuccess<TValue>(TValue value) =>
        Union3<TValue, Error, Error>.Case1(value);

    /// <summary>
    /// Creates a rate-limit case in the LLM union shape.
    /// </summary>
    public static Union3<TValue, Error, Error> LlmRateLimit<TValue>(TimeSpan retryAfter) =>
        Union3<TValue, Error, Error>.Case2(
            Error.Create($"Rate limited. Retry after {retryAfter.TotalSeconds:F0}s.", "RATE_LIMIT"));

    /// <summary>
    /// Creates a hard model error case in the LLM union shape.
    /// </summary>
    public static Union3<TValue, Error, Error> LlmModelError<TValue>(string message) =>
        Union3<TValue, Error, Error>.Case3(Error.Create(message, "MODEL_ERROR"));
}
