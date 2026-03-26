# Why MonadicSharp.Framework?

MonadicSharp.Framework is a meta-package that installs all six infrastructure packages in one step. This page helps you decide whether to use it or install packages individually.

## What the meta-package installs

```bash
dotnet add package MonadicSharp.Framework
```

This single command adds:

- `MonadicSharp.Framework.Agents`
- `MonadicSharp.Framework.Security`
- `MonadicSharp.Framework.Telemetry`
- `MonadicSharp.Framework.Http`
- `MonadicSharp.Framework.Persistence`
- `MonadicSharp.Framework.Caching`

All packages depend only on `MonadicSharp` core. They are independent of each other at the assembly level.

## When the meta-package makes sense

**New projects starting from scratch.** You do not know yet which packages you will use. The meta-package lets you start building without spending time on dependency decisions.

**Proof-of-concept work.** Speed matters more than bundle size. Install everything, explore, then trim later.

**Teams new to Railway-Oriented Programming.** Having all packages available from day one reduces the friction of discovering patterns incrementally. Developers can read any part of the framework documentation without needing a separate install step.

**Internal services where deployment size is not a constraint.** Long-running .NET workers and ASP.NET APIs rarely benefit from removing a few hundred KB of framework assemblies.

## When individual packages are better

**Minimal APIs and single-responsibility services.** A service that only makes outbound HTTP calls and caches results needs `Http` and `Caching`. Adding `Agents`, `Security`, and `Persistence` adds code you will never use.

**Strict dependency auditing.** Some organizations review every transitive dependency. Using individual packages keeps the graph minimal and explicit.

**Incremental adoption in an existing codebase.** Introducing ROP to an existing project works better one layer at a time. Start with `Http` to replace `HttpClient` try/catch blocks, then add `Persistence` when you refactor the data layer.

**Serverless and trimmed deployments.** Native AOT and `dotnet publish --sc` with size trimming benefit from smaller reference sets.

## Comparison

| Scenario | Recommendation |
|---|---|
| New greenfield project | `MonadicSharp.Framework` (meta-package) |
| Proof of concept / prototype | `MonadicSharp.Framework` (meta-package) |
| Team unfamiliar with ROP | `MonadicSharp.Framework` (meta-package) |
| Only need HTTP + Caching | Individual packages |
| Minimal API / trimmed publish | Individual packages |
| Incremental adoption in legacy codebase | Individual packages |
| Strict transitive dependency policy | Individual packages |
| Internal long-running service | Either — no meaningful difference |

## Mixing approaches

You can start with the meta-package and later replace it with explicit individual references without any code changes. The meta-package adds no types of its own; it is purely a dependency bundle.

```xml
<!-- Before: single reference -->
<PackageReference Include="MonadicSharp.Framework" Version="1.*" />

<!-- After: only what you use -->
<PackageReference Include="MonadicSharp.Framework.Agents" Version="1.*" />
<PackageReference Include="MonadicSharp.Framework.Http" Version="1.*" />
<PackageReference Include="MonadicSharp.Framework.Caching" Version="1.*" />
```

Both forms are compatible. No API surface changes.
