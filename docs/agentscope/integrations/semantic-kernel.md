# Semantic Kernel Integration

This page documents the AgentScope plugin for Microsoft Semantic Kernel, which enables full trace visibility into SK kernel invocations.

::: warning Work in Progress
This page is under construction. AgentScope is in active development.
:::

## Overview

When this page is complete, it will explain how AgentScope integrates with Microsoft Semantic Kernel through the standard SK telemetry hooks. Semantic Kernel already emits OpenTelemetry spans for kernel function invocations, prompt rendering, and connector calls — AgentScope enriches these spans with its own domain model, correlates them with MonadicSharp pipeline spans when both are present in the same trace, and surfaces them in the AgentScope dashboard alongside your application's own pipeline data.

## What Gets Traced

This section will document exactly which Semantic Kernel activities appear as spans in AgentScope: kernel function invocations (both native functions and semantic functions), planner steps, memory reads and writes, connector calls to LLM providers, and prompt template rendering events. For each span type, the page will describe the attributes captured — such as the function name, the model used, the prompt character count, the token counts reported by the provider, and the outcome.

## Correlating SK Spans with MonadicSharp Pipelines

This section will describe the correlation story: when a MonadicSharp pipeline calls into a Semantic Kernel function (for example, via a `BindAsync` step that invokes an SK kernel), AgentScope links the SK spans as children of the MonadicSharp pipeline span. The result is a single unified trace tree that shows the full execution path — from the incoming request, through the MonadicSharp pipeline steps, into the SK kernel invocations, and down to the individual LLM API calls.

---

[← Back to integrations](/integrations/)

[← Back to home](/)
