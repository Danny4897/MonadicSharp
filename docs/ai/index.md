---
layout: home

hero:
  name: "MonadicSharp.AI"
  text: "Typed error handling for LLM pipelines"
  tagline: "Exponential backoff, execution tracing, structured output validation, and streaming — all composable with Result<T>."
  actions:
    - theme: brand
      text: Get Started
      link: /ai/getting-started
    - theme: alt
      text: API Reference
      link: /ai/api/ai-error
    - theme: alt
      text: GitHub
      link: https://github.com/Danny4897/MonadicSharp.AI

features:
  - icon: 🔴
    title: AiError
    details: Semantic error types for every LLM failure mode — RateLimit, Timeout, TokenExhausted, ContentFiltered. Each carries retry metadata so callers know exactly what to do.
    link: /ai/api/ai-error
    linkText: AiError docs

  - icon: 🔄
    title: RetryResult<T>
    details: Exponential backoff with jitter, built on Result<T>. Terminal errors (ContentFiltered, Unauthorized) short-circuit immediately — no wasted retries.
    link: /ai/api/retry-result
    linkText: RetryResult docs

  - icon: ✅
    title: ValidatedResult<T>
    details: JSON parsing and domain validation in a single composable step. Parsing errors and validation failures surface as distinct, typed errors.
    link: /ai/api/validated-result
    linkText: ValidatedResult docs

  - icon: 🕵️
    title: AgentResult<T>
    details: Multi-step pipeline tracing with per-step timing, token counts, and error context. Full execution history as a value — no external APM required for development.
    link: /ai/api/agent-result
    linkText: AgentResult docs

  - icon: 🌊
    title: StreamResult
    details: Handle streaming completions without try/catch. Mid-stream errors are captured as Result<StreamError> — the partial response is preserved for debugging.
    link: /ai/api/stream-result
    linkText: StreamResult docs

  - icon: 🔗
    title: Composable
    details: Every type integrates seamlessly with MonadicSharp pipelines via Bind, Map, and Match. Mix AI operations with your domain logic — no impedance mismatch.
---
