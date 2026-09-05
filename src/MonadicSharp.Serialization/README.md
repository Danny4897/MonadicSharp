# MonadicSharp.Serialization

AOT-safe `System.Text.Json` converters for MonadicSharp primitives.

```bash
dotnet add package MonadicSharp.Serialization
```

## Formats

| Type | Success / Some | Failure / None |
|---|---|---|
| `Result<T>` | `{"ok":true,"value":<T>}` | `{"ok":false,"error":"msg","code":"CODE","type":"NotFound"}` |
| `Option<T>` | `{"hasValue":true,"value":<T>}` | `{"hasValue":false}` |
| `Either<L,R>` | `{"isRight":true,"value":<R>}` | `{"isRight":false,"left":<L>}` |

## Setup

```csharp
// ASP.NET Core
builder.Services.Configure<JsonOptions>(opts =>
    opts.JsonSerializerOptions.AddMonadicSharp());

// Standalone
var opts = SerializationExtensions.CreateDefaultOptions();
var json = result.ToJson(opts);
var back = SerializationExtensions.FromJson<MyType>(json, opts);
```

## Notes

- Converters register **per `JsonSerializerOptions` instance** — no global state.
- Factory approach uses reflection for generic type resolution. For Native AOT, use
  `[DynamicDependency]` on your converter registration or await a future source-gen version.
