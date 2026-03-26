# Option\<T\>

`Option<T>` represents a value that may or may not be present. It eliminates null references by making absence explicit in the type system.

```csharp
public readonly struct Option<T> // value type — zero heap allocation
```

Two states: **`Some(value)`** when a value is present, **`None`** when it is absent.

## Creating options

```csharp
// Explicit
Option<User> some = Option<User>.Some(user);
Option<User> none = Option<User>.None;

// From a nullable — the idiomatic bridge from legacy code
Option<User> opt  = Option<User>.From(nullableUser); // None if null, Some otherwise
Option<User> opt2 = Option.From(nullableUser);       // static helper

// Implicit conversion from T
Option<string> name = "Alice"; // → Some("Alice")
Option<string> empty = Option.None; // → None (any type)
```

## Map

Transform the value if present. If `None`, the result is `None`.

```csharp
Option<string> email = FindUser(id)
    .Map(u => u.Email); // None if user not found, Some(email) otherwise
```

## Bind

Chain an operation that itself returns an `Option<T>`.

```csharp
Option<string> city = FindUser(id)
    .Bind(u => FindProfile(u.Id))
    .Map(p => p.City);
```

## Where (filter)

Converts `Some` to `None` if the predicate is not satisfied.

```csharp
Option<User> active = FindUser(id)
    .Where(u => u.IsActive);
// Returns None if user is found but not active.
```

## Match

Unwrap at the boundary.

```csharp
// Returns a value from both branches
string display = FindUser(id).Match(
    onSome: user  => user.Name,
    onNone: ()    => "Guest");

// Void overload
FindUser(id).Match(
    onSome: user  => Console.WriteLine($"Found: {user.Name}"),
    onNone: ()    => Console.WriteLine("Not found"));
```

## GetValueOrDefault

Safe extraction without pattern matching.

```csharp
string name = FindUser(id)
    .Map(u => u.Name)
    .GetValueOrDefault("Anonymous");

// Lazy factory — only called when None
User user = FindUser(id)
    .GetValueOrDefault(() => User.Guest());
```

## Do

Side effect if value is present. Returns `this` unchanged.

```csharp
FindUser(id)
    .Do(u => _cache.Set(u.Id, u));
```

## Converting between Option and Result

```csharp
// Option → Result (adds error context)
Result<User> result = FindUser(id).ToOption()
    // ... not directly, use the Result pattern:
Result<User> r = FindUser(id)
    .Match(
        onSome: user => Result<User>.Success(user),
        onNone: ()   => Result<User>.Failure(Error.NotFound("User", id.ToString())));

// Result → Option (drops error)
Option<User> opt = GetUser(id).ToOption();
```

## When to use Option vs Result

| Scenario | Use |
|---------|-----|
| Database lookup that may find nothing | `Option<T>` |
| Operation that can fail with a reason | `Result<T>` |
| Optional configuration / nullable setting | `Option<T>` |
| HTTP call, I/O, external API | `Result<T>` |
| Collection `.FirstOrDefault()` | `Option<T>` |

## API reference

| Member | Description |
|--------|-------------|
| `HasValue` / `IsNone` | State predicates |
| `Option<T>.Some(value)` | Wrap a value |
| `Option<T>.None` | Absence sentinel |
| `Option<T>.From(value?)` | Bridge from nullable |
| `Map<TResult>(Func<T, TResult>)` | Transform if present |
| `Bind<TResult>(Func<T, Option<TResult>>)` | Chain optional operation |
| `Where(Func<T, bool>)` | Filter by predicate |
| `Do(Action<T>)` | Side effect if present |
| `Match<TResult>(onSome, onNone)` | Unwrap — returns value |
| `Match(onSome, onNone)` | Unwrap — void |
| `GetValueOrDefault(T)` | Safe extraction with fallback |
| `GetValueOrDefault(Func<T>)` | Lazy fallback |
