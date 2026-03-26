# /forge-analyze

Analyzes the file currently open in the OpenCode session for green-code violations. Results are returned as a structured table sorted by severity.

## What it does

1. Sends the current file path to MonadicLeaf via `monadicleaf analyze`.
2. MonadicLeaf parses the file with Roslyn and applies rules GC001–GC010.
3. Returns a list of violations with their code, severity, line number, and description.
4. OpenCode renders the results inline in the chat.

## Usage

```
/forge-analyze
/forge-analyze --severity error
/forge-analyze --file src/Services/OrderService.cs
/forge-analyze --json
```

## Options

| Option | Default | Description |
|---|---|---|
| `--severity` | `hint` | Minimum severity to include (`hint`, `warning`, `error`) |
| `--file` | current file | Override the file to analyze |
| `--json` | off | Emit raw JSON instead of the rendered table |

## Output: file with violations

```
File: src/Services/OrderService.cs

┌────────┬──────────┬──────┬──────────────────────────────────────────────────────┐
│ Code   │ Severity │ Line │ Description                                          │
├────────┼──────────┼──────┼──────────────────────────────────────────────────────┤
│ GC001  │ error    │  42  │ Bare throw detected. Use Result<T> to propagate.     │
│ GC003  │ warning  │  67  │ Null return on failure path. Use Option<T>.          │
│ GC007  │ hint     │  91  │ Boolean return for failable operation. Use Result.   │
└────────┴──────────┴──────┴──────────────────────────────────────────────────────┘

3 violations found (1 error, 1 warning, 1 hint)
Run /migrate to auto-fix GC001 and GC003.
```

## Output: clean file

```
File: src/Services/UserService.cs

No violations found.
Green Score for this file: 100/100
```

## JSON output (`--json`)

```json
{
  "file": "src/Services/OrderService.cs",
  "violations": [
    {
      "code": "GC001",
      "severity": "error",
      "line": 42,
      "column": 13,
      "description": "Bare throw detected. Use Result<T> to propagate.",
      "ruleUrl": "https://danny4897.github.io/MonadicLeaf/rules/GC001"
    },
    {
      "code": "GC003",
      "severity": "warning",
      "line": 67,
      "column": 8,
      "description": "Null return on failure path. Use Option<T>.",
      "ruleUrl": "https://danny4897.github.io/MonadicLeaf/rules/GC003"
    }
  ],
  "summary": {
    "total": 2,
    "errors": 1,
    "warnings": 1,
    "hints": 0
  }
}
```

## Violation codes

| Code | Severity | Rule |
|---|---|---|
| GC001 | error | No bare `throw` |
| GC002 | error | No `catch` without re-wrap |
| GC003 | warning | No `null` return on failure path |
| GC004 | warning | No `bool` return for failable operations |
| GC005 | warning | No swallowed exceptions (`catch {}`) |
| GC006 | hint | Prefer `BindAsync` over manual `if (result.IsSuccess)` |
| GC007 | hint | Boolean return for failable operation — use `Result<T>` |
| GC008 | hint | Use `Option<T>` instead of nullable reference type |
| GC009 | hint | Async method returns `Task<T?>` — use `Task<Option<T>>` |
| GC010 | hint | Missing `CancellationToken` on async method |

## Related

- [/migrate](./migrate) — auto-fix violations in the current file
- [/green-check](./green-check) — run a full project score
- [Green Code philosophy](../green-code) — understand why these rules exist
