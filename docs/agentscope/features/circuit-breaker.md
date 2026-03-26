# Circuit Breaker Visualization

This page covers AgentScope's real-time view of circuit breaker states across all monitored pipelines.

::: warning Work in Progress
This page is under construction. AgentScope is in active development.
:::

## Real-Time State View

When this page is complete, it will describe the circuit breaker panel in the AgentScope dashboard. Every named circuit breaker registered in the application is shown as a status card that updates in real time via server-sent events. Each card displays the current state (Closed, Open, or Half-Open), the current failure count against the configured threshold, the time elapsed since the last state transition, and the next scheduled probe attempt when the breaker is Open.

## Threshold Configuration

This section will explain how circuit breaker thresholds are configured and how AgentScope reads those settings to provide context alongside the live state. It will cover the relationship between the configuration in your application code and the display in AgentScope, and how to use AgentScope's interface to view the effective thresholds without reading source code or environment variables directly.

## History and Event Log

This section will document the circuit breaker event log — a chronological record of every state transition (Closed to Open, Open to Half-Open, Half-Open back to Closed or Open). Each event entry records the timestamp, the reason for the transition (which specific operation triggered the failure threshold), and the duration the breaker remained in the Open state. This history is invaluable for post-incident analysis and for tuning threshold values based on observed behavior.

---

[← Back to features](/features/)

[← Back to home](/)
