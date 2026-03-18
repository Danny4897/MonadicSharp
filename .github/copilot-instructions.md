# GitHub Copilot Instructions — MonadicSharp
# Place this file at: .github/copilot-instructions.md
# Copilot will apply these rules automatically in every suggestion for this repository.

This project uses **MonadicSharp** — Railway-Oriented Programming for C#.
All code suggestions must follow the patterns below. Never suggest `try/catch` or `throw`
for expected business failures. Never suggest `null` returns. Always use `Result<T>`.

---

## The One Rule

**Every method that can fail must return `Result<T>` or `Task<Result<T>>`.**

```csharp
// Copilot: suggest this
public Result<User>              GetUser(int id)       { ... }
public Task<Result<Order>>       PlaceOrderAsync(...)  { ... }
public Task<Result<IList<User>>> SearchUsersAsync(...) { ... }

// Copilot: NEVER suggest this
public User  GetUser(int id)      { ... }   // hides failure
public User? GetUser(int id)      { ... }   // null = hidden failure
```

---

## Chaining Operations

```csharp
// RIGHT — full railway pipeline
public async Task<Result<OrderConfirmation>> ConfirmAsync(ConfirmOrderRequest request)
{
    return await ValidateRequest(request)
        .Bind(req   => _inventory.CheckAvailabilityAsync(req))
        .Bind(items => _payment.AuthorizeAsync(items))
        .Bind(auth  => _orders.CommitAsync(auth))
        .Map(order  => new OrderConfirmation(order.Id, order.Total));
}

// WRONG — do not suggest this
public async Task<OrderConfirmation> ConfirmAsync(ConfirmOrderRequest request)
{
    try {
        var order = await _orders.CommitAsync(request);
        return new OrderConfirmation(order.Id, order.Total);
    } catch (Exception ex) { throw; }
}
```

## Error Construction

```csharp
// RIGHT — typed, semantic, mappable to HTTP
Error.Validation("Email is required", field: "email")
Error.NotFound("Product", productId.ToString())
Error.Conflict("Email already registered", resource: "email")
Error.Forbidden("Requires billing:write scope")
Error.Unexpected("Unhandled state", exception)

// WRONG — never use raw strings or throw
Result<User>.Failure("something went wrong")
throw new NotFoundException(...)
throw new ValidationException(...)
```

## Match at the Boundary

```csharp
// Controllers and endpoints only — this is the ONLY place to unwrap
return result.Match(
    onSuccess: dto   => Ok(dto),
    onFailure: error => error.Type switch
    {
        ErrorType.NotFound   => NotFound(new  { error.Message }),
        ErrorType.Validation => BadRequest(new { error.Message, error.Field }),
        ErrorType.Conflict   => Conflict(new  { error.Message }),
        ErrorType.Forbidden  => Forbid(),
        _                    => Problem(error.Message)
    });
```

## Validation — Collect All Errors

```csharp
// RIGHT — collects ALL failures, not just the first
var validation = new[]
{
    ValidateName(request.Name),
    ValidateEmail(request.Email),
    ValidatePhone(request.Phone)
}.Sequence();

return await validation
    .Bind(fields => _users.CreateAsync(fields));
```

## Legacy / External Code

```csharp
// Wrap anything that throws
Result<string>      content = Result.Try(() => File.ReadAllText(path));
Result<JsonElement> json    = Result.Try(() => JsonDocument.Parse(raw).RootElement);
Result<string>      resp    = await Result.TryAsync(() => httpClient.GetStringAsync(url));
```

## AI Agent Pipelines

```csharp
// Use MonadicSharp.AI for LLM calls — never raw try/catch
var result = await Result.TryAsync(() => _llm.CompleteAsync(prompt))
    .WithRetry(maxAttempts: 3, initialDelay: TimeSpan.FromSeconds(2));

// Multi-step agents with execution tracing
var agentResult = await AgentResult
    .StartTrace("SummarizeAgent", userInput)
    .Step("Retrieve", q   => _search.FindChunksAsync(q))
    .Step("Generate", ctx => _llm.CompleteAsync(ctx))
    .ExecuteAsync();

// Self-healing JSON output (MonadicSharp.Recovery)
var output = await GenerateStructuredAsync()
    .StartFixBranchAsync(
        when:     AiRecoveryPredicates.InvalidOutput(),
        recovery: (err, attempt) => RepairWithLlmAsync(err, attempt),
        maxAttempts: 2);
```

## Repository Interface Template

```csharp
public interface IRepository<T, TId>
{
    Task<Result<T>>         GetByIdAsync(TId id);
    Task<Result<T>>         SaveAsync(T entity);
    Task<Result<IList<T>>>  GetAllAsync();
    Task<Result<Unit>>      DeleteAsync(TId id);
}
```

---

## What to AVOID in suggestions

| Pattern | Replace with |
|---------|-------------|
| `try { } catch { throw; }` | `Result.Try(...)` |
| `if (x == null) throw new ...` | `return Error.NotFound(...)` |
| `throw new ValidationException` | `return Error.Validation(...)` |
| `return null` | `return Option<T>.None` or `Error.NotFound(...)` |
| `.Value` without guard | `.Match(...)` or `.Bind(...)` |
| `Task<User>` for fallible ops | `Task<Result<User>>` |

---

Docs: https://github.com/Danny4897/MonadicSharp
NuGet: https://www.nuget.org/profiles/Klexir
