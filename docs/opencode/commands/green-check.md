# /green-check

Runs a full Green Score analysis on the entire project. Returns a score from 0 to 100 with a per-file breakdown and optional threshold enforcement.

## Difference from /forge-analyze

`/forge-analyze` inspects a single file and lists individual violations. `/green-check` scans every `.cs` file in the project, aggregates the results, and computes a weighted score. Use it before committing or as a quality gate in CI.

## Usage

```
/green-check
/green-check --threshold 85
/green-check --json
```

## Options

| Option | Default | Description |
|---|---|---|
| `--threshold` | value from `.monadicleaf.json` or `0` | Fail if score is below this value |
| `--json` | off | Emit raw JSON instead of the formatted report |
| `--exclude` | value from `.monadicleaf.json` | Glob pattern for paths to skip |

## Human-readable output

```
Green Score: 74/100

Breakdown by module:

  src/Services/          87/100   ██████████████████░░░
  src/Infrastructure/    61/100   ████████████░░░░░░░░░
  src/Controllers/       58/100   ███████████░░░░░░░░░░
  src/Domain/            96/100   ███████████████████░░

Files with most violations:

  src/Infrastructure/EmailSender.cs      GC001(2) GC002(1) GC003(1)
  src/Controllers/OrderController.cs     GC001(1) GC005(2)
  src/Services/ReportService.cs          GC003(3)

Threshold: 80   Status: FAIL (74 < 80)
Run /forge-analyze on the files above or /migrate to start fixing.
```

## JSON output (`--json`)

```json
{
  "score": 74,
  "threshold": 80,
  "passed": false,
  "modules": [
    {
      "path": "src/Services/",
      "score": 87,
      "violations": 4
    },
    {
      "path": "src/Infrastructure/",
      "score": 61,
      "violations": 12
    }
  ],
  "files": [
    {
      "path": "src/Infrastructure/EmailSender.cs",
      "score": 48,
      "violations": [
        { "code": "GC001", "count": 2 },
        { "code": "GC002", "count": 1 },
        { "code": "GC003", "count": 1 }
      ]
    }
  ],
  "summary": {
    "totalFiles": 34,
    "totalViolations": 28,
    "byCode": {
      "GC001": 8,
      "GC002": 3,
      "GC003": 9,
      "GC005": 4,
      "GC006": 4
    }
  }
}
```

## Score calculation

The Green Score is computed per file and then aggregated:

```
file_score = 100 - (error_weight × errors) - (warning_weight × warnings) - (hint_weight × hints)
```

Default weights: errors = 10, warnings = 5, hints = 1. Minimum file score is 0.

The project score is the weighted average of file scores, where each file is weighted by its line count.

## Threshold configuration

Set the minimum acceptable score in `.monadicleaf.json`:

```json
{
  "minGreenScore": 80,
  "failOnSeverity": "error",
  "excludePaths": ["**/Migrations/**", "**/obj/**"]
}
```

`failOnSeverity` triggers a non-zero exit code if any violation of that severity or higher is found, regardless of the numeric score.

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Analysis complete, score at or above threshold |
| `1` | Fatal error (project not found, parse failure) |
| `2` | Score below threshold or `failOnSeverity` triggered |
