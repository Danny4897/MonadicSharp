#nullable enable
namespace MonadicSharp.Extensions;

#if NET11_0_OR_GREATER
/// <summary>
/// <c>ValueTask&lt;Result&lt;T&gt;&gt;</c> extension operators for .NET 11+ Runtime Async.
/// Prefer <c>ValueTask</c> over <c>Task</c> on hot paths: Runtime Async eliminates
/// state-machine allocation for <c>ValueTask</c>-returning methods, reducing GC pressure
/// in LLM inference loops and high-throughput pipelines.
/// </summary>
public static class ValueTaskResultExtensions
{
    /// <summary>
    /// Maps a successful value through <paramref name="mapper"/> within a <c>ValueTask</c>.
    /// Zero allocation on the success path when the result is already completed.
    /// </summary>
    public static async ValueTask<Result<TResult>> Map<T, TResult>(
        this ValueTask<Result<T>> resultTask,
        Func<T, TResult> mapper)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Map(mapper);
    }

    /// <summary>
    /// Chains a monadic function over a <c>ValueTask&lt;Result&lt;T&gt;&gt;</c>.
    /// Short-circuits to failure without awaiting <paramref name="binder"/> on error.
    /// </summary>
    public static async ValueTask<Result<TResult>> Bind<T, TResult>(
        this ValueTask<Result<T>> resultTask,
        Func<T, ValueTask<Result<TResult>>> binder)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsFailure
            ? Result<TResult>.Failure(result.Error)
            : await binder(result.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs a side-effectful action on success without changing the result value.
    /// </summary>
    public static async ValueTask<Result<T>> Do<T>(
        this ValueTask<Result<T>> resultTask,
        Action<T> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess) action(result.Value);
        return result;
    }

    /// <summary>
    /// Performs an async side-effectful action on success without changing the result value.
    /// </summary>
    public static async ValueTask<Result<T>> DoAsync<T>(
        this ValueTask<Result<T>> resultTask,
        Func<T, ValueTask> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess) await action(result.Value).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Applies a recovery function when the result is a failure.
    /// </summary>
    public static async ValueTask<Result<T>> OrElse<T>(
        this ValueTask<Result<T>> resultTask,
        Func<Error, ValueTask<Result<T>>> recovery)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? result : await recovery(result.Error).ConfigureAwait(false);
    }

    /// <summary>
    /// Converts a <c>ValueTask&lt;Result&lt;T&gt;&gt;</c> to <c>Task&lt;Result&lt;T&gt;&gt;</c>
    /// when interop with older APIs is required.
    /// </summary>
    public static Task<Result<T>> AsTask<T>(this ValueTask<Result<T>> vt) => vt.AsTask();

    /// <summary>
    /// Wraps a <c>Result&lt;T&gt;</c> in a completed <c>ValueTask</c>.
    /// </summary>
    public static ValueTask<Result<T>> AsValueTask<T>(this Result<T> result) =>
        ValueTask.FromResult(result);
}

/// <summary>
/// <c>ValueTask&lt;Option&lt;T&gt;&gt;</c> extension operators for .NET 11+ Runtime Async.
/// </summary>
public static class ValueTaskOptionExtensions
{
    /// <summary>Maps the value through <paramref name="mapper"/> if present.</summary>
    public static async ValueTask<Option<TResult>> Map<T, TResult>(
        this ValueTask<Option<T>> optionTask,
        Func<T, TResult> mapper)
    {
        var option = await optionTask.ConfigureAwait(false);
        return option.Map(mapper);
    }

    /// <summary>Chains a monadic function over the option value if present.</summary>
    public static async ValueTask<Option<TResult>> Bind<T, TResult>(
        this ValueTask<Option<T>> optionTask,
        Func<T, ValueTask<Option<TResult>>> binder)
    {
        var option = await optionTask.ConfigureAwait(false);
        return option.HasValue
            ? await binder(option.GetValueOrDefault(default(T)!)).ConfigureAwait(false)
            : Option<TResult>.None;
    }

    /// <summary>Converts to <c>ValueTask&lt;Result&lt;T&gt;&gt;</c>.</summary>
    public static async ValueTask<Result<T>> ToResult<T>(
        this ValueTask<Option<T>> optionTask, Error error)
    {
        var option = await optionTask.ConfigureAwait(false);
        return option.ToResult(error);
    }

    /// <summary>Wraps an <c>Option&lt;T&gt;</c> in a completed <c>ValueTask</c>.</summary>
    public static ValueTask<Option<T>> AsValueTask<T>(this Option<T> option) =>
        ValueTask.FromResult(option);
}
#endif
