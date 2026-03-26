# Either\<TLeft, TRight\>

`Either<TLeft, TRight>` holds exactly one of two possible values — a `Left` or a `Right`. Unlike `Result<T>`, neither side carries a special meaning: both `TLeft` and `TRight` are domain types.

```csharp
public readonly struct Either<TLeft, TRight> // value type — zero heap allocation
```

Convention (not enforced): **`Left` = error / failure**, **`Right` = success / value**.
For error handling, prefer `Result<T>`. Use `Either` when both branches are meaningful domain values — e.g., returning a cached result vs. a freshly-fetched one.

## Creating

```csharp
Either<string, int> left  = Either<string, int>.FromLeft("error message");
Either<string, int> right = Either<string, int>.FromRight(42);
```

## Inspecting

```csharp
either.IsLeft;   // true if Left
either.IsRight;  // true if Right

// Each side is wrapped in Option<T> — safe to access regardless of state
Option<string> l = either.Left;   // Some("error") or None
Option<int>    r = either.Right;  // Some(42) or None
```

## Match

Unwrap both cases and return a value.

```csharp
string display = either.Match(
    onLeft:  err   => $"Error: {err}",
    onRight: value => $"Value: {value}");

// Void overload
either.Match(
    onLeft:  err   => logger.LogError(err),
    onRight: value => Console.WriteLine(value));
```

## Map

Transform the `Right` value. If `Left`, passes through unchanged.

```csharp
Either<string, string> formatted = Either<string, int>.FromRight(42)
    .Map(n => $"Result: {n}");
// → Right("Result: 42")

Either<string, int>.FromLeft("invalid")
    .Map(n => n * 2);
// → Left("invalid")  — Map not called
```

## Bind

Chain an operation that itself returns an `Either`. Short-circuits on `Left`.

```csharp
Either<string, Config> loaded = LoadFile(path)
    .Bind(ParseConfig)
    .Bind(ValidateConfig);
// If LoadFile returns Left, ParseConfig and ValidateConfig are skipped.
```

## When to use Either vs Result

| Scenario | Use |
|---------|-----|
| Operation that can fail with an `Error` | `Result<T>` |
| Two meaningful domain outcomes (e.g., cached vs live) | `Either<T1, T2>` |
| Routing to different handlers based on type | `Either<T1, T2>` |
| Interop with a library that returns `Either` | `Either<TLeft, TRight>` |

```csharp
// Example: distinguish cache hit from database fetch
Either<CachedUser, FreshUser> GetUser(int id) =>
    _cache.TryGet(id) is { HasValue: true } cached
        ? Either<CachedUser, FreshUser>.FromLeft(cached.Value)
        : Either<CachedUser, FreshUser>.FromRight(_db.Find(id));
```

## Converting to Result

```csharp
Result<int> result = either.Match(
    onLeft:  err   => Result<int>.Failure(Error.Create(err)),
    onRight: value => Result<int>.Success(value));
```

## API reference

| Member | Description |
|--------|-------------|
| `IsLeft` / `IsRight` | State predicates |
| `Left` | `Option<TLeft>` — safe accessor |
| `Right` | `Option<TRight>` — safe accessor |
| `Either<L,R>.FromLeft(value)` | Wrap a Left value |
| `Either<L,R>.FromRight(value)` | Wrap a Right value |
| `Map<TResult>(Func<TRight, TResult>)` | Transform Right, pass Left through |
| `Bind<TResult>(Func<TRight, Either<TLeft,TResult>>)` | Chain fallible Right operation |
| `Match<TResult>(onLeft, onRight)` | Unwrap — returns value |
| `Match(onLeft, onRight)` | Unwrap — void |
