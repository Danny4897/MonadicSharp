#nullable enable
using System.Runtime.CompilerServices;

namespace MonadicSharp.Extensions;

#if NET11_0_OR_GREATER
/// <summary>
/// <c>IAsyncEnumerable&lt;Result&lt;T&gt;&gt;</c> streaming operators for .NET 11+ Runtime Async pipelines.
/// These methods leverage .NET 11 Runtime Async (coroutine-based, zero state-machine allocation)
/// for LLM streaming responses and batch processing hot paths.
/// </summary>
public static class AsyncStreamExtensions
{
    /// <summary>
    /// Projects each successful value in an async stream through <paramref name="mapper"/>,
    /// propagating failures as-is. Allocation-free on .NET 11 Runtime Async.
    /// </summary>
    public static async IAsyncEnumerable<Result<TResult>> MapEach<T, TResult>(
        this IAsyncEnumerable<Result<T>> source,
        Func<T, TResult> mapper,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var r in source.WithCancellation(ct).ConfigureAwait(false))
            yield return r.IsSuccess ? Result<TResult>.Success(mapper(r.Value)) : Result<TResult>.Failure(r.Error);
    }

    /// <summary>
    /// Chains a monadic function over each successful value in an async stream.
    /// Short-circuits individual items to failure without stopping the stream.
    /// </summary>
    public static async IAsyncEnumerable<Result<TResult>> BindEach<T, TResult>(
        this IAsyncEnumerable<Result<T>> source,
        Func<T, Result<TResult>> binder,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var r in source.WithCancellation(ct).ConfigureAwait(false))
            yield return r.IsSuccess ? binder(r.Value) : Result<TResult>.Failure(r.Error);
    }

    /// <summary>
    /// Chains an async monadic function over each successful value in an async stream.
    /// </summary>
    public static async IAsyncEnumerable<Result<TResult>> BindEachAsync<T, TResult>(
        this IAsyncEnumerable<Result<T>> source,
        Func<T, Task<Result<TResult>>> binder,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var r in source.WithCancellation(ct).ConfigureAwait(false))
            yield return r.IsSuccess ? await binder(r.Value).ConfigureAwait(false) : Result<TResult>.Failure(r.Error);
    }

    /// <summary>
    /// Filters the async stream to only successful values matching <paramref name="predicate"/>.
    /// Failed results and non-matching successes are discarded.
    /// </summary>
    public static async IAsyncEnumerable<T> SuccessValues<T>(
        this IAsyncEnumerable<Result<T>> source,
        Func<T, bool>? predicate = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var r in source.WithCancellation(ct).ConfigureAwait(false))
        {
            if (r.IsSuccess && (predicate is null || predicate(r.Value)))
                yield return r.Value;
        }
    }

    /// <summary>
    /// Stops the stream at the first <see cref="Result{T}"/> failure, yielding all
    /// prior successes. Useful for strict LLM batch pipelines where any error is fatal.
    /// </summary>
    public static async IAsyncEnumerable<Result<T>> TakeUntilError<T>(
        this IAsyncEnumerable<Result<T>> source,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var r in source.WithCancellation(ct).ConfigureAwait(false))
        {
            yield return r;
            if (r.IsFailure) yield break;
        }
    }

    /// <summary>
    /// Materializes an async stream into <c>Result&lt;IReadOnlyList&lt;T&gt;&gt;</c>.
    /// Collects all failures and combines them if any exist; otherwise returns all values.
    /// </summary>
    public static async ValueTask<Result<IReadOnlyList<T>>> CollectAsync<T>(
        this IAsyncEnumerable<Result<T>> source,
        CancellationToken ct = default)
    {
        var values = new List<T>();
        var errors = new List<Error>();

        await foreach (var r in source.WithCancellation(ct).ConfigureAwait(false))
        {
            if (r.IsSuccess) values.Add(r.Value);
            else errors.Add(r.Error);
        }

        if (errors.Count > 0)
            return Result<IReadOnlyList<T>>.Failure(
                errors.Count == 1 ? errors[0] : Error.Combine([.. errors]));

        return Result<IReadOnlyList<T>>.Success(values);
    }

    /// <summary>
    /// Materializes only the successful values from an async stream into a list.
    /// Errors are silently discarded — use <see cref="CollectAsync{T}"/> to capture them.
    /// </summary>
    public static async ValueTask<IReadOnlyList<T>> CollectValuesAsync<T>(
        this IAsyncEnumerable<Result<T>> source,
        CancellationToken ct = default)
    {
        var values = new List<T>();
        await foreach (var r in source.WithCancellation(ct).ConfigureAwait(false))
            if (r.IsSuccess) values.Add(r.Value);
        return values;
    }

    /// <summary>
    /// Converts an <c>IAsyncEnumerable&lt;T&gt;</c> into an async result stream
    /// by applying <paramref name="selector"/> to each element.
    /// </summary>
    public static async IAsyncEnumerable<Result<TResult>> TraverseAsync<T, TResult>(
        this IAsyncEnumerable<T> source,
        Func<T, Result<TResult>> selector,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in source.WithCancellation(ct).ConfigureAwait(false))
            yield return selector(item);
    }

    /// <summary>
    /// Converts an <c>IAsyncEnumerable&lt;T&gt;</c> into an async result stream
    /// by applying an async <paramref name="selector"/> to each element.
    /// </summary>
    public static async IAsyncEnumerable<Result<TResult>> TraverseAsync<T, TResult>(
        this IAsyncEnumerable<T> source,
        Func<T, Task<Result<TResult>>> selector,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in source.WithCancellation(ct).ConfigureAwait(false))
            yield return await selector(item).ConfigureAwait(false);
    }
}
#endif
