namespace MonadicSharp;

/// <summary>
/// Represents a type with only one value, used in functional programming
/// when we need to return something but there's no meaningful value
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>
    /// The single instance of Unit
    /// </summary>
    public static readonly Unit Value = new();

    /// <summary>
    /// Implicit conversion from Unit to void operations
    /// </summary>
    public static implicit operator Unit(ValueTuple _) => Value;

    /// <summary>
    /// Returns true if obj is Unit
    /// </summary>
    public override bool Equals(object? obj) => obj is Unit;

    /// <summary>
    /// Returns true (all Unit instances are equal)
    /// </summary>
    public bool Equals(Unit other) => true;

    /// <summary>
    /// Returns 0 (all Unit instances have the same hash code)
    /// </summary>
    public override int GetHashCode() => 0;

    /// <summary>
    /// Returns "()"
    /// </summary>
    public override string ToString() => "()";

    /// <summary>
    /// Equality operator
    /// </summary>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>
    /// Inequality operator
    /// </summary>
    public static bool operator !=(Unit left, Unit right) => false;
}
