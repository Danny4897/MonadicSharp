using System.Diagnostics.CodeAnalysis;

namespace MonadicSharp;

/// <summary>
/// Represents an optional value that can be either present (Some) or absent (None).
/// Implements the Option/Maybe pattern to avoid null reference exceptions.
/// </summary>
/// <typeparam name="T">The type of the contained value</typeparam>
public readonly struct Option<T> : IEquatable<Option<T>>
{
    private readonly T? _value;
    private readonly bool _hasValue;

    private Option(T value)
    {
        _value = value;
        _hasValue = true;
    }

    /// <summary>
    /// Creates a Some instance containing a value
    /// </summary>
    public static Option<T> Some(T value) =>
        value is null ? throw new ArgumentNullException(nameof(value)) : new Option<T>(value);

    /// <summary>
    /// Represents the absence of a value
    /// </summary>
    public static Option<T> None => new();

    /// <summary>
    /// Creates an Option from a nullable value
    /// </summary>
    public static Option<T> From(T? value) =>
        value is null ? None : Some(value);

    /// <summary>
    /// Indicates if the Option contains a value
    /// </summary>
    public bool HasValue => _hasValue;

    /// <summary>
    /// Indicates if the Option is empty
    /// </summary>
    public bool IsNone => !_hasValue;

    /// <summary>
    /// Applies a function to the value if present
    /// </summary>
    public Option<TResult> Map<TResult>(Func<T, TResult> mapper) =>
        _hasValue ? Option<TResult>.Some(mapper(_value!)) : Option<TResult>.None;

    /// <summary>
    /// Applies a function that returns an Option to the value if present (FlatMap/Bind)
    /// </summary>
    public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> binder) =>
        _hasValue ? binder(_value!) : Option<TResult>.None;

    /// <summary>
    /// Returns the value if present, otherwise the default value
    /// </summary>
    public T GetValueOrDefault(T defaultValue) =>
        _hasValue ? _value! : defaultValue;

    /// <summary>
    /// Returns the value if present, otherwise the result of the function
    /// </summary>
    public T GetValueOrDefault(Func<T> defaultValueFactory) =>
        _hasValue ? _value! : defaultValueFactory();

    /// <summary>
    /// Executes an action if the value is present
    /// </summary>
    public Option<T> Do(Action<T> action)
    {
        if (_hasValue)
            action(_value!);
        return this;
    }

    /// <summary>
    /// Filters the value based on a predicate
    /// </summary>
    public Option<T> Where(Func<T, bool> predicate) =>
        _hasValue && predicate(_value!) ? this : None;

    /// <summary>
    /// Functional pattern matching
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSome, Func<TResult> onNone) =>
        _hasValue ? onSome(_value!) : onNone();

    /// <summary>
    /// Pattern matching with actions
    /// </summary>
    public void Match(Action<T> onSome, Action onNone)
    {
        if (_hasValue)
            onSome(_value!);
        else
            onNone();
    }

    public bool Equals(Option<T> other) =>
        _hasValue == other._hasValue &&
        (!_hasValue || EqualityComparer<T>.Default.Equals(_value, other._value));

    public override bool Equals(object? obj) =>
        obj is Option<T> other && Equals(other);

    public override int GetHashCode() =>
        _hasValue ? (_value?.GetHashCode() ?? 0) : 0;

    public static bool operator ==(Option<T> left, Option<T> right) =>
        left.Equals(right);

    public static bool operator !=(Option<T> left, Option<T> right) =>
        !left.Equals(right);

    public override string ToString() =>
        _hasValue ? $"Some({_value})" : "None";

    // Implicit conversions
    public static implicit operator Option<T>(T value) => From(value);
    public static implicit operator Option<T>(Option.NoneType _) => None;
}

/// <summary>
/// Utility class for creating Option instances
/// </summary>
public static class Option
{
    public sealed class NoneType
    {
        internal NoneType() { }
    }

    /// <summary>
    /// Represents None for any type
    /// </summary>
    public static readonly NoneType None = new();

    /// <summary>
    /// Creates a Some containing the specified value
    /// </summary>
    public static Option<T> Some<T>(T value) => Option<T>.Some(value);

    /// <summary>
    /// Creates an Option from a nullable value
    /// </summary>
    public static Option<T> From<T>(T? value) => Option<T>.From(value);
}
