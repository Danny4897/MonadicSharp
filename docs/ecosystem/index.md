# Ecosystem

MonadicSharp is a family of focused packages. Start with the core, add only what you need.

## Packages

### [MonadicSharp](https://danny4897.github.io/MonadicSharp/) _(core)_

The foundation. All other packages depend on it.

```bash
dotnet add package MonadicSharp
```

Includes: `Result<T>`, `Option<T>`, `Either<L,R>`, `Error`, `Try`, `Unit`, `PipelineBuilder`, functional extensions.

---

### [MonadicSharp.Framework](https://danny4897.github.io/MonadicSharp.Framework/) _(meta-package)_

Installs all packages above in one command. Enterprise-grade AI agent infrastructure for .NET.

```bash
dotnet add package MonadicSharp.Framework
```

::: tip Prefer individual packages
Only install what you use. `MonadicSharp.Framework` is convenient for new projects; individual packages keep existing ones lean.
:::

---

### [MonadicSharp.AI](https://danny4897.github.io/MonadicSharp.AI/)

Typed errors for LLM operations. Retry with exponential backoff. Agent result tracing.

```bash
dotnet add package MonadicSharp.AI
```

Key types: `AgentResult<T>`, `AiError`, `RetryResult<T>`, `StreamResult`.

---

### [MonadicSharp.Recovery](https://danny4897.github.io/MonadicSharp.Recovery/)

Self-healing pipelines — Railway-Oriented error recovery with `RescueAsync` and `StartFixBranchAsync`.

```bash
dotnet add package MonadicSharp.Recovery
```

Key types: `RescueAsync`, `StartFixBranchAsync`, `IRecoveryTelemetry`.

---

### [MonadicSharp.Azure](https://danny4897.github.io/MonadicSharp.Azure/)

Railway-Oriented Programming for the Azure ecosystem — every SDK call wrapped in `Result<T>`.

```bash
dotnet add package MonadicSharp.Azure
```

Covers: Cosmos DB, Service Bus, Blob Storage, Key Vault, Azure OpenAI, Azure Functions.

---

### [MonadicSharp.DI](https://danny4897.github.io/MonadicSharp.DI/)

Lightweight functional mediator for .NET — CQRS aligned with MonadicSharp primitives.

```bash
dotnet add package MonadicSharp.DI
```

Key types: `IQueryHandler<Q,T>`, `ICommandHandler<C,T>`, `IPipelineBehavior<R,T>`, `INotification`.

---

## Tooling

### [MonadicLeaf](https://danny4897.github.io/MonadicLeaf/)

Static analysis and Green Score for MonadicSharp codebases. Ensures AI-generated C# code doesn't break in production.

```bash
dotnet add package MonadicLeaf.Analyzers
```

Includes: Roslyn analyzers (GC001–GC010), CLI tool, CI integration.

---

### [MonadicSharp × OpenCode](https://danny4897.github.io/MonadicSharp-OpenCode/)

Structural guarantee that AI-generated C# code does not break in production — integrated directly into OpenCode.

Commands: `/forge-analyze`, `/green-check`, `/migrate`.

---

### [AgentScope](https://danny4897.github.io/AgentScope/)

AI agent observability platform for .NET — see every agent, trace every pipeline, catch every failure.

Key features: Pipeline Tracing, Metrics Dashboard, Circuit Breakers, Alerts.

---

## Klexir Ecosystem

Not MonadicSharp packages — a separate research project *built on* MonadicSharp's `Result<T>`, exploring systems-programming concepts (event buses, actors, workflow engines, storage engines, bytecode VMs, and a real programming language) in .NET. Public, not yet on NuGet.

### [Klexir.Lang](https://danny4897.github.io/Klexir.Lang/)

A real, runnable language — records, unions, an `Option`/`Result` railway, lists — with a CLI and VS Code syntax highlighting. `KlexirInterop` bridges its values straight into real `MonadicSharp.Option<T>`/`Result<T>`.

```bash
dotnet run --project src/Klexir.Cli -- run file.klx
```

---

### [Klexir.EventFlow](https://danny4897.github.io/Klexir.EventFlow/)

Strongly typed, in-process event bus — resilience, idempotency, dead-lettering and tracing built in.

Key types: `InMemoryEventBus`, `IEventHandler<T>`, `EventBusResilienceOptions`.

---

### [Klexir.Actor](https://danny4897.github.io/Klexir.Actor/)

`Channel<T>`-backed actor primitives — serialized state transitions without locks, tell/ask messaging, supervision.

Key types: `Actor<TMessage, TState>`, `InMemoryActorRef`, `SupervisionOptions`.

---

### [Klexir.Workflow](https://danny4897.github.io/Klexir.Workflow/)

Durable workflow and saga orchestration — a fluent step builder, checkpointing, crash resumption.

Key types: `Workflow.Define`, `WorkflowEngine`, `FileWorkflowStore`.

---

### [Klexir.Engine](https://danny4897.github.io/Klexir.Engine/)

Storage engine internals from the file up — pages, a buffer pool, a B-Tree, a write-ahead log, 2PL transactions.

Key types: `FilePageStore`, `BufferPool`, `PagedBTree`, `WalRecovery`.

---

### [Klexir.Runtime](https://danny4897.github.io/Klexir.Runtime/)

A small stack-based bytecode VM — the interpretation loop, a call stack, a real garbage collector, made explicit.

Key types: `BytecodeBuilder`, `KlexirVm`.

---

## Dependency graph

```
MonadicSharp (core)
├── MonadicSharp.Framework (meta-package)
├── MonadicSharp.AI
│   └── MonadicSharp.Recovery
├── MonadicSharp.Azure
├── MonadicSharp.DI
└── Tooling
    ├── MonadicLeaf (analyzers)
    ├── MonadicSharp-OpenCode (AI coding)
    └── AgentScope (observability)

Klexir Ecosystem (separate project, built on MonadicSharp)
├── Klexir.Lang       (language — Option/Result railway inside the language itself)
├── Klexir.EventFlow  (event bus)
├── Klexir.Actor      (actor model)
├── Klexir.Workflow   (saga orchestration)
├── Klexir.Engine     (storage engine)
└── Klexir.Runtime    (bytecode VM)
```

## Templates

Scaffold a new project with MonadicSharp pre-configured:

```bash
dotnet new install MonadicSharp.Templates
dotnet new monadic-api    # Minimal API + Result<T> + EF Core
dotnet new monadic-clean  # Clean Architecture + CQRS
```

→ [Templates guide](/templates)
