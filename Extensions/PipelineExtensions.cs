namespace MonadicSharp.Extensions;

/// <summary>
/// Extension methods per operazioni di piping e composizione funzionale
/// </summary>
public static class PipeExtensions
{
    /// <summary>
    /// Pipe operation - applica una funzione al valore
    /// </summary>
    public static TResult Pipe<T, TResult>(this T value, Func<T, TResult> func) =>
        func(value);

    /// <summary>
    /// Pipe operation asincrona
    /// </summary>
    public static async Task<TResult> PipeAsync<T, TResult>(this T value, Func<T, Task<TResult>> func) =>
        await func(value);

    /// <summary>
    /// Pipe operation per Task
    /// </summary>
    public static async Task<TResult> Pipe<T, TResult>(this Task<T> valueTask, Func<T, TResult> func)
    {
        var value = await valueTask;
        return func(value);
    }
}

/// <summary>
/// Extension methods per pipeline composition avanzata
/// </summary>
public static class PipelineExtensions
{
    /// <summary>
    /// Compone una pipeline di operazioni Task<Result<T>> in sequenza
    /// Ogni operazione riceve il risultato della precedente
    /// Si ferma al primo errore (fail-fast behavior)
    /// </summary>
    /// <typeparam name="T">Tipo iniziale</typeparam>
    /// <param name="initialValue">Valore iniziale</param>
    /// <param name="operations">Array di funzioni da applicare in sequenza</param>
    /// <returns>Risultato finale della pipeline</returns>
    public static async Task<Result<T>> PipelineAsync<T>(
        this Task<Result<T>> initialValue,
        params Func<T, Task<Result<T>>>[] operations)
    {
        var current = await initialValue;

        foreach (var operation in operations)
        {
            if (current.IsFailure)
                return current;

            current = await operation(current.Value);
        }

        return current;
    }

    /// <summary>
    /// Compone una pipeline di operazioni con tipi diversi
    /// Ogni operazione può trasformare il tipo
    /// </summary>
    public static async Task<Result<TResult>> PipelineAsync<TInput, TResult>(
        this Task<Result<TInput>> initialValue,
        params IPipelineStep<TInput, TResult>[] steps)
    {
        var current = await initialValue;
        if (current.IsFailure)
            return Result<TResult>.Failure(current.Error);

        object? currentValue = current.Value;

        foreach (var step in steps)
        {
            if (currentValue is null)
                return Result<TResult>.Failure(Error.Create("Null value in pipeline"));

            var stepResult = await step.ExecuteAsync(currentValue);
            if (stepResult.IsFailure)
                return Result<TResult>.Failure(stepResult.Error);

            currentValue = stepResult.Value;
        }

        return currentValue is TResult result
            ? Result<TResult>.Success(result)
            : Result<TResult>.Failure(Error.Create("Invalid type conversion in pipeline"));
    }

    /// <summary>
    /// Pipeline fluente con sintassi più leggibile
    /// Esempio: await InitialValue().Then(Step1).Then(Step2).Then(Step3).ExecuteAsync();
    /// </summary>
    public static PipelineBuilder<T> Then<T>(this Task<Result<T>> initialValue, Func<T, Task<Result<T>>> operation)
        => new PipelineBuilder<T>(initialValue).Then(operation);

    /// <summary>
    /// Pipeline con gestione condizionale
    /// Applica l'operazione solo se la condizione è soddisfatta
    /// </summary>
    public static async Task<Result<T>> ThenIf<T>(
        this Task<Result<T>> pipeline,
        Func<T, bool> condition,
        Func<T, Task<Result<T>>> operation)
    {
        var result = await pipeline;
        if (result.IsFailure || !condition(result.Value))
            return result;

        return await operation(result.Value);
    }

    /// <summary>
    /// Pipeline con retry automatico
    /// </summary>
    public static async Task<Result<T>> ThenWithRetry<T>(
        this Task<Result<T>> pipeline,
        Func<T, Task<Result<T>>> operation,
        int maxAttempts = 3,
        TimeSpan? delay = null)
    {
        var result = await pipeline;
        if (result.IsFailure)
            return result;

        return await RetryAsync(() => operation(result.Value), maxAttempts, delay ?? TimeSpan.FromSeconds(1));
    }

    private static async Task<Result<T>> RetryAsync<T>(
        Func<Task<Result<T>>> operation,
        int maxAttempts,
        TimeSpan delay)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var result = await operation();
            if (result.IsSuccess || attempt == maxAttempts)
                return result;

            await Task.Delay(delay);
        }

        return Result<T>.Failure(Error.Create("Unexpected retry loop exit"));
    }
}

/// <summary>
/// Builder pattern per pipeline fluenti
/// </summary>
public class PipelineBuilder<T>
{
    private readonly List<Func<T, Task<Result<T>>>> _operations = new();
    private readonly Task<Result<T>> _initialValue;

    internal PipelineBuilder(Task<Result<T>> initialValue)
    {
        _initialValue = initialValue;
    }

    /// <summary>
    /// Aggiunge un'operazione alla pipeline
    /// </summary>
    public PipelineBuilder<T> Then(Func<T, Task<Result<T>>> operation)
    {
        _operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Aggiunge un'operazione condizionale
    /// </summary>
    public PipelineBuilder<T> ThenIf(Func<T, bool> condition, Func<T, Task<Result<T>>> operation)
    {
        _operations.Add(async value => condition(value) ? await operation(value) : Result<T>.Success(value));
        return this;
    }

    /// <summary>
    /// Aggiunge side-effect (logging, etc.) senza modificare il valore
    /// </summary>
    public PipelineBuilder<T> Do(Func<T, Task> sideEffect)
    {
        _operations.Add(async value =>
        {
            await sideEffect(value);
            return Result<T>.Success(value);
        });
        return this;
    }

    /// <summary>
    /// Esegue la pipeline
    /// </summary>
    public async Task<Result<T>> ExecuteAsync()
    {
        return await _initialValue.PipelineAsync(_operations.ToArray());
    }
}

/// <summary>
/// Interfaccia per step di pipeline con tipi diversi
/// </summary>
public interface IPipelineStep<TInput, TOutput>
{
    Task<Result<object>> ExecuteAsync(object input);
}

/// <summary>
/// Implementazione base di un pipeline step
/// </summary>
public abstract class PipelineStep<TInput, TOutput> : IPipelineStep<TInput, TOutput>
{
    public abstract Task<Result<TOutput>> ExecuteAsync(TInput input);

    async Task<Result<object>> IPipelineStep<TInput, TOutput>.ExecuteAsync(object input)
    {
        if (input is not TInput typedInput)
            return Result<object>.Failure(Error.Create($"Expected {typeof(TInput).Name}, got {input?.GetType().Name}"));

        var result = await ExecuteAsync(typedInput);
        return result.IsSuccess
            ? Result<object>.Success(result.Value!)
            : Result<object>.Failure(result.Error);
    }
}
