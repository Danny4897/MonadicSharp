---
layout: home

hero:
  name: "AgentScope"
  text: "AI agent observability for .NET"
  tagline: "See every agent. Trace every pipeline. Catch every failure — natively in .NET, built on MonadicSharp and OpenTelemetry."
  actions:
    - theme: brand
      text: Get Started
      link: /agentscope/getting-started
    - theme: alt
      text: Features
      link: /agentscope/features/tracing
    - theme: alt
      text: GitHub
      link: https://github.com/Danny4897/AgentScope

features:
  - icon: 🔭
    title: Pipeline Tracing
    details: Interactive waterfall visualization of every agent and span in your pipeline. Step durations, token counts, and Result<T> states — all in one view.
    link: /agentscope/features/tracing
    linkText: Tracing docs

  - icon: 📊
    title: Real-time Metrics
    details: Latency, throughput, failure rates, and token usage tracked in real time. Dashboards that understand Result<T> — failed results show in red, not as missing data.
    link: /agentscope/features/metrics
    linkText: Metrics docs

  - icon: ⚡
    title: Circuit Breaker Monitoring
    details: Visualize CircuitBreaker state transitions across your agents. Know when a downstream service is degraded before your users do.
    link: /agentscope/features/circuit-breaker
    linkText: Circuit breaker docs

  - icon: 🔔
    title: Alerts
    details: Configure alerts on Green Score, error rate, or specific error types. Notify via email, Slack, or Teams — no third-party APM required.
    link: /agentscope/features/alerts
    linkText: Alerts docs

  - icon: 🔗
    title: Auto-discovery
    details: AgentScope auto-discovers traces from MonadicSharp.Telemetry, Microsoft.Extensions.AI, and Semantic Kernel. Two lines of setup — no manual instrumentation.
    link: /agentscope/getting-started
    linkText: Quick setup

  - icon: 🛠️
    title: Speaks fluent C#
    details: Built on MonadicSharp, OpenTelemetry, and ASP.NET Core 8. Not a Python-first tool retrofitted for .NET — every concept maps directly to idiomatic C# patterns.
    link: /agentscope/architecture
    linkText: Architecture
---
