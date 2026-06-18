# MonadicSharp.Unions

Tagged union types (`Union2<T1,T2>`, `Union3<T1,T2,T3>`) with `Map/Bind/Match` monadic operators
and bridges to `Result<T>`, `Option<T>`, and `Either<TLeft,TRight>`.

**Design intent:** this package prepares codebases for C# 15 native Discriminated Unions.
When native DUs GA (November 2026), replace `Union2`/`Union3` with `union` keyword types and keep
the MonadicSharp extension blocks for `Map/Bind/Match/ToResult`.

```bash
dotnet add package MonadicSharp.Unions
```

## Usage

```csharp
using MonadicSharp.Unions;
using MonadicSharp.Unions.Extensions;

// Model an LLM result with three cases
Union3<string, Error, Error> llmResult = UnionExtensions.LlmSuccess("Paris is the capital of France.");

// Map on the success path
var upper = llmResult.Map(s => s.ToUpperInvariant());

// Bridge to Result<T> for pipeline composition
Result<string> result = llmResult.ToResult(
    case2Error: rateLimitErr => rateLimitErr,
    case3Error: modelErr => modelErr);

// Use in existing MonadicSharp pipelines
result
    .Map(text => Summarise(text))
    .Bind(summary => ValidateLength(summary))
    .Match(
        onSuccess: s => Console.WriteLine(s),
        onFailure: e => Console.WriteLine($"Error: {e.Message}"));
```

## Union2 — binary alternative

```csharp
Union2<Error, User> found = Union2<Error, User>.Case2(new User("Alice"));
Result<User> result = found.ToResult(); // Error-or-value bridge

// Custom error factory
Result<User> result2 = found.ToResult(err => Error.NotFound("User", err.Code));
```

## Migration path to C# 15

When C# 15 ships (November 2026):

```csharp
// Before (this package)
Union3<string, Error, Error> result = UnionExtensions.LlmSuccess("hello");

// After (C# 15 native DU + MonadicSharp.Unions extension block)
public union LlmResult<T> { Success(T), RateLimit(Error), ModelError(Error) }

// Extension block added by MonadicSharp.Unions for C# 15
extension<T>(LlmResult<T> result)
{
    public Result<T> ToResult() => result switch { ... };
    public LlmResult<TOut> Map<TOut>(Func<T, TOut> f) => result switch { ... };
}
```
