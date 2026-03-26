# Alerts

This page covers AgentScope's alerting system — how to configure conditions that trigger notifications when pipeline health degrades.

::: warning Work in Progress
This page is under construction. AgentScope is in active development.
:::

## Alert Conditions

When this page is complete, it will document all supported alert condition types. These include threshold-based alerts (e.g., "error rate exceeds 5% over the last 10 minutes"), anomaly-based alerts (e.g., "p99 latency increases by more than 50% compared to the rolling baseline"), budget alerts (e.g., "cumulative token spend in the last 24 hours exceeds a configured limit"), and circuit breaker alerts (e.g., "any circuit breaker has been in the Open state for more than 5 minutes"). Each condition type will be documented with its configuration parameters and evaluation frequency.

## Notification Channels

This section will describe the supported notification delivery channels. AgentScope ships with built-in support for Slack (via incoming webhooks), email (via SMTP or SendGrid), and generic outbound webhooks for integration with PagerDuty, Teams, or any other system that accepts HTTP POST notifications. The page will include configuration examples for each channel and explain how to test a channel by sending a sample notification.

## Alert Management

This section will explain the alert lifecycle in AgentScope: how alerts are created and stored, how they transition between Pending, Firing, and Resolved states, how to configure cooldown periods to avoid notification storms, and how to silence an alert temporarily during a planned maintenance window. It will also cover the alert history view, which shows a log of all fired alerts and their resolution times.

---

[← Back to features](/features/)

[← Back to home](/)
