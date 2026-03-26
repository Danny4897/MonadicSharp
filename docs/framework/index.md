---
layout: home

hero:
  name: "MonadicSharp.Framework"
  text: "Enterprise AI agent infrastructure for .NET 8"
  tagline: "Failures, security violations, and persistence errors as first-class Result<T> values — never unhandled exceptions."
  actions:
    - theme: brand
      text: Get Started
      link: /framework/getting-started
    - theme: alt
      text: Architecture
      link: /framework/architecture
    - theme: alt
      text: GitHub
      link: https://github.com/Danny4897/MonadicSharp.Framework

features:
  - icon: 🤖
    title: Agents
    details: Core orchestration and pipeline management. Build multi-step AI workflows where every step returns Result<T> — no hidden exceptions, full composability.
    link: /framework/packages/agents
    linkText: Explore Agents

  - icon: 🔐
    title: Security
    details: Prompt injection protection, secret masking, and trust boundary enforcement. Every security violation surfaces as a typed Error — never a thrown exception.
    link: /framework/packages/security
    linkText: Explore Security

  - icon: 📡
    title: Telemetry
    details: OpenTelemetry integration that understands Result<T>. Trace agent pipelines, capture failure paths, and export spans without modifying your business logic.
    link: /framework/packages/telemetry
    linkText: Explore Telemetry

  - icon: 🌐
    title: Http
    details: Typed HTTP client with automatic retry logic and circuit breaker patterns. Network failures become Result<NetworkError> — catch them where they make sense.
    link: /framework/packages/http
    linkText: Explore Http

  - icon: 🗄️
    title: Persistence
    details: Entity Framework Core 8 repository pattern aligned with Railway-Oriented Programming. Database errors are explicit, not surprise exceptions at runtime.
    link: /framework/packages/persistence
    linkText: Explore Persistence

  - icon: ⚡
    title: Caching
    details: Result-aware caching layer. Cache hits and misses compose with your existing pipelines — no boilerplate, no swallowed errors.
    link: /framework/packages/caching
    linkText: Explore Caching
---
