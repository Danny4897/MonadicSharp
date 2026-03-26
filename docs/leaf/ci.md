# CI/CD Integration

This page explains how to integrate MonadicLeaf into your continuous integration pipeline to enforce Green Score thresholds and surface violations directly in pull requests.

::: warning Work in Progress
This page is under construction. MonadicLeaf is in active development.
:::

## GitHub Actions Workflow

When this page is complete, it will provide a ready-to-use GitHub Actions workflow definition that installs the `ml` tool, runs `ml score` against your solution, and fails the build if the score falls below a configurable threshold. The workflow will be designed to run on pull requests and push events, with caching for the .NET tool installation to keep run times short.

## Fail-Build on Score Threshold

This section will document how to configure the minimum acceptable Green Score for CI purposes — separate from the local development threshold if needed. It will explain how `ml score --min 80` returns exit code 1 when the score is below 80, how to map this to a required status check on GitHub branch protection rules, and how to handle score regressions introduced by a specific PR.

## PR Comment Reports

This section will describe how MonadicLeaf can post a detailed score breakdown as a comment on pull requests. When a PR introduces new violations, the comment will list each affected file, the rule triggered, and a suggested fix — giving reviewers full context without leaving GitHub. It will cover the GitHub token permissions required and how to configure the comment format.

---

[← Back to home](/)
