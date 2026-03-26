# Try

`Try` is a static helper class that wraps any throwing code in a `Result<T>`. It turns exceptions into structured `Error` values — the bridge between legacy throwing APIs and Railway-Oriented pipelines.

```csharp
public static class Try
```

## Why Try?

In a MonadicSharp pipeline you never write `try/catch` inside `Bind` or `Map`. Instead, wrap the I/O boundary with `Try.Execute` or `Try.ExecuteAsync` once — and the rest of the pipeline stays clean.

```csharp
// Without Try — exception leaks out of the pipeline
Result<Config> config = LoadFile(path)
    .Bind(content => ParseJson(content)); // throws JsonException if malformed

// With Try — exception is captured as a structured Error
Result<Config> config = Try.Execute(() => File.ReadAllText(path))
    .Bind(content => Try.Execute(() => JsonSerializer.Deserialize<Config>(content)));
```

## Try.Execute

Synchronous wrapper. Returns `Success(value)` or `Failure(Error.FromException(ex))`.

```csharp
Result<int>    r1 = Try.Execute(() => int.Parse(input));
Result<Config> r2 = Try.Execute(() => JsonSerializer.Deserialize<Config>(json));
```

### With input parameter

Avoids closure allocation when the input is already available.

```csharp
Result<Config> r = Try.Execute(json, s => JsonSerializer.Deserialize<Config>(s));
// Equivalent to: Try.Execute(() => JsonSerializer.Deserialize<Config>(json))
```

## Try.ExecuteAsync

Async wrapper. Returns `Task<Result<T>>`.

```csharp
Task<Result<string>> r1 = Try.ExecuteAsync(() => File.ReadAllTextAsync(path));
Task<Result<User>>   r2 = Try.ExecuteAsync(() => _httpClient.GetFromJsonAsync<User>(url));
```

### With input parameter

```csharp
Task<Result<string>> r = Try.ExecuteAsync(path, p => File.ReadAllTextAsync(p));
```

## Inside a pipeline

`Try.ExecuteAsync` composes naturally with `Bind`:

```csharp
public async Task<Result<InvoiceDto>> GenerateInvoiceAsync(int orderId) =>
    await Try.ExecuteAsync(() => _db.Orders.FindAsync(orderId))
        .Bind(order  => Try.ExecuteAsync(() => _pricing.CalculateAsync(order)))
        .Bind(priced => Try.ExecuteAsync(() => _pdf.RenderAsync(priced)))
        .Map(pdf     => new InvoiceDto(pdf));
```

No `try/catch` — exceptions from EF Core, HTTP clients, or PDF rendering are all captured at their source.

## Error produced by Try

The captured `Error` has:
- `Type` = `ErrorType.Exception`
- `Code` = exception type name (e.g. `"FILENOTFOUNDEXCEPTION"`)
- `Message` = `exception.Message`
- `Metadata["ExceptionType"]` = full type name
- `Metadata["StackTrace"]` = stack trace string

```csharp
Result<string> r = Try.Execute(() => File.ReadAllText("/missing.txt"));
// r.Error.Type    → ErrorType.Exception
// r.Error.Code    → "FILENOTFOUNDEXCEPTION"
// r.Error.Message → "Could not find file '/missing.txt'."
```

## Try vs Result.Try

`Result.Try` and `Result.TryAsync` (instance methods on `Result`) do the same thing when you already have a result in scope. Use whichever reads more clearly:

```csharp
// Equivalent — choose for readability
Result<int> a = Try.Execute(() => int.Parse(s));
Result<int> b = Result.Try(() => int.Parse(s));
```

## API reference

| Member | Description |
|--------|-------------|
| `Try.Execute<T>(Func<T>)` | Wrap synchronous throwing code |
| `Try.Execute<T,TResult>(T, Func<T,TResult>)` | Same, with input parameter |
| `Try.ExecuteAsync<T>(Func<Task<T>>)` | Wrap async throwing code |
| `Try.ExecuteAsync<T,TResult>(T, Func<T,Task<TResult>>)` | Same, with input parameter |
