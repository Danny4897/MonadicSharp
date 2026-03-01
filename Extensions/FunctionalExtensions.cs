namespace MonadicSharp.Extensions;

/// <summary>
/// Extension methods per Result per supportare operazioni funzionali avanzate
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Trasforma una lista di Result in un Result di lista
    /// </summary>
    public static Result<IEnumerable<T>> Sequence<T>(this IEnumerable<Result<T>> results)
    {
        var resultsList = results.ToList();
        var failures = resultsList.Where(r => r.IsFailure).ToList();

        if (failures.Any())
        {
            var combinedError = Error.Combine(failures.Select(f => f.Error).ToArray());
            return Result<IEnumerable<T>>.Failure(combinedError);
        }

        return Result<IEnumerable<T>>.Success(resultsList.Select(r => r.Value));
    }

    /// <summary>
    /// Applica una funzione a ciascun elemento di una lista, fermandosi al primo errore
    /// </summary>
    public static Result<IEnumerable<TResult>> Traverse<T, TResult>(this IEnumerable<T> source,
        Func<T, Result<TResult>> selector) =>
        source.Select(selector).Sequence();

    /// <summary>
    /// Applica una funzione che può fallire solo se il Result è Success
    /// </summary>
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Error error) =>
        result.IsFailure ? result : result.Where(predicate, error);

    /// <summary>
    /// Applica una funzione che può fallire solo se il Result è Success
    /// </summary>
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage) =>
        result.Ensure(predicate, Error.Create(errorMessage));

    /// <summary>
    /// Combina due Result usando una funzione
    /// </summary>
    public static Result<TResult> Combine<T1, T2, TResult>(this Result<T1> result1, Result<T2> result2,
        Func<T1, T2, TResult> combiner)
    {
        if (result1.IsFailure && result2.IsFailure)
            return Result<TResult>.Failure(Error.Combine(result1.Error, result2.Error));

        if (result1.IsFailure)
            return Result<TResult>.Failure(result1.Error);

        if (result2.IsFailure)
            return Result<TResult>.Failure(result2.Error);

        return Result<TResult>.Success(combiner(result1.Value, result2.Value));
    }

    /// <summary>
    /// Versione asincrona di Map
    /// </summary>
    public static async Task<Result<TResult>> MapAsync<T, TResult>(this Result<T> result,
        Func<T, Task<TResult>> mapper) =>
        result.IsFailure ? Result<TResult>.Failure(result.Error) :
        Result<TResult>.Success(await mapper(result.Value));

    /// <summary>
    /// Versione asincrona di Bind
    /// </summary>
    public static async Task<Result<TResult>> BindAsync<T, TResult>(this Result<T> result,
        Func<T, Task<Result<TResult>>> binder) =>
        result.IsFailure ? Result<TResult>.Failure(result.Error) : await binder(result.Value);

    /// <summary>
    /// Applica una funzione di recupero in caso di fallimento
    /// </summary>
    public static Result<T> OrElse<T>(this Result<T> result, Func<Error, Result<T>> recovery) =>
        result.IsSuccess ? result : recovery(result.Error);

    /// <summary>
    /// Restituisce un Result alternativo in caso di fallimento
    /// </summary>
    public static Result<T> OrElse<T>(this Result<T> result, Result<T> alternative) =>
        result.IsSuccess ? result : alternative;

    /// <summary>
    /// Converte un Result&lt;T&gt; in Task&lt;Result&lt;T&gt;&gt;
    /// </summary>
    public static Task<Result<T>> AsTask<T>(this Result<T> result) =>
        Task.FromResult(result);

    /// <summary>
    /// Handles success case in a Result
    /// </summary>
    public static Result<T> Success<T>(this Result<T> result, Func<T, Result<T>> onSuccess)
    {
        return result.IsSuccess ? onSuccess(result.Value) : result;
    }

    /// <summary>
    /// Handles failure case in a Result
    /// </summary>
    public static Result<T> Failure<T>(this Result<T> result, Func<Error, Result<T>> onFailure)
    {
        return result.IsFailure ? onFailure(result.Error) : result;
    }
}

/// <summary>
/// Extension methods per Option
/// </summary>
public static class OptionExtensions
{
    /// <summary>
    /// Filtra un Option basato su un predicato
    /// </summary>
    public static Option<T> Filter<T>(this Option<T> option, Func<T, bool> predicate) =>
        option.HasValue && predicate(option.GetValueOrDefault(default(T)!)) ? option : Option<T>.None;

    /// <summary>
    /// Trasforma una lista di Option in un Option di lista (tutti devono avere valore)
    /// </summary>
    public static Option<IEnumerable<T>> Sequence<T>(this IEnumerable<Option<T>> options)
    {
        var optionsList = options.ToList();

        if (optionsList.Any(o => o.IsNone))
            return Option<IEnumerable<T>>.None;

        return Option<IEnumerable<T>>.Some(optionsList.Select(o => o.GetValueOrDefault(default(T)!)));
    }

    /// <summary>
    /// Applica una funzione a ciascun elemento di una lista
    /// </summary>
    public static Option<IEnumerable<TResult>> Traverse<T, TResult>(this IEnumerable<T> source,
        Func<T, Option<TResult>> selector) =>
        source.Select(selector).Sequence();

    /// <summary>
    /// Restituisce il primo Option che ha un valore
    /// </summary>
    public static Option<T> FirstOrNone<T>(this IEnumerable<Option<T>> options) =>
        options.FirstOrDefault(o => o.HasValue);

    /// <summary>
    /// Converte un Option in Result
    /// </summary>
    public static Result<T> ToResult<T>(this Option<T> option, Error error) =>
        option.HasValue ? Result<T>.Success(option.GetValueOrDefault(default(T)!)) : Result<T>.Failure(error);

    /// <summary>
    /// Converte un Option in Result con messaggio di errore
    /// </summary>
    public static Result<T> ToResult<T>(this Option<T> option, string errorMessage) =>
        option.ToResult(Error.Create(errorMessage));

    /// <summary>
    /// Versione asincrona di Map
    /// </summary>
    public static async Task<Option<TResult>> MapAsync<T, TResult>(this Option<T> option,
        Func<T, Task<TResult>> mapper) =>
        option.HasValue ? Option<TResult>.Some(await mapper(option.GetValueOrDefault(default(T)!))) :
        Option<TResult>.None;

    /// <summary>
    /// Versione asincrona di Bind
    /// </summary>
    public static async Task<Option<TResult>> BindAsync<T, TResult>(this Option<T> option,
        Func<T, Task<Option<TResult>>> binder) =>
        option.HasValue ? await binder(option.GetValueOrDefault(default(T)!)) : Option<TResult>.None;

    /// <summary>
    /// Converte un Option<T> in Task<Option<T>>
    /// </summary>
    public static Task<Option<T>> AsTask<T>(this Option<T> option) =>
        Task.FromResult(option);
}

/// <summary>
/// Extension methods per Task<Result<T>>
/// </summary>
public static class TaskResultExtensions
{
    /// <summary>
    /// Map asincrono per Task<Result<T>>
    /// </summary>
    public static async Task<Result<TResult>> Map<T, TResult>(this Task<Result<T>> resultTask,
        Func<T, TResult> mapper)
    {
        var result = await resultTask;
        return result.Map(mapper);
    }

    /// <summary>
    /// Bind asincrono per Task<Result<T>>
    /// </summary>
    public static async Task<Result<TResult>> Bind<T, TResult>(this Task<Result<T>> resultTask,
        Func<T, Task<Result<TResult>>> binder)
    {
        var result = await resultTask;
        return result.IsFailure ? Result<TResult>.Failure(result.Error) : await binder(result.Value);
    }

    /// <summary>
    /// Do asincrono per Task<Result<T>>
    /// </summary>
    public static async Task<Result<T>> Do<T>(this Task<Result<T>> resultTask,
        Func<T, Task> action)
    {
        var result = await resultTask;
        if (result.IsSuccess)
            await action(result.Value);
        return result;
    }

    /// <summary>
    /// BindAsync per Task<Result<T>> - permette di concatenare operazioni asincrone
    /// </summary>
    public static async Task<Result<TResult>> BindAsync<T, TResult>(this Task<Result<T>> resultTask,
        Func<T, Task<Result<TResult>>> binder)
    {
        var result = await resultTask;
        return result.IsFailure ? Result<TResult>.Failure(result.Error) : await binder(result.Value);
    }

    /// <summary>
    /// MapAsync per Task<Result<T>> - trasforma il valore di un Result asincrono
    /// </summary>
    public static async Task<Result<TResult>> MapAsync<T, TResult>(this Task<Result<T>> resultTask,
        Func<T, TResult> mapper)
    {
        var result = await resultTask;
        return result.Map(mapper);
    }

    /// <summary>
    /// MapAsync per Task<Result<T>> con mapper asincrono
    /// </summary>
    public static async Task<Result<TResult>> MapAsync<T, TResult>(this Task<Result<T>> resultTask,
        Func<T, Task<TResult>> mapper)
    {
        var result = await resultTask;
        return result.IsFailure ? Result<TResult>.Failure(result.Error) : Result<TResult>.Success(await mapper(result.Value));
    }

    /// <summary>
    /// DoAsync per Task<Result<T>> - esegue un'azione asincrona se il risultato è Success
    /// </summary>
    public static async Task<Result<T>> DoAsync<T>(this Task<Result<T>> resultTask,
        Func<T, Task> action)
    {
        var result = await resultTask;
        if (result.IsSuccess)
            await action(result.Value);
        return result;
    }

    /// <summary>
    /// GetValueOrDefault per Task<Result<Option<T>>>
    /// </summary>
    public static async Task<Option<T>> GetValueOrDefault<T>(this Task<Result<Option<T>>> taskResult)
    {
        var result = await taskResult;
        return result.IsSuccess ? result.Value : Option<T>.None;
    }

    /// <summary>
    /// GetValueOrDefault per Task<Result<Option<T>>> con valore di default
    /// </summary>
    public static async Task<Option<T>> GetValueOrDefault<T>(this Task<Result<Option<T>>> taskResult, Option<T> defaultValue)
    {
        var result = await taskResult;
        return result.IsSuccess ? result.Value : defaultValue;
    }

    /// <summary>
    /// Bind per Task<Result<Result<T>>> - flattens nested Results
    /// </summary>
    public static async Task<Result<T>> Bind<T>(this Task<Result<Result<T>>> taskResult)
    {
        var result = await taskResult;
        return result.IsSuccess ? result.Value : Result<T>.Failure(result.Error);
    }

    /// <summary>
    /// Bind per Task<Result<Result<Unit>>> - flattens nested Results specifically for Unit
    /// </summary>
    public static async Task<Result<Unit>> Bind(this Task<Result<Result<Unit>>> taskResult)
    {
        var result = await taskResult;
        return result.IsSuccess ? result.Value : Result<Unit>.Failure(result.Error);
    }
}

/// <summary>
/// Extension methods per Task<Option<T>>
/// </summary>
public static class TaskOptionExtensions
{
    /// <summary>
    /// Map asincrono per Task<Option<T>>
    /// </summary>
    public static async Task<Option<TResult>> Map<T, TResult>(this Task<Option<T>> optionTask,
        Func<T, TResult> mapper)
    {
        var option = await optionTask;
        return option.Map(mapper);
    }

    /// <summary>
    /// Bind asincrono per Task<Option<T>>
    /// </summary>
    public static async Task<Option<TResult>> Bind<T, TResult>(this Task<Option<T>> optionTask,
        Func<T, Task<Option<TResult>>> binder)
    {
        var option = await optionTask;
        return option.HasValue ? await binder(option.GetValueOrDefault(default(T)!)) : Option<TResult>.None;
    }

    /// <summary>
    /// Do asincrono per Task<Option<T>>
    /// </summary>
    public static async Task<Option<T>> Do<T>(this Task<Option<T>> optionTask,
        Func<T, Task> action)
    {
        var option = await optionTask;
        if (option.HasValue)
            await action(option.GetValueOrDefault(default(T)!));
        return option;
    }

    /// <summary>
    /// ToResult per Task<Option<T>>
    /// </summary>
    public static async Task<Result<T>> ToResult<T>(this Task<Option<T>> optionTask, Error error)
    {
        var option = await optionTask;
        return option.ToResult(error);
    }

    /// <summary>
    /// ToResult per Task<Option<T>> con messaggio di errore
    /// </summary>
    public static async Task<Result<T>> ToResult<T>(this Task<Option<T>> optionTask, string errorMessage)
    {
        var option = await optionTask;
        return option.ToResult(errorMessage);
    }
}
