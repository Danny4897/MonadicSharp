# Rules Reference

MonadicLeaf enforces 10 green-code rules, grouped by severity.

## Error rules (block CI by default)

| Rule | Name | Description |
|------|------|-------------|
| [GC001](/rules/gc001) | No bare throw | Public methods must not throw — return `Result<T>` instead |
| [GC002](/rules/gc002) | No nullable return | Failable methods must return `Option<T>` instead of `T?` |
| [GC003](/rules/gc003) | No unhandled Result | Every `Result<T>` must be matched, bound, or explicitly discarded |
| [GC004](/rules/gc004) | No bool for failable | Methods that can fail must not return `bool` — use `Result<T>` |
| [GC005](/rules/gc005) | No out parameters | Use `Option<T>` instead of `TryGet`-style out params |
| [GC006](/rules/gc006) | No swallowed catch | Empty catch blocks are forbidden |

## Warning rules

| Rule | Name | Description |
|------|------|-------------|
| GC007 | Prefer Bind over await | Sequential Result chains should use `BindAsync` instead of nested `await` |
| GC008 | Prefer Map over manual Select | Use `Map` instead of manually unwrapping and rewrapping values |
| GC009 | No nested Result | `Result<Result<T>>` is always a modeling error |
| GC010 | Prefer Match at boundaries | Unwrap Results at the outermost layer — not mid-pipeline |

## Auto-fixable rules

Rules marked with 🔧 can be fixed automatically with `leaf migrate`:

🔧 GC001, GC002, GC003, GC005, GC009
