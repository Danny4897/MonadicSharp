#nullable enable
namespace MonadicSharp.Unions;

/// <summary>
/// A discriminated union of two cases (<typeparamref name="T1"/> or <typeparamref name="T2"/>).
/// Provides Map/Bind/Match monadic operators and bridges to MonadicSharp <see cref="Result{T}"/>
/// and <see cref="Either{TLeft,TRight}"/>.
/// When C# 15 native Discriminated Unions GA, replace this type with a native <c>union</c>
/// and keep the MonadicSharp extension blocks for Map/Bind/Match.
/// </summary>
public readonly struct Union2<T1, T2>
{
    private readonly T1? _case1;
    private readonly T2? _case2;
    private readonly byte _tag;   // 1 = T1, 2 = T2

    private Union2(T1 value) { _case1 = value; _tag = 1; }
    private Union2(T2 value) { _case2 = value; _tag = 2; }

    /// <summary>Creates a union holding a <typeparamref name="T1"/> value.</summary>
    public static Union2<T1, T2> Case1(T1 value) => new(value);

    /// <summary>Creates a union holding a <typeparamref name="T2"/> value.</summary>
    public static Union2<T1, T2> Case2(T2 value) => new(value);

    /// <summary>Returns <c>true</c> when this union holds a <typeparamref name="T1"/> value.</summary>
    public bool IsCase1 => _tag == 1;

    /// <summary>Returns <c>true</c> when this union holds a <typeparamref name="T2"/> value.</summary>
    public bool IsCase2 => _tag == 2;

    /// <summary>Exhaustive pattern match — both cases must be handled.</summary>
    public TOut Match<TOut>(Func<T1, TOut> onCase1, Func<T2, TOut> onCase2) =>
        _tag == 1 ? onCase1(_case1!) : onCase2(_case2!);

    /// <summary>Exhaustive pattern match with side effects.</summary>
    public void Match(Action<T1> onCase1, Action<T2> onCase2)
    {
        if (_tag == 1) onCase1(_case1!);
        else onCase2(_case2!);
    }

    /// <summary>
    /// Transforms <typeparamref name="T2"/> (the right / success path by convention),
    /// leaving <typeparamref name="T1"/> untouched.
    /// </summary>
    public Union2<T1, TOut> Map<TOut>(Func<T2, TOut> mapper) =>
        _tag == 2
            ? Union2<T1, TOut>.Case2(mapper(_case2!))
            : Union2<T1, TOut>.Case1(_case1!);

    /// <summary>
    /// Chains a function that returns a <c>Union2</c> on the <typeparamref name="T2"/> path,
    /// short-circuiting on <typeparamref name="T1"/>.
    /// </summary>
    public Union2<T1, TOut> Bind<TOut>(Func<T2, Union2<T1, TOut>> binder) =>
        _tag == 2 ? binder(_case2!) : Union2<T1, TOut>.Case1(_case1!);

    /// <summary>
    /// Converts to <c>Either&lt;T1, T2&gt;</c> using the MonadicSharp convention
    /// (Left = T1 / failure path, Right = T2 / success path).
    /// </summary>
    public Either<T1, T2> ToEither() =>
        _tag == 1
            ? Either<T1, T2>.FromLeft(_case1!)
            : Either<T1, T2>.FromRight(_case2!);

    /// <inheritdoc/>
    public override string ToString() =>
        _tag == 1 ? $"Case1({_case1})" : $"Case2({_case2})";

    public static implicit operator Union2<T1, T2>(T1 value) => Case1(value);
    public static implicit operator Union2<T1, T2>(T2 value) => Case2(value);
}
