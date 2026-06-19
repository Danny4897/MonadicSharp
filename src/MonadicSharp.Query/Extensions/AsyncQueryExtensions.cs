#nullable enable
using System.Runtime.CompilerServices;

namespace MonadicSharp.Query.Extensions;

#if NET11_0_OR_GREATER
/// <summary>
/// Async streaming operators on <c>IAsyncEnumerable&lt;Result&lt;T&gt;&gt;</c> for .NET 11+.
/// Designed for LLM streaming inference responses — SSE streams, chunked completions —
/// where results arrive incrementally and must be partitioned or aggregated on-the-fly.
/// </summary>
public static class AsyncQueryExtensions
{
    /// <summary>
    /// Projects successful values through <paramref name="successProjection"/> as they arrive,
    /// returning failures without buffering. Zero-latency streaming projection.
    /// </summary>
    public static async IAsyncEnumerable<Result<TOut>> PartitionMapAsync<T, TOut>(
        this IAsyncEnumerable<Result<T>> source,
        Func<T, TOut> successProjection,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var r in source.WithCancellation(ct).ConfigureAwait(false))
            yield return r.IsSuccess
                ? Result<TOut>.Success(successProjection(r.Value))
                : Result<TOut>.Failure(r.Error);
    }

    /// <summary>
    /// Materializes the stream and checks whether the success rate meets
    /// <paramref name="minimumRate"/>, returning <c>Result&lt;IReadOnlyList&lt;T&gt;&gt;</c>.
    /// Useful for accepting partial LLM streaming batch results with a quality threshold.
    /// </summary>
    public static async ValueTask<Result<IReadOnlyList<T>>> RequireSuccessRateAsync<T>(
        this IAsyncEnumerable<Result<T>> source,
        double minimumRate,
        CancellationToken ct = default)
    {
        var values = new List<T>();
        var errors = new List<Error>();

        await foreach (var r in source.WithCancellation(ct).ConfigureAwait(false))
        {
            if (r.IsSuccess) values.Add(r.Value);
            else errors.Add(r.Error);
        }

        int total = values.Count + errors.Count;
        double rate = total == 0 ? 0d : values.Count / (double)total;

        if (rate >= minimumRate)
            return Result<IReadOnlyList<T>>.Success(values);

        var combined = errors.Count == 1 ? errors[0] : Error.Combine([.. errors]);
        return Result<IReadOnlyList<T>>.Failure(
            Error.Create($"Success rate {rate:P0} below required {minimumRate:P0}.", combined.Code)
                 .WithMetadata("successRate", rate)
                 .WithMetadata("requiredRate", minimumRate)
                 .WithInnerError(combined));
    }

    /// <summary>
    /// Partitions an async result stream into buffers of successes and failures.
    /// Drains the full stream before returning.
    /// </summary>
    public static async ValueTask<(IReadOnlyList<T> Successes, IReadOnlyList<Error> Failures)> PartitionAsync<T>(
        this IAsyncEnumerable<Result<T>> source,
        CancellationToken ct = default)
    {
        var successes = new List<T>();
        var failures  = new List<Error>();

        await foreach (var r in source.WithCancellation(ct).ConfigureAwait(false))
        {
            if (r.IsSuccess) successes.Add(r.Value);
            else failures.Add(r.Error);
        }

        return (successes, failures);
    }

    /// <summary>
    /// Streams only successful values, discarding errors. Use <see cref="PartitionAsync{T}"/>
    /// when error visibility is required.
    /// </summary>
    public static async IAsyncEnumerable<T> SuccessValuesAsync<T>(
        this IAsyncEnumerable<Result<T>> source,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var r in source.WithCancellation(ct).ConfigureAwait(false))
            if (r.IsSuccess) yield return r.Value;
    }

    /// <summary>
    /// Returns the best successful result scored by <paramref name="scorer"/>.
    /// Drains the full stream. Fails if no successful result exists.
    /// </summary>
    public static async ValueTask<Result<T>> BestOfAsync<T>(
        this IAsyncEnumerable<Result<T>> source,
        Func<T, double> scorer,
        CancellationToken ct = default)
    {
        var best = (score: double.MinValue, value: default(T)!, found: false);
        Error? lastError = null;

        await foreach (var r in source.WithCancellation(ct).ConfigureAwait(false))
        {
            if (r.IsFailure) { lastError = r.Error; continue; }
            var s = scorer(r.Value);
            if (s > best.score) best = (s, r.Value, true);
        }

        return best.found
            ? Result<T>.Success(best.value)
            : Result<T>.Failure(lastError ?? Error.Create("No successful results in async stream."));
    }
}
#endif
