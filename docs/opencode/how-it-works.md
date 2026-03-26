# How It Works

MonadicSharp for OpenCode is a thin bridge between OpenCode's slash command system and the MonadicLeaf CLI tool. This page describes the full execution flow from the moment you type a command to the moment you see output.

## System requirements

- [OpenCode](https://opencode.ai) — AI coding assistant
- [MonadicLeaf](https://danny4897.github.io/MonadicLeaf/) — .NET global tool (`dotnet tool install -g MonadicLeaf`)
- .NET 8.0 SDK or runtime
- A `.monadicleaf.json` at project root (optional, controls thresholds and rule sets)

## Architecture overview

```
OpenCode session
      │
      │  user types /forge-analyze, /green-check, or /migrate
      ▼
opencode.json (command definitions)
      │
      │  resolves to a MonadicLeaf CLI invocation
      ▼
MonadicLeaf CLI (dotnet global tool)
      │
      │  analyzes C# source using Roslyn
      │  applies green-code ruleset (GC001–GC010)
      ▼
stdout: structured JSON output
      │
      │  OpenCode reads stdout
      ▼
OpenCode inline response
      │  (table / score / diff rendered in chat)
```

## Step-by-step flow

### 1. Command entry

You type `/forge-analyze` inside an OpenCode session. OpenCode matches the slash command against the definitions in `opencode.json`.

### 2. Command resolution

`opencode.json` contains the command manifest. Each entry maps a slash command to a CLI invocation:

```json
{
  "commands": [
    {
      "name": "forge-analyze",
      "description": "Analyze current file for green-code violations",
      "run": "monadicleaf analyze --file {{file}} --format json"
    },
    {
      "name": "green-check",
      "description": "Run full Green Score on the project",
      "run": "monadicleaf score --project {{project}} --format json"
    },
    {
      "name": "migrate",
      "description": "Auto-migrate violations in current file",
      "run": "monadicleaf migrate --file {{file}} --dry-run"
    }
  ]
}
```

OpenCode interpolates `{{file}}` with the absolute path of the file currently open in the editor session. `{{project}}` resolves to the nearest `.csproj` or solution root.

### 3. MonadicLeaf execution

OpenCode invokes the MonadicLeaf CLI as a child process and captures stdout. MonadicLeaf uses Roslyn to parse the target C# source, applies the configured ruleset, and writes a JSON result to stdout.

MonadicLeaf exits with:

- `0` — analysis complete (violations may still be present)
- `1` — fatal error (file not found, parse failure, etc.)
- `2` — threshold breach (Green Score below `minGreenScore` in `.monadicleaf.json`)

### 4. Output rendering

OpenCode reads the JSON from stdout and renders it inline in the chat as a formatted table, score card, or diff depending on the command. No intermediate files are written; the entire flow is in-process.

## `opencode.json` schema

```json
{
  "commands": [
    {
      "name": "string",
      "description": "string",
      "run": "string — shell command with {{file}} / {{project}} tokens",
      "outputFormat": "json | text"
    }
  ]
}
```

`outputFormat` defaults to `json`. Set it to `text` if you add custom commands that produce plain output.

## Environment variables

MonadicLeaf reads the following variables if set:

| Variable | Purpose |
|---|---|
| `MONADICLEAF_CONFIG` | Override path to `.monadicleaf.json` |
| `MONADICLEAF_SEVERITY` | Minimum severity to report (`hint`, `warning`, `error`) |
| `NO_COLOR` | Disable ANSI color codes in text output |
