# Green Score

This page covers the Green Score — MonadicLeaf's 0–100 metric for measuring how well a codebase adheres to functional, exception-free coding patterns.

::: warning Work in Progress
This page is under construction. MonadicLeaf is in active development.
:::

## How the Score Is Calculated

When this page is complete, it will explain the full scoring formula. The Green Score is derived from the ratio of clean code paths to total analyzed paths, weighted by rule severity. Error-level violations (such as bare throws or unhandled `Result<T>` values) carry more weight than warnings. The final number is scaled to the 0–100 range, where 100 means zero violations detected across the entire solution.

## Per-File Breakdown

This section will document the per-file score report that MonadicLeaf generates alongside the project-level score. Each file receives its own sub-score, along with a list of the specific violations that reduced it. This breakdown helps teams identify hotspots — files with a disproportionate number of issues — so they can prioritize refactoring efforts effectively.

## Configurable Thresholds and Badges

This section will describe how to configure minimum acceptable scores for different contexts (local development vs. CI), how to suppress specific rules or directories, and how to generate a dynamic Green Score badge for your repository's README. The badge updates automatically on each CI run, giving contributors an at-a-glance view of the project's functional health.

---

[← Back to home](/)
