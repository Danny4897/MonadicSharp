#nullable enable
namespace MonadicSharp.Unions;

// =============================================================================
// C# 15 NATIVE DISCRIMINATED UNIONS — MIGRATION GUIDE
// =============================================================================
//
// When C# 15 Discriminated Unions ship (GA with .NET 11, expected Nov 2026),
// replace the Union2<T1,T2> / Union3<T1,T2,T3> / Union4<T1,T2,T3,T4> bridge types
// with native `union` declarations.
//
// EXPECTED SYNTAX (preview, subject to change until GA):
//
//   union LlmResult<T>
//   {
//       case Success(T Value);
//       case RateLimit(TimeSpan RetryAfter);
//       case ModelError(string Message, string ModelId);
//   }
//
// EQUIVALENT Union3<T, RateLimitInfo, ModelErrorInfo> TODAY:
//
//   var result = Union3<T, RateLimitInfo, ModelErrorInfo>.Case1(value);
//   var label  = result.Match(
//       onCase1: v   => $"Success: {v}",
//       onCase2: rl  => $"Retry after {rl.RetryAfter}",
//       onCase3: err => $"Model error: {err.Message}");
//
// AFTER MIGRATION (C# 15):
//
//   LlmResult<T> result = new LlmResult<T>.Success(value);
//   string label = result switch
//   {
//       LlmResult<T>.Success(var v)        => $"Success: {v}",
//       LlmResult<T>.RateLimit(var r)      => $"Retry after {r}",
//       LlmResult<T>.ModelError(var m, _)  => $"Model error: {m}"
//   };
//
// BRIDGE EXTENSION METHODS WILL SURVIVE:
//   The Map/Bind/ToResult/ToOption extension methods in UnionExtensions.cs
//   will be re-implemented as extension members (C# 14) on the native DU types,
//   preserving the ROP pipeline API surface.
//
// MIGRATION STEPS:
//   1. Replace Union2<Error,T> with: union Result<T> { case Ok(T); case Fail(Error); }
//      → Or just use MonadicSharp.Result<T> directly (already native).
//   2. Replace Union3/Union4 with native `union` types under #if NET11_0_OR_GREATER.
//   3. Remove implicit conversion operators (DUs don't need them).
//   4. Update Match() calls to exhaustive switch expressions.
//   5. Keep UnionExtensions.cs — update to use extension blocks (C# 14 already done).
//   6. Bump NuGet version to 2.0.0 (breaking change: type removal).
// =============================================================================

// This file is intentionally empty at runtime — it exists only as a migration guide.
// Set <NoneCompile>true</NoneCompile> to exclude from compilation, or leave as-is.
