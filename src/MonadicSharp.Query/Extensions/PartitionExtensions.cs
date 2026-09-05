#nullable enable
namespace MonadicSharp.Query.Extensions;

/// <summary>
/// Extended partitioning operators on <c>IEnumerable&lt;Result&lt;T&gt;&gt;</c>.
/// Provides richer split/group semantics than the core <c>Partition</c> extension.
/// </summary>
public static class PartitionExtensions
{
    /// <summary>
    /// Partitions results into successes and failures, applying
    /// <paramref name="successProjection"/> to each successful value.
    /// Useful for projecting a batch of LLM responses into domain objects.
    /// </summary>
    public static (IReadOnlyList<TOut> Successes, IReadOnlyList<Error> Failures)
        PartitionMap<T, TOut>(
            this IEnumerable<Result<T>> results,
            Func<T, TOut> successProjection)
    {
        var successes = new List<TOut>();
        var failures = new List<Error>();
        foreach (var r in results)
        {
            if (r.IsSuccess) successes.Add(successProjection(r.Value));
            else failures.Add(r.Error);
        }
        return (successes, failures);
    }

    /// <summary>
    /// Returns only the successful values, discarding errors.
    /// </summary>
    public static IEnumerable<T> SuccessValues<T>(this IEnumerable<Result<T>> results) =>
        results.Where(r => r.IsSuccess).Select(r => r.Value);

    /// <summary>
    /// Returns only the failure errors, discarding successful values.
    /// </summary>
    public static IEnumerable<Error> FailureErrors<T>(this IEnumerable<Result<T>> results) =>
        results.Where(r => r.IsFailure).Select(r => r.Error);

    /// <summary>
    /// Groups failure errors by <see cref="ErrorType"/>, allowing targeted retry or logging strategies.
    /// Use <see cref="SuccessValues{T}"/> separately for the successful values.
    /// </summary>
    public static ILookup<ErrorType, Error> GroupFailuresByType<T>(
        this IEnumerable<Result<T>> results) =>
        results
            .Where(r => r.IsFailure)
            .Select(r => r.Error)
            .ToLookup(e => e.Type);

    /// <summary>
    /// Returns the ratio of successes to total results.
    /// Returns 0 for an empty sequence.
    /// </summary>
    public static double SuccessRate<T>(this IEnumerable<Result<T>> results)
    {
        var list = results as IList<Result<T>> ?? results.ToList();
        if (list.Count == 0) return 0d;
        return list.Count(r => r.IsSuccess) / (double)list.Count;
    }

    /// <summary>
    /// Returns a <c>Result&lt;IReadOnlyList&lt;T&gt;&gt;</c> that succeeds only when
    /// the success rate is at or above <paramref name="minimumRate"/> (0.0–1.0).
    /// Useful for accepting partial LLM batch results with a quality threshold.
    /// </summary>
    public static Result<IReadOnlyList<T>> RequireSuccessRate<T>(
        this IEnumerable<Result<T>> results,
        double minimumRate)
    {
        var list = results as IList<Result<T>> ?? results.ToList();
        var values = new List<T>(list.Count);
        var errors = new List<Error>();

        foreach (var r in list)
        {
            if (r.IsSuccess) values.Add(r.Value);
            else errors.Add(r.Error);
        }

        var rate = list.Count == 0 ? 0d : values.Count / (double)list.Count;
        if (rate >= minimumRate)
            return Result<IReadOnlyList<T>>.Success(values);

        var combined = errors.Count == 1 ? errors[0] : Error.Combine([.. errors]);
        return Result<IReadOnlyList<T>>.Failure(
            Error.Create($"Success rate {rate:P0} below required {minimumRate:P0}.", combined.Code)
                 .WithMetadata("successRate", rate)
                 .WithMetadata("requiredRate", minimumRate)
                 .WithInnerError(combined));
    }
}
