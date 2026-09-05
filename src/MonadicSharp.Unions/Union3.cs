#nullable enable
namespace MonadicSharp.Unions;

/// <summary>
/// A discriminated union of three cases (<typeparamref name="T1"/>,
/// <typeparamref name="T2"/>, or <typeparamref name="T3"/>).
/// Provides Map/Bind/Match monadic operators and bridges to MonadicSharp
/// <see cref="Result{T}"/>. Especially useful for LLM result types
/// (Success / RateLimit / ModelError) before C# 15 native DUs.
/// </summary>
public readonly struct Union3<T1, T2, T3>
{
    private readonly T1? _case1;
    private readonly T2? _case2;
    private readonly T3? _case3;
    private readonly byte _tag;   // 1 = T1, 2 = T2, 3 = T3

    private Union3(T1 v) { _case1 = v; _tag = 1; }
    private Union3(T2 v) { _case2 = v; _tag = 2; }
    private Union3(T3 v) { _case3 = v; _tag = 3; }

    /// <summary>Creates a union holding a <typeparamref name="T1"/> value.</summary>
    public static Union3<T1, T2, T3> Case1(T1 value) => new(value);

    /// <summary>Creates a union holding a <typeparamref name="T2"/> value.</summary>
    public static Union3<T1, T2, T3> Case2(T2 value) => new(value);

    /// <summary>Creates a union holding a <typeparamref name="T3"/> value.</summary>
    public static Union3<T1, T2, T3> Case3(T3 value) => new(value);

    /// <summary>Returns <c>true</c> when this union holds a <typeparamref name="T1"/> value.</summary>
    public bool IsCase1 => _tag == 1;

    /// <summary>Returns <c>true</c> when this union holds a <typeparamref name="T2"/> value.</summary>
    public bool IsCase2 => _tag == 2;

    /// <summary>Returns <c>true</c> when this union holds a <typeparamref name="T3"/> value.</summary>
    public bool IsCase3 => _tag == 3;

    /// <summary>Exhaustive pattern match — all three cases must be handled.</summary>
    public TOut Match<TOut>(
        Func<T1, TOut> onCase1,
        Func<T2, TOut> onCase2,
        Func<T3, TOut> onCase3) =>
        _tag switch
        {
            1 => onCase1(_case1!),
            2 => onCase2(_case2!),
            _ => onCase3(_case3!)
        };

    /// <summary>Exhaustive pattern match with side effects.</summary>
    public void Match(Action<T1> onCase1, Action<T2> onCase2, Action<T3> onCase3)
    {
        if (_tag == 1) onCase1(_case1!);
        else if (_tag == 2) onCase2(_case2!);
        else onCase3(_case3!);
    }

    /// <summary>
    /// Transforms <typeparamref name="T1"/> (the primary success path by convention),
    /// passing <typeparamref name="T2"/> and <typeparamref name="T3"/> through unchanged.
    /// </summary>
    public Union3<TOut, T2, T3> Map<TOut>(Func<T1, TOut> mapper) =>
        _tag == 1
            ? Union3<TOut, T2, T3>.Case1(mapper(_case1!))
            : _tag == 2
                ? Union3<TOut, T2, T3>.Case2(_case2!)
                : Union3<TOut, T2, T3>.Case3(_case3!);

    /// <summary>
    /// Chains a function on the <typeparamref name="T1"/> path, short-circuiting on
    /// <typeparamref name="T2"/> and <typeparamref name="T3"/>.
    /// </summary>
    public Union3<TOut, T2, T3> Bind<TOut>(Func<T1, Union3<TOut, T2, T3>> binder) =>
        _tag == 1
            ? binder(_case1!)
            : _tag == 2
                ? Union3<TOut, T2, T3>.Case2(_case2!)
                : Union3<TOut, T2, T3>.Case3(_case3!);

    /// <inheritdoc/>
    public override string ToString() =>
        _tag switch
        {
            1 => $"Case1({_case1})",
            2 => $"Case2({_case2})",
            _ => $"Case3({_case3})"
        };
}
