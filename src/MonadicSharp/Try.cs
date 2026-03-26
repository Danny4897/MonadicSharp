namespace MonadicSharp;

public static class Try
{
    public static Result<T> Execute<T>(Func<T> func)
    {
        try
        {
            return Result<T>.Success(func());
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(Error.FromException(ex));
        }
    }

    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> func)
    {
        try
        {
            var result = await func();
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(Error.FromException(ex));
        }
    }

    // Versioni con parametri
    public static Result<TResult> Execute<T, TResult>(T input, Func<T, TResult> func)
    {
        try
        {
            return Result<TResult>.Success(func(input));
        }
        catch (Exception ex)
        {
            return Result<TResult>.Failure(Error.FromException(ex));
        }
    }

    public static async Task<Result<TResult>> ExecuteAsync<T, TResult>(T input, Func<T, Task<TResult>> func)
    {
        try
        {
            var result = await func(input);
            return Result<TResult>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<TResult>.Failure(Error.FromException(ex));
        }
    }
}