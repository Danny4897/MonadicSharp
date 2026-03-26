---
layout: home

hero:
  name: "MonadicSharp"
  text: "Railway-Oriented Programming"
  tagline: "Replace exception-driven control flow with composable, explicit error handling — zero dependencies, pure .NET."
  image:
    src: /logo.svg
    alt: MonadicSharp
  actions:
    - theme: brand
      text: Get Started →
      link: /getting-started
    - theme: alt
      text: Core Types
      link: /core/result
    - theme: alt
      text: GitHub
      link: https://github.com/Danny4897/MonadicSharp

features:
  - icon: 🛤️
    title: Result<T>
    details: Success or failure, never both. Chain operations with Bind, transform values with Map, unwrap at the boundary with Match. Errors propagate automatically — no boilerplate.
    link: /core/result
    linkText: Learn more
  - icon: ❓
    title: Option<T>
    details: Value or nothing — NullReferenceException gone from the type system. Explicit absence encoded in the type. Map, Bind, GetValueOrDefault.
    link: /core/option
    linkText: Learn more
  - icon: 🔴
    title: Structured Errors
    details: Semantic, HTTP-mappable errors. Error.NotFound → 404. Error.Validation → 400. Compose sub-errors, attach metadata, chain inner errors.
    link: /core/error
    linkText: Learn more
  - icon: ⚡
    title: Async-first Pipelines
    details: Full Task<Result<T>> support. Bind and Map work transparently with async operations. PipelineBuilder for fluent, readable chains.
    link: /pipelines
    linkText: Learn more
  - icon: 📦
    title: Zero Dependencies
    details: One package, no transitive dependency hell. No Reactive, no LanguageExt. Adopt incrementally — one method at a time, in any existing codebase.
  - icon: 🤖
    title: AI Code Assistants
    details: Ship .cursorrules and copilot-instructions.md. Copilot, Cursor, and Claude generate MonadicSharp-first code automatically from day one.
    link: /templates
    linkText: Setup guide
---

<div class="vp-doc" style="max-width:960px;margin:0 auto;padding:4rem 1.5rem">

<div class="nuget-badges">
  <a href="https://www.nuget.org/packages/MonadicSharp/" target="_blank"><img src="https://img.shields.io/nuget/v/MonadicSharp.svg" alt="NuGet Version" /></a>
  <a href="https://www.nuget.org/packages/MonadicSharp/" target="_blank"><img src="https://img.shields.io/nuget/dt/MonadicSharp.svg" alt="NuGet Downloads" /></a>
  <a href="https://github.com/Danny4897/MonadicSharp/actions/workflows/ci.yml" target="_blank"><img src="https://github.com/Danny4897/MonadicSharp/actions/workflows/ci.yml/badge.svg" alt="CI" /></a>
  <img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="MIT License" />
  <img src="https://img.shields.io/badge/.NET-8.0-purple.svg" alt=".NET 8" />
</div>

## Before vs After

The same operation — one with exceptions, one with Railway-Oriented Programming:

<div class="home-comparison">
<div>

```csharp
// Exception-driven: contract invisible to callers
public User CreateUser(string name, string email)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new ValidationException("Name required");

    if (!email.Contains('@'))
        throw new ValidationException("Invalid email");

    var existing = _db.Users
        .FirstOrDefault(u => u.Email == email);
    if (existing != null)
        throw new ConflictException("Email taken");

    return _db.Save(new User(name, email));
}
// Every caller needs try/catch.
// Errors are invisible in the signature.
```

</div>
<div>

```csharp
// Railway-Oriented: contract explicit in the return type
public Result<User> CreateUser(string name, string email) =>
    ValidateName(name)
        .Bind(_ => ValidateEmail(email))
        .Bind(_ => CheckEmailNotTaken(email))
        .Map(_ => new User(name, email))
        .Bind(user => _db.Users.AddAsync(user));

// The signature tells the truth.
// Errors propagate automatically — no try/catch.
// Compose at the controller with Match:
return result.Match(
    onSuccess: user  => Ok(user),
    onFailure: error => error.Type switch {
        ErrorType.Validation => BadRequest(error.Message),
        ErrorType.Conflict   => Conflict(error.Message),
        _                    => Problem(error.Message)
    });
```

</div>
</div>

## Ecosystem

MonadicSharp is a family of focused packages — use only what you need.

<div class="ecosystem-grid">

<a class="eco-card" href="/MonadicSharp/framework/">
  <div class="eco-card-header" style="background:#4f46e5">Framework</div>
  <div class="eco-card-body">
    <strong>MonadicSharp.Framework</strong>
    <p>Enterprise AI agent infrastructure — Agents, Security, Telemetry, Http, Persistence, Caching in one meta-package.</p>
    <span class="eco-tag">meta-package</span>
  </div>
</a>

<a class="eco-card" href="/MonadicSharp/ai/">
  <div class="eco-card-header" style="background:#2563eb">AI</div>
  <div class="eco-card-body">
    <strong>MonadicSharp.AI</strong>
    <p>Typed LLM errors, retry with backoff, agent pipeline tracing, structured output validation, streaming.</p>
    <span class="eco-tag">nuget</span>
  </div>
</a>

<a class="eco-card" href="/MonadicSharp/recovery/">
  <div class="eco-card-header" style="background:#d97706">Recovery</div>
  <div class="eco-card-body">
    <strong>MonadicSharp.Recovery</strong>
    <p>Self-healing pipelines. RescueAsync and StartFixBranchAsync bring failed operations back to the green track.</p>
    <span class="eco-tag">nuget</span>
  </div>
</a>

<a class="eco-card" href="/MonadicSharp/azure/">
  <div class="eco-card-header" style="background:#0078d4">Azure</div>
  <div class="eco-card-body">
    <strong>MonadicSharp.Azure</strong>
    <p>Railway-Oriented wrappers for CosmosDB, Service Bus, Blob, Key Vault, Azure OpenAI — every call returns Result&lt;T&gt;.</p>
    <span class="eco-tag">nuget</span>
  </div>
</a>

<a class="eco-card" href="/MonadicSharp/di/">
  <div class="eco-card-header" style="background:#0d9488">DI</div>
  <div class="eco-card-body">
    <strong>MonadicSharp.DI</strong>
    <p>Lightweight functional mediator. CQRS aligned with Result&lt;T&gt; — handlers never throw, behaviors compose cleanly.</p>
    <span class="eco-tag">nuget</span>
  </div>
</a>

<a class="eco-card" href="/MonadicSharp/leaf/">
  <div class="eco-card-header" style="background:#16a34a">Leaf</div>
  <div class="eco-card-body">
    <strong>MonadicLeaf</strong>
    <p>Roslyn analyzers that enforce green-code rules. Green Score 0–100 per project. Auto-migration for common violations.</p>
    <span class="eco-tag wip">wip</span>
  </div>
</a>

<a class="eco-card" href="/MonadicSharp/opencode/">
  <div class="eco-card-header" style="background:#0369a1">OpenCode</div>
  <div class="eco-card-body">
    <strong>MonadicSharp × OpenCode</strong>
    <p>Green-code analysis and auto-migration inside your OpenCode AI coding session. /forge-analyze, /green-check, /migrate.</p>
    <span class="eco-tag">tooling</span>
  </div>
</a>

<a class="eco-card" href="/MonadicSharp/agentscope/">
  <div class="eco-card-header" style="background:#ea580c">AgentScope</div>
  <div class="eco-card-body">
    <strong>AgentScope</strong>
    <p>AI agent observability platform. Pipeline tracing, metrics dashboard, circuit breakers, alerts — native .NET.</p>
    <span class="eco-tag wip">wip</span>
  </div>
</a>

</div>

→ [Ecosystem](/ecosystem/) · [Templates](/templates) · [Try it ▶](/try-it)

</div>
