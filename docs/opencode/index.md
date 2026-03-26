---
layout: home

hero:
  name: "MonadicSharp × OpenCode"
  text: "Green code inside your AI editor"
  tagline: "The structural guarantee that AI-generated C# code doesn't break in production — integrated directly into OpenCode."
  actions:
    - theme: brand
      text: Get Started
      link: /opencode/getting-started
    - theme: alt
      text: Commands
      link: /opencode/commands/forge-analyze
    - theme: alt
      text: GitHub
      link: https://github.com/Danny4897/MonadicSharp-OpenCode

features:
  - icon: 🔬
    title: /forge-analyze
    details: Analyze the current file or selection for green-code violations. Returns a structured list of issues with suggested fixes — directly inside OpenCode.
    link: /opencode/commands/forge-analyze
    linkText: Command docs

  - icon: ✅
    title: /green-check
    details: Run a full Green Score analysis on your project and display the result as an inline report. Gate your work to a minimum score before committing.
    link: /opencode/commands/green-check
    linkText: Command docs

  - icon: 🔧
    title: /migrate
    details: Auto-migrate violations in the current file. Bare throws become Result<T>, nullable returns become Option<T> — all within your OpenCode session.
    link: /opencode/commands/migrate
    linkText: Command docs

  - icon: 🤖
    title: AI-aware patterns
    details: OpenCode generates C# — MonadicSharp ensures it's correct. Every suggestion is validated against Railway-Oriented Programming rules before it reaches your codebase.

  - icon: 🟢
    title: Green Code by default
    details: Configure OpenCode to always prefer MonadicSharp idioms. No more try/catch, no nullables, no hidden exceptions in AI-generated code.
    link: /opencode/green-code
    linkText: What is Green Code?

  - icon: 🔗
    title: Ecosystem integration
    details: Works with MonadicLeaf CLI for CI enforcement, MonadicSharp.AI for LLM pipelines, and the full MonadicSharp.Framework for enterprise projects.
---
