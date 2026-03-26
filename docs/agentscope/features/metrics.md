# Metrics Dashboard

This page documents the AgentScope metrics dashboard — the real-time KPI overview for AI pipeline health and performance.

::: warning Work in Progress
This page is under construction. AgentScope is in active development.
:::

## Key Performance Indicators

When this page is complete, it will describe the top-level KPIs displayed on the AgentScope metrics dashboard. These include: total pipeline invocations over a configurable time window, the overall success rate and error rate as percentages, total tokens consumed across all LLM calls, and a summary of circuit breaker states. Each KPI is accompanied by a trend indicator showing whether the metric has improved or degraded compared to the previous equivalent time window.

## Latency Percentiles

This section will cover the latency charts in detail. AgentScope computes and displays p50, p95, and p99 latency for both individual pipeline steps and end-to-end traces. Understanding the difference between median and tail latency is critical for AI applications where a small percentage of slow LLM calls can dominate the user-perceived experience. The page will explain how to read the charts, how to filter by pipeline name or step type, and how to export the underlying data.

## Token Usage and Cost Estimation

This section will describe the token tracking capabilities. AgentScope aggregates token counts reported by LLM integrations (Semantic Kernel, `Microsoft.Extensions.AI`) and breaks them down by model, by pipeline, and over time. When unit costs are configured, AgentScope can display estimated spend alongside raw token counts, helping teams identify which pipelines are the most expensive and where optimizations — such as prompt compression or caching — would have the greatest impact.

---

[← Back to features](/features/)

[← Back to home](/)
