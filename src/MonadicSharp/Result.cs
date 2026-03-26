using System.Diagnostics.CodeAnalysis;

namespace MonadicSharp;

/// <summary>
/// Represents the result of an operation that can be either Success or Failure.
/// Implements the Result pattern for Railway-Oriented Programming.
/// </summary>
/// <typeparam name="T">The type of the value in case of success</typeparam>
public readonly struct Result<T> : IEquatable<Result<T>>
{
    private readonly T? _value;
    private readonly Error? _error;
    private readonly bool _isSuccess;

    private Result(T value)
    {
        _value = value;
        _error = null;
        _isSuccess = true;
    }

    private Result(Error error)
    {
        _value = default;
        _error = error;
        _isSuccess = false;
    }

    /// <summary>
    /// Creates a successful Result
    /// </summary>
    public static Result<T> Success(T value) =>
        value is null ? throw new ArgumentNullException(nameof(value)) : new Result<T>(value);

    /// <summary>
    /// Creates a failed Result
    /// </summary>
    public static Result<T> Failure(Error error) => new(error);

    /// <summary>
    /// Creates a failed Result with message
    /// </summary>
    public static Result<T> Failure(string message) => new(Error.Create(message));

    /// <summary>
    /// Indicates if the result is successful
    /// </summary>
    public bool IsSuccess => _isSuccess;

    /// <summary>
    /// Indicates if the result is a failure
    /// </summary>
    public bool IsFailure => !_isSuccess;

    /// <summary>
    /// The value in case of success
    /// </summary>
    public T Value => _isSuccess ? _value! : throw new InvalidOperationException("Cannot access value of a failed result");

    /// <summary>
    /// The error in case of failure
    /// </summary>
    public Error Error => !_isSuccess ? _error! : throw new InvalidOperationException("Cannot access error of a successful result");

    /// <summary>
    /// Applies a function to the value if it's a success
    /// </summary>
    public Result<TResult> Map<TResult>(Func<T, TResult> mapper) =>
        _isSuccess ? Result<TResult>.Success(mapper(_value!)) : Result<TResult>.Failure(_error!);

    /// <summary>
    /// Applies a function that returns a Result to the value if it's a success (FlatMap/Bind)
    /// </summary>
    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder) =>
        _isSuccess ? binder(_value!) : Result<TResult>.Failure(_error!);

    /// <summary>
    /// Applies a function to the error if it's a failure
    /// </summary>
    public Result<T> MapError(Func<Error, Error> mapper) =>
        _isSuccess ? this : Failure(mapper(_error!));

    /// <summary>
    /// Executes an action if the result is a success
    /// </summary>
    public Result<T> Do(Action<T> action)
    {
        if (_isSuccess)
            action(_value!);
        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure
    /// </summary>
    public Result<T> DoError(Action<Error> action)
    {
        if (!_isSuccess)
            action(_error!);
        return this;
    }

    /// <summary>
    /// Returns the value if it's a success, otherwise the default value
    /// </summary>
    public T GetValueOrDefault(T defaultValue) =>
        _isSuccess ? _value! : defaultValue;

    /// <summary>
    /// Returns the value if it's a success, otherwise the result of the function
    /// </summary>
    public T GetValueOrDefault(Func<T> defaultValueFactory) =>
        _isSuccess ? _value! : defaultValueFactory();

    /// <summary>
    /// Filters the value based on a predicate
    /// </summary>
    public Result<T> Where(Func<T, bool> predicate, Error errorIfFalse) =>
        _isSuccess && predicate(_value!) ? this : Failure(errorIfFalse);

    /// <summary>
    /// Filters the value based on a predicate
    /// </summary>
    public Result<T> Where(Func<T, bool> predicate, string errorMessage) =>
        Where(predicate, Error.Create(errorMessage));

    /// <summary>
    /// Functional pattern matching
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure) =>
        _isSuccess ? onSuccess(_value!) : onFailure(_error!);

    /// <summary>
    /// Pattern matching with actions
    /// </summary>
    public void Match(Action<T> onSuccess, Action<Error> onFailure)
    {
        if (_isSuccess)
            onSuccess(_value!);
        else
            onFailure(_error!);
    }

    /// <summary>
    /// Converts to Option, losing error information
    /// </summary>
    public Option<T> ToOption() =>
        _isSuccess ? Option<T>.Some(_value!) : Option<T>.None;

    public bool Equals(Result<T> other) =>
        _isSuccess == other._isSuccess &&
        (_isSuccess ? EqualityComparer<T>.Default.Equals(_value, other._value) : _error!.Equals(other._error));

    public override bool Equals(object? obj) =>
        obj is Result<T> other && Equals(other);

    public override int GetHashCode() =>
        _isSuccess ? (_value?.GetHashCode() ?? 0) : _error!.GetHashCode();

    public static bool operator ==(Result<T> left, Result<T> right) =>
        left.Equals(right);

    public static bool operator !=(Result<T> left, Result<T> right) =>
        !left.Equals(right);

    public override string ToString() =>
        _isSuccess ? $"Success({_value})" : $"Failure({_error})";

    // Implicit conversions
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);

    // Conversioni implicite più utili
    public static implicit operator Result<T>(Exception ex) => Failure(Error.FromException(ex));
}

/// <summary>
/// Utility class for creating Result instances
/// </summary>
public static class Result
{
    /// <summary>
    /// Creates a successful Result
    /// </summary>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>
    /// Creates a failed Result
    /// </summary>
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    /// <summary>
    /// Creates a failed Result with message
    /// </summary>
    public static Result<T> Failure<T>(string message) => Result<T>.Failure(message);

    /// <summary>
    /// Combines multiple Results into one, failing if at least one fails
    /// </summary>
    public static Result<T[]> Combine<T>(params Result<T>[] results)
    {
        var failures = results.Where(r => r.IsFailure).ToArray();
        if (failures.Any())
        {
            var combinedError = Error.Combine(failures.Select(f => f.Error).ToArray());
            return Result<T[]>.Failure(combinedError);
        }

        return Result<T[]>.Success(results.Select(r => r.Value).ToArray());
    }

    /// <summary>
    /// Executes a function that can throw exceptions and wraps it in a Result
    /// </summary>
    public static Result<T> Try<T>(Func<T> func)
    {
        try
        {
            return Success(func());
        }
        catch (Exception ex)
        {
            return Failure<T>(Error.FromException(ex));
        }
    }

    /// <summary>
    /// Async version of Try
    /// </summary>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> func)
    {
        try
        {
            var result = await func();
            return Success(result);
        }
        catch (Exception ex)
        {
            return Failure<T>(Error.FromException(ex));
        }
    }
}
