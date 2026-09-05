#nullable enable
namespace MonadicSharp.Query.Extensions;

/// <summary>
/// Multi-source reconciliation operators for batches of <c>Result&lt;T&gt;</c>.
/// Designed for LLM pipelines that aggregate outputs from multiple model calls
/// or multiple data sources and need a unified, typed outcome.
/// </summary>
public static class ReconcileExtensions
{
    /// <summary>
    /// Reconciles two result sequences by key, producing a combined result per key.
    /// Items present in only one source use <paramref name="leftOnly"/> or <paramref name="rightOnly"/>.
    /// Items present in both are merged via <paramref name="merge"/>.
    /// Non-matching failures from either side are propagated.
    /// </summary>
    public static IEnumerable<Result<TOut>> ReconcileBy<TLeft, TRight, TKey, TOut>(
        this IEnumerable<Result<TLeft>> left,
        IEnumerable<Result<TRight>> right,
        Func<TLeft, TKey> leftKey,
        Func<TRight, TKey> rightKey,
        Func<TLeft, TOut> leftOnly,
        Func<TRight, TOut> rightOnly,
        Func<TLeft, TRight, TOut> merge)
    {
        var leftList  = left.ToList();
        var rightList = right.ToList();

        // Propagate failures as-is
        foreach (var r in leftList.Where(r => r.IsFailure))
            yield return Result<TOut>.Failure(r.Error);
        foreach (var r in rightList.Where(r => r.IsFailure))
            yield return Result<TOut>.Failure(r.Error);

        var leftMap  = leftList.Where(r => r.IsSuccess)
                               .ToDictionary(r => leftKey(r.Value), r => r.Value);
        var rightMap = rightList.Where(r => r.IsSuccess)
                                .ToDictionary(r => rightKey(r.Value), r => r.Value);

        foreach (var (key, lv) in leftMap)
        {
            yield return rightMap.TryGetValue(key, out var rv)
                ? Result<TOut>.Success(merge(lv, rv))
                : Result<TOut>.Success(leftOnly(lv));
        }

        foreach (var (key, rv) in rightMap)
        {
            if (!leftMap.ContainsKey(key))
                yield return Result<TOut>.Success(rightOnly(rv));
        }
    }

    /// <summary>
    /// Merges two same-type result sequences by key, preferring the left value
    /// when both sides have a matching key.
    /// </summary>
    public static IEnumerable<Result<T>> MergeBy<T, TKey>(
        this IEnumerable<Result<T>> left,
        IEnumerable<Result<T>> right,
        Func<T, TKey> keySelector) =>
        left.ReconcileBy(
            right,
            leftKey:   keySelector,
            rightKey:  keySelector,
            leftOnly:  x => x,
            rightOnly: y => y,
            merge:     (x, _) => x);

    /// <summary>
    /// Aggregates multiple result sequences into a single <c>Result&lt;IReadOnlyList&lt;T&gt;&gt;</c>.
    /// Fails immediately if any source contains a failure.
    /// </summary>
    public static Result<IReadOnlyList<T>> AggregateAll<T>(
        this IEnumerable<IEnumerable<Result<T>>> sources)
    {
        var allResults = new List<T>();
        var allErrors  = new List<Error>();

        foreach (var source in sources)
        {
            foreach (var r in source)
            {
                if (r.IsSuccess) allResults.Add(r.Value);
                else allErrors.Add(r.Error);
            }
        }

        if (allErrors.Count > 0)
            return Result<IReadOnlyList<T>>.Failure(
                allErrors.Count == 1 ? allErrors[0] : Error.Combine([.. allErrors]));

        return Result<IReadOnlyList<T>>.Success(allResults);
    }

    /// <summary>
    /// Picks the best result from a sequence using <paramref name="scorer"/>.
    /// Fails if no successful result exists.
    /// </summary>
    public static Result<T> BestOf<T>(
        this IEnumerable<Result<T>> results,
        Func<T, double> scorer)
    {
        var best = (score: double.MinValue, value: default(T)!, found: false);
        Error? lastError = null;

        foreach (var r in results)
        {
            if (r.IsFailure) { lastError = r.Error; continue; }
            var s = scorer(r.Value);
            if (s > best.score) best = (s, r.Value, true);
        }

        return best.found
            ? Result<T>.Success(best.value)
            : Result<T>.Failure(lastError ?? Error.Create("No successful results to pick from."));
    }
}
