# Ecosystem

MonadicSharp is a family of focused packages. Start with the core, add only what you need.

## Packages

### MonadicSharp _(core)_

The foundation. All other packages depend on it.

```bash
dotnet add package MonadicSharp
```

Includes: `Result<T>`, `Option<T>`, `Either<L,R>`, `Error`, `Try`, `Unit`, `PipelineBuilder`, functional extensions.

---

### MonadicSharp.AI

Typed errors for LLM operations. Retry with exponential backoff. Agent result tracing.

```bash
dotnet add package MonadicSharp.AI
```

Key types: `AgentResult<T>`, `LlmError`, `TokenBudget`, `CachingAgentWrapper`.

---

### MonadicSharp.Recovery

Self-healing pipelines — the **Amber track**. When a step fails, the recovery pipeline attempts to resolve the issue and resume.

```bash
dotnet add package MonadicSharp.Recovery
```

Key types: `AmberPipeline<T>`, `RecoveryStrategy`, `CircuitBreaker`.

---

### MonadicSharp.Agents

Multi-agent orchestration with built-in circuit breakers, capability constraints, and agent context isolation.

```bash
dotnet add package MonadicSharp.Agents
```

Key types: `AgentOrchestrator`, `AgentContext`, `AgentCapability`, `CircuitBreakerAgent`.

---

### MonadicSharp.Http

Result-aware HTTP client. Maps `HttpResponseMessage` to `Result<T>` automatically. Typed retry policies.

```bash
dotnet add package MonadicSharp.Http
```

Key types: `MonadicHttpClient`, `HttpResultExtensions`.

---

### MonadicSharp.Persistence

Result-aware repository and Unit of Work pattern for EF Core 8. Every DB operation returns `Result<T>` — no silent `null` returns.

```bash
dotnet add package MonadicSharp.Persistence
```

Key types: `IMonadicRepository<T>`, `MonadicDbContext`, `UnitOfWork`.

---

### MonadicSharp.Security

Prompt injection detection. Secret masking in agent outputs. Policy enforcement for AI pipelines.

```bash
dotnet add package MonadicSharp.Security
```

Key types: `InjectionDetector`, `SecretMasker`, `SecurityPolicy`.

---

### MonadicSharp.Telemetry

OpenTelemetry tracing for agent pipelines. Automatic span creation for `Bind` chains. Error propagation in traces.

```bash
dotnet add package MonadicSharp.Telemetry
```

Key types: `MonadicActivitySource`, `PipelineTracer`.

---

### MonadicSharp.Framework _(meta-package)_

Installs all packages above in one command.

```bash
dotnet add package MonadicSharp.Framework
```

::: tip Prefer individual packages
Only install what you use. `MonadicSharp.Framework` is convenient for new projects; individual packages keep existing ones lean.
:::

## Dependency graph

```
MonadicSharp (core)
├── MonadicSharp.AI
│   └── MonadicSharp.Recovery
├── MonadicSharp.Agents
├── MonadicSharp.Http
├── MonadicSharp.Persistence
├── MonadicSharp.Security
└── MonadicSharp.Telemetry
```

## Templates

Scaffold a new project with MonadicSharp pre-configured:

```bash
dotnet new install MonadicSharp.Templates
dotnet new monadic-api    # Minimal API + Result<T> + EF Core
dotnet new monadic-clean  # Clean Architecture + CQRS
```

→ [Templates guide](/templates)
