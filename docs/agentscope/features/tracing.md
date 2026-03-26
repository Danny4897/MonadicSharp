# Tracing

This page explains how AgentScope captures and visualizes the execution of MonadicSharp pipelines as distributed traces.

::: warning Work in Progress
This page is under construction. AgentScope is in active development.
:::

## Pipeline Steps as Spans

When this page is complete, it will explain how every step in a MonadicSharp pipeline — each `Bind`, `Map`, `Match`, and `TryAsync` call — is automatically instrumented as an OpenTelemetry span when `MonadicSharp.Framework.Telemetry` is configured. These spans carry the operation name, the success or failure outcome, any error details, and the elapsed time. AgentScope receives these spans via OTLP and groups them into a trace that represents the full lifecycle of a single pipeline execution.

## Timeline Visualization

This section will describe the trace timeline view in the AgentScope dashboard. Each trace is rendered as a horizontal Gantt-style timeline where spans are positioned according to their start time and duration. Nested spans — representing sub-pipelines or delegated operations — are shown as child rows indented under their parent. Color coding distinguishes successful spans from failed ones at a glance, and hovering over a span reveals its full attribute payload.

## Drill-Down on Errors

This section will cover the error drill-down workflow. When a pipeline step fails, AgentScope links the failure span to its full error context: the error type, the error message, the Result error value (if structured), and the stack trace if one was captured. From the timeline, a single click opens the error detail panel, allowing developers to understand exactly which step in a multi-stage AI pipeline failed and why, without needing to grep through logs.

---

[← Back to features](/features/)

[← Back to home](/)
