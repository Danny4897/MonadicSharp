---
layout: home

hero:
  name: "MonadicLeaf"
  text: "Structural guarantee for AI-generated C#"
  tagline: "Static analysis + auto-migration + Green Score. Catch the patterns that LLMs produce but never ship to production."
  actions:
    - theme: brand
      text: Get Started
      link: /leaf/getting-started
    - theme: alt
      text: Rules Reference
      link: /leaf/rules/
    - theme: alt
      text: GitHub
      link: https://github.com/Danny4897/MonadicLeaf

features:
  - icon: 🔍
    title: Static Analysis
    details: 10 green-code rules (GC001–GC010) catch bare throws, nullable returns, unhandled Results, and more. Zero false positives — every violation is a real structural problem.
    link: /leaf/rules/
    linkText: View all rules

  - icon: 🔧
    title: Auto-Migration
    details: Run `leaf migrate` and MonadicLeaf rewrites violations automatically — bare throws become Result<T>, nullable returns become Option<T>. Safe, incremental, reversible.
    link: /leaf/cli
    linkText: CLI reference

  - icon: 🟢
    title: Green Score
    details: A 0–100 metric measuring how much of your codebase follows Railway-Oriented Programming patterns. Track it in CI — catch regressions before they reach main.
    link: /leaf/green-score
    linkText: Green Score docs

  - icon: 📊
    title: HTML Reports
    details: Generate a full HTML report per analysis run. Violations grouped by rule, severity, and file — shareable without external tooling.

  - icon: 🤖
    title: Built for AI code
    details: LLMs love try/catch and nullable returns. MonadicLeaf is the lint layer that ensures AI-generated code meets your MonadicSharp standards automatically.

  - icon: ⚙️
    title: CI Ready
    details: Exit code 1 on violations above threshold. Configure severity levels, excluded rules, and Green Score minimums in `.monadicleaf.json`.
    link: /leaf/ci
    linkText: CI integration
---
