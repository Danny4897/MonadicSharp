# CLI Reference

This page documents the `ml` command-line tool — MonadicLeaf's interface for analyzing, scoring, and migrating your C# projects from the terminal.

::: warning Work in Progress
This page is under construction. MonadicLeaf is in active development.
:::

## Installation

When this page is complete, it will walk through installing MonadicLeaf as a .NET global tool via `dotnet tool install -g MonadicLeaf`. It will cover prerequisites (SDK version, supported platforms), how to update to a new version, and how to verify the installation with `ml --version`.

## Available Commands

This section will provide full reference documentation for every `ml` subcommand:

- **`ml analyze`** — runs the full Roslyn-based analysis on a solution or project file and prints a structured report to stdout.
- **`ml score`** — outputs the Green Score for the current solution, with optional flags to fail with a non-zero exit code below a given threshold.
- **`ml migrate`** — applies auto-fixable rule corrections (GC001, GC002, GC003, GC005, GC009) across the codebase in a single pass, with a dry-run mode to preview changes.
- **`ml watch`** — starts a file-system watcher that re-analyzes changed files in real time, streaming results to the terminal as you code.

## Configuration File

This section will document the `monadic-leaf.json` configuration file format — how to set per-project thresholds, exclude directories, enable or disable individual rules, and override default severity levels.

---

[← Back to home](/)
