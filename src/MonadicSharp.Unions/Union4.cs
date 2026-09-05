#nullable enable
namespace MonadicSharp.Unions;

/// <summary>
/// A discriminated union of four cases (<typeparamref name="T1"/>, <typeparamref name="T2"/>,
/// <typeparamref name="T3"/>, or <typeparamref name="T4"/>).
/// Provides Map/Bind/Match monadic operators and bridges to MonadicSharp primitives.
/// When C# 15 native Discriminated Unions ship, replace with a native <c>union</c> declaration
/// and keep the MonadicSharp extension methods for Map/Bind/Match.
/// </summary>
public readonly struct Union4<T1, T2, T3, T4>
{
    private readonly T1? _case1;
    private readonly T2? _case2;
    private readonly T3? _case3;
    private readonly T4? _case4;
    private readonly byte _tag;   // 1..4

    private Union4(T1 v) { _case1 = v; _tag = 1; }
    private Union4(T2 v) { _case2 = v; _tag = 2; }
    private Union4(T3 v) { _case3 = v; _tag = 3; }
    private Union4(T4 v) { _case4 = v; _tag = 4; }

    /// <summary>Creates a union holding a <typeparamref name="T1"/> value.</summary>
    public static Union4<T1, T2, T3, T4> Case1(T1 value) => new(value);

    /// <summary>Creates a union holding a <typeparamref name="T2"/> value.</summary>
    public static Union4<T1, T2, T3, T4> Case2(T2 value) => new(value);

    /// <summary>Creates a union holding a <typeparamref name="T3"/> value.</summary>
    public static Union4<T1, T2, T3, T4> Case3(T3 value) => new(value);

    /// <summary>Creates a union holding a <typeparamref name="T4"/> value.</summary>
    public static Union4<T1, T2, T3, T4> Case4(T4 value) => new(value);

    /// <summary>Returns <c>true</c> when this union holds a <typeparamref name="T1"/> value.</summary>
    public bool IsCase1 => _tag == 1;
    /// <summary>Returns <c>true</c> when this union holds a <typeparamref name="T2"/> value.</summary>
    public bool IsCase2 => _tag == 2;
    /// <summary>Returns <c>true</c> when this union holds a <typeparamref name="T3"/> value.</summary>
    public bool IsCase3 => _tag == 3;
    /// <summary>Returns <c>true</c> when this union holds a <typeparamref name="T4"/> value.</summary>
    public bool IsCase4 => _tag == 4;

    /// <summary>Exhaustive pattern match — all four cases must be handled.</summary>
    public TOut Match<TOut>(
        Func<T1, TOut> onCase1,
        Func<T2, TOut> onCase2,
        Func<T3, TOut> onCase3,
        Func<T4, TOut> onCase4) =>
        _tag switch
        {
            1 => onCase1(_case1!),
            2 => onCase2(_case2!),
            3 => onCase3(_case3!),
            _ => onCase4(_case4!)
        };

    /// <summary>Exhaustive pattern match with side effects.</summary>
    public void Match(
        Action<T1> onCase1,
        Action<T2> onCase2,
        Action<T3> onCase3,
        Action<T4> onCase4)
    {
        switch (_tag)
        {
            case 1: onCase1(_case1!); break;
            case 2: onCase2(_case2!); break;
            case 3: onCase3(_case3!); break;
            default: onCase4(_case4!); break;
        }
    }

    /// <summary>
    /// Transforms <typeparamref name="T1"/> (the primary success path by convention),
    /// passing other cases through unchanged.
    /// </summary>
    public Union4<TOut, T2, T3, T4> Map<TOut>(Func<T1, TOut> mapper) =>
        _tag switch
        {
            1 => Union4<TOut, T2, T3, T4>.Case1(mapper(_case1!)),
            2 => Union4<TOut, T2, T3, T4>.Case2(_case2!),
            3 => Union4<TOut, T2, T3, T4>.Case3(_case3!),
            _ => Union4<TOut, T2, T3, T4>.Case4(_case4!)
        };

    /// <summary>
    /// Chains a function on <typeparamref name="T1"/>, short-circuiting on other cases.
    /// </summary>
    public Union4<TOut, T2, T3, T4> Bind<TOut>(Func<T1, Union4<TOut, T2, T3, T4>> binder) =>
        _tag == 1
            ? binder(_case1!)
            : _tag switch
            {
                2 => Union4<TOut, T2, T3, T4>.Case2(_case2!),
                3 => Union4<TOut, T2, T3, T4>.Case3(_case3!),
                _ => Union4<TOut, T2, T3, T4>.Case4(_case4!)
            };

    /// <inheritdoc/>
    public override string ToString() =>
        _tag switch
        {
            1 => $"Case1({_case1})",
            2 => $"Case2({_case2})",
            3 => $"Case3({_case3})",
            _ => $"Case4({_case4})"
        };

    public static implicit operator Union4<T1, T2, T3, T4>(T1 value) => Case1(value);
    public static implicit operator Union4<T1, T2, T3, T4>(T2 value) => Case2(value);
    public static implicit operator Union4<T1, T2, T3, T4>(T3 value) => Case3(value);
    public static implicit operator Union4<T1, T2, T3, T4>(T4 value) => Case4(value);
}
