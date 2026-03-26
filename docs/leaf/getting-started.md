# Getting Started

MonadicLeaf is a CLI tool that analyzes C# codebases for violations of [MonadicSharp](https://danny4897.github.io/MonadicSharp/) green-code patterns, then migrates them automatically.

## Install

```bash
dotnet tool install -g MonadicLeaf
```

**Requires**: .NET 8.0+

## Analyze a project

```bash
# Analyze current directory
leaf analyze

# Analyze a specific path
leaf analyze ./src

# Output as HTML report
leaf analyze --report report.html
```

Example output:

```
MonadicLeaf — Green Code Analysis
──────────────────────────────────
✖ GC001  Services/OrderService.cs:42  Bare throw in public method
✖ GC002  Models/UserResult.cs:18      Nullable return type on failable method
✖ GC003  Controllers/ApiController.cs:67  Result<T> returned but not handled
⚠ GC005  Utils/Parser.cs:11          Out parameter used instead of Option<T>

Green Score: 74/100
4 violations found (3 errors, 1 warning)
```

## Auto-migrate violations

```bash
# Preview what would change (dry run)
leaf migrate --dry-run

# Apply migrations
leaf migrate

# Migrate only specific rules
leaf migrate --rules GC001,GC002
```

## Configure

Create `.monadicleaf.json` in your project root:

```json
{
  "minGreenScore": 80,
  "failOnSeverity": "error",
  "excludeRules": [],
  "excludePaths": ["**/Migrations/**", "**/Generated/**"],
  "reportFormat": "html"
}
```

## CI Integration

```yaml
# .github/workflows/green-check.yml
- name: Run MonadicLeaf
  run: leaf analyze --fail-below 80
```

The tool exits with code `1` if the Green Score falls below the configured minimum, blocking the merge.

## Next steps

- [Rules reference](./rules/) — understand each GC001–GC010 rule
- [Green Score](./green-score) — how the 0–100 score is calculated
- [CI Integration](./ci) — full CI/CD setup guide
