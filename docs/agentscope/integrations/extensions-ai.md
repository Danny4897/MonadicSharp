# Microsoft.Extensions.AI Integration

::: warning Work in Progress
This page is under construction. AgentScope is in active development — integrations are being finalized.
:::

## Overview

AgentScope will provide a middleware for `Microsoft.Extensions.AI` that automatically captures all AI calls made through the `IChatClient` and `IEmbeddingGenerator` abstractions.

Once available, this integration will give you zero-configuration observability for any AI provider registered via `Microsoft.Extensions.AI` — including Azure OpenAI, OpenAI, Ollama, and any other provider that implements the standard interfaces.

## What this will include

**Automatic instrumentation** — every `IChatClient.CompleteAsync` and `IEmbeddingGenerator.GenerateAsync` call will be captured as a span in AgentScope without any code changes to your application.

**Token tracking** — input and output tokens will be extracted from `UsageDetails` and displayed in the AgentScope dashboard with cost estimates per model.

**Prompt & response capture** — optionally capture the full prompt and response text for debugging (configurable per environment to avoid logging sensitive data in production).

**Pipeline visualization** — when used alongside MonadicSharp pipelines, AgentScope will correlate the AI call with the surrounding `Result<T>` steps to show the full execution path.

## Planned configuration

```csharp
// Planned API — subject to change
builder.Services.AddChatClient(inner =>
    inner.Use(new AgentScopeMiddleware(agentScopeEndpoint)));
```

→ [Back to AgentScope home](/)
