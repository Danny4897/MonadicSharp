#nullable enable
namespace MonadicSharp.Unions.Extensions;

/// <summary>
/// Async variants of Match/Map/Bind for <see cref="Union2{T1,T2}"/>,
/// <see cref="Union3{T1,T2,T3}"/>, and <see cref="Union4{T1,T2,T3,T4}"/>.
/// On .NET 11+, these methods benefit from Runtime Async zero-allocation execution.
/// </summary>
public static class UnionAsyncExtensions
{
    // ── Union2 ──────────────────────────────────────────────────────────────

    /// <summary>Async exhaustive match over <see cref="Union2{T1,T2}"/>.</summary>
    public static Task<TOut> MatchAsync<T1, T2, TOut>(
        this Union2<T1, T2> union,
        Func<T1, Task<TOut>> onCase1,
        Func<T2, Task<TOut>> onCase2) =>
        union.Match(onCase1, onCase2);

    /// <summary>Async map over the <typeparamref name="T2"/> path of <see cref="Union2{T1,T2}"/>.</summary>
    public static async Task<Union2<T1, TOut>> MapAsync<T1, T2, TOut>(
        this Union2<T1, T2> union,
        Func<T2, Task<TOut>> mapper) =>
        union.IsCase2
            ? Union2<T1, TOut>.Case2(await mapper(union.Match(t1 => default(T2)!, t2 => t2)).ConfigureAwait(false))
            : union.Match(Union2<T1, TOut>.Case1, _ => default!);

    // ── Union3 ──────────────────────────────────────────────────────────────

    /// <summary>Async exhaustive match over <see cref="Union3{T1,T2,T3}"/>.</summary>
    public static Task<TOut> MatchAsync<T1, T2, T3, TOut>(
        this Union3<T1, T2, T3> union,
        Func<T1, Task<TOut>> onCase1,
        Func<T2, Task<TOut>> onCase2,
        Func<T3, Task<TOut>> onCase3) =>
        union.Match(onCase1, onCase2, onCase3);

    // ── Union4 ──────────────────────────────────────────────────────────────

    /// <summary>Async exhaustive match over <see cref="Union4{T1,T2,T3,T4}"/>.</summary>
    public static Task<TOut> MatchAsync<T1, T2, T3, T4, TOut>(
        this Union4<T1, T2, T3, T4> union,
        Func<T1, Task<TOut>> onCase1,
        Func<T2, Task<TOut>> onCase2,
        Func<T3, Task<TOut>> onCase3,
        Func<T4, Task<TOut>> onCase4) =>
        union.Match(onCase1, onCase2, onCase3, onCase4);

#if NET11_0_OR_GREATER
    // ValueTask overloads for Runtime Async zero-alloc hot paths

    /// <summary>ValueTask async match over <see cref="Union2{T1,T2}"/> (.NET 11+).</summary>
    public static ValueTask<TOut> MatchAsync<T1, T2, TOut>(
        this Union2<T1, T2> union,
        Func<T1, ValueTask<TOut>> onCase1,
        Func<T2, ValueTask<TOut>> onCase2) =>
        union.Match(onCase1, onCase2);

    /// <summary>ValueTask async match over <see cref="Union3{T1,T2,T3}"/> (.NET 11+).</summary>
    public static ValueTask<TOut> MatchAsync<T1, T2, T3, TOut>(
        this Union3<T1, T2, T3> union,
        Func<T1, ValueTask<TOut>> onCase1,
        Func<T2, ValueTask<TOut>> onCase2,
        Func<T3, ValueTask<TOut>> onCase3) =>
        union.Match(onCase1, onCase2, onCase3);

    /// <summary>ValueTask async match over <see cref="Union4{T1,T2,T3,T4}"/> (.NET 11+).</summary>
    public static ValueTask<TOut> MatchAsync<T1, T2, T3, T4, TOut>(
        this Union4<T1, T2, T3, T4> union,
        Func<T1, ValueTask<TOut>> onCase1,
        Func<T2, ValueTask<TOut>> onCase2,
        Func<T3, ValueTask<TOut>> onCase3,
        Func<T4, ValueTask<TOut>> onCase4) =>
        union.Match(onCase1, onCase2, onCase3, onCase4);
#endif
}
