# MonadicSharp.Framework.Telemetry

This page explains how to configure `MonadicSharp.Framework.Telemetry` to send pipeline observability data to AgentScope.

::: warning Work in Progress
This page is under construction. AgentScope is in active development.
:::

## Package Overview

When this page is complete, it will introduce `MonadicSharp.Framework.Telemetry` — the optional NuGet package that adds automatic OpenTelemetry instrumentation to MonadicSharp pipelines. Installing it requires no changes to existing pipeline code: it hooks into the MonadicSharp execution engine via a source-generated interceptor and emits spans for every pipeline operation automatically.

## Configuration

This section will walk through the setup steps in detail. After installing the package, the developer registers AgentScope as the OTLP exporter in the application's `Program.cs` using the standard OpenTelemetry .NET SDK `AddOtlpExporter` extension. It will cover the AgentScope endpoint URL format, how to pass an API key for authenticated deployments, how to configure the export interval for metrics, and how to enable or disable specific signal types (traces only, metrics only, or both).

## What Gets Instrumented

This section will provide a complete list of the MonadicSharp operations that `MonadicSharp.Framework.Telemetry` instruments automatically: `Result<T>` creation, `Bind`, `BindAsync`, `Map`, `MapAsync`, `Match`, `TryAsync`, and the AI-specific extensions from `MonadicSharp.AI`. For each operation, it will document which span attributes are recorded — such as the operation name, the success/failure outcome, the error type if applicable, and the pipeline depth — and how those attributes map to AgentScope's UI.

---

[← Back to integrations](/integrations/)

[← Back to home](/)
