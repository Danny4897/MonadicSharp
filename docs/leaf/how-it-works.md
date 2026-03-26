# How It Works

This page explains the internal pipeline MonadicLeaf uses to analyze your C# codebase, compute a Green Score, and produce actionable reports.

::: warning Work in Progress
This page is under construction. MonadicLeaf is in active development.
:::

## Static Analysis with Roslyn

When this page is complete, it will describe how MonadicLeaf integrates with the .NET Compiler Platform (Roslyn) to perform deep static analysis of your C# source code. Rather than relying on text-pattern matching, MonadicLeaf works directly on the semantic model — meaning it understands types, method signatures, return values, and control flow across your entire solution. This allows it to detect issues that simple linters cannot catch, such as a `Result<T>` value that is returned from a helper but never consumed by the caller.

## Score Computation

This section will explain how MonadicLeaf aggregates individual rule violations across all analyzed files into a single Green Score. The scoring algorithm weights violations by severity (error vs. warning), normalizes against the total number of analyzed code paths, and applies configurable thresholds to produce a 0–100 score. You will find a detailed breakdown of the formula, how partial credit works, and how to influence the score through configuration.

## Report Generation

This section will cover the reporting pipeline: how MonadicLeaf serializes analysis results into structured formats (JSON, SARIF, HTML), how it generates per-file breakdowns and project-level summaries, and how those outputs are consumed by the CLI, the CI integration, and the badge generator. It will also explain how incremental analysis works, allowing subsequent runs on large codebases to skip unchanged files.

---

[← Back to home](/)
