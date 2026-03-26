# Green Code

Green Code is a set of structural rules for writing C# where errors are values, not exceptions. Code is green when the type system fully describes what can go wrong — no hidden control flow, no nullable surprises, no exception-based branching.

## The core idea

In standard C#, a method signature like this:

```csharp
public Order ProcessOrder(Guid orderId)
```

tells you nothing about what can fail. The method might throw `InvalidOperationException`, `DbException`, `ArgumentNullException`, or nothing at all. The compiler has no opinion.

A green method makes failure explicit:

```csharp
public Result<Order> ProcessOrder(Guid orderId)
```

The caller must handle both cases. The compiler enforces it. There is no way to accidentally ignore the error.

## Railway-Oriented Programming in brief

Railway-Oriented Programming (ROP) is a pattern where every operation returns a result that is either a success value or a typed error. Operations are composed with `Bind` — if any step fails, the failure propagates automatically without branching.

```csharp
return await ValidateInput(request)
    .BindAsync(FindOrder)
    .BindAsync(ApplyDiscount)
    .BindAsync(SaveOrder);
```

If `ValidateInput` returns a failure, `FindOrder`, `ApplyDiscount`, and `SaveOrder` are never called. The failure value reaches the caller unchanged.

## The 10 Green Code principles

### GC001 — No bare throw
Errors must be return values. `throw` is only permitted when re-throwing inside a framework-level catch that converts to `Result<T>`.

### GC002 — No catch without re-wrap
Every `catch` block must return a typed `Result.Fail(...)` or call the next handler. Swallowing an exception in silence or re-throwing it as-is is not green.

### GC003 — No null return on failure path
If a method can return "nothing" as a valid outcome, the return type must be `Option<T>`. `null` is not an acceptable absence value.

### GC004 — No bool return for failable operations
`bool` erases the reason for failure. Use `Result<T>` when the operation can fail in a meaningful way.

### GC005 — No swallowed exceptions
`catch (Exception) { }` with no action is forbidden. It hides bugs permanently.

### GC006 — Compose with Bind, not if-chains
Manual `if (result.IsSuccess) { ... }` nesting is a sign that `BindAsync` or `MapAsync` should be used instead.

### GC007 — Boolean failable operations become Result
A method named `TrySave`, `TryParse`, or `TryConnect` that returns `bool` should return `Result<T>`.

### GC008 — No nullable reference types for optional domain values
`User?` as a field or return type means "might be absent." That belongs in `Option<User>`.

### GC009 — Async methods return `Task<Option<T>>`, not `Task<T?>`
Nullable task returns are hard to compose. Wrap them in `Option<T>` for consistent pipeline behavior.

### GC010 — All async methods accept CancellationToken
Every `async` method must have a `CancellationToken ct = default` parameter. This is required for cooperative cancellation in pipelines.

## Why green code is safer for AI-assisted development

AI coding assistants generate code by pattern-matching training data. Standard C# training data is full of try/catch patterns, nullable returns, and exception-based control flow — because that is how most C# is written.

When OpenCode or another AI assistant generates a method, it will default to these patterns unless it has a strong structural signal to do otherwise. Green Code provides that signal:

- A codebase with consistent `Result<T>` and `Option<T>` usage creates a pattern strong enough for the AI to follow.
- `/forge-analyze` gives immediate feedback when the AI generates non-green code.
- `/migrate` corrects it in a single step.

The result is that AI-generated code in a green codebase tends to be green too, without requiring detailed prompting.

## How the Green Score is calculated

Each file starts at 100 points. Violations deduct points based on severity:

| Severity | Deduction per violation |
|---|---|
| error | 10 |
| warning | 5 |
| hint | 1 |

The file score floor is 0. Project score is the line-count-weighted average across all files. Files in `excludePaths` in `.monadicleaf.json` are not included.

A project at 100/100 has no violations of any severity in any included file.
