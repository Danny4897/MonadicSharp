---
layout: home

hero:
  name: "MonadicSharp.Recovery"
  text: "Self-healing pipelines for .NET"
  tagline: "Intercept specific failures, attempt recovery, and merge back to the success track — without breaking Railway-Oriented Programming flow."
  actions:
    - theme: brand
      text: Get Started
      link: /recovery/getting-started
    - theme: alt
      text: The Three Lanes
      link: /recovery/three-lanes
    - theme: alt
      text: GitHub
      link: https://github.com/Danny4897/MonadicSharp.Recovery

features:
  - icon: 🔄
    title: RescueAsync
    details: One-shot recovery. When a specific error occurs, attempt a single corrective action and merge back to the green track. If recovery fails, stay on the red track.
    link: /recovery/api/rescue-async
    linkText: RescueAsync docs

  - icon: 🔁
    title: StartFixBranchAsync
    details: Multi-attempt recovery with configurable delay. Try up to N times before giving up — each attempt is logged, delayable, and can target specific error predicates.
    link: /recovery/api/start-fix-branch-async
    linkText: StartFixBranchAsync docs

  - icon: 🛣️
    title: The Three Lanes
    details: Green (success), Amber (recovery), Red (failure). Recovery.Amber is a first-class lane — not an exception handler bolted on top.
    link: /recovery/three-lanes
    linkText: Concept docs

  - icon: 📡
    title: Recovery Telemetry
    details: Implement IRecoveryTelemetry to receive structured events for every recovery attempt — start, success, failure, and exhaustion. Integrates with OpenTelemetry.
    link: /recovery/api/telemetry
    linkText: Telemetry docs

  - icon: 🎯
    title: Error Predicates
    details: Target only the errors that should trigger recovery. Predicates compose — match by type, code, severity, or any custom condition. Other errors pass through unchanged.

  - icon: 🔗
    title: Composable
    details: Recovery operators chain naturally with MonadicSharp Bind/Map. No wrapping, no unwrapping — the amber track is transparent to the rest of your pipeline.
---
