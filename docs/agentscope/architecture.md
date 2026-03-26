# Architecture

This page describes the high-level architecture of AgentScope — how its components fit together to collect, store, and visualize observability data from AI pipelines.

::: warning Work in Progress
This page is under construction. AgentScope is in active development.
:::

## System Overview

When this page is complete, it will provide a full architectural diagram and narrative of AgentScope's components. The backend is a .NET API that receives telemetry data via the OpenTelemetry Protocol (OTLP), processes incoming spans and metrics, and persists them to a structured database. The frontend is a Blazor Server application that queries the backend and renders dashboards, trace timelines, and metric charts in real time.

## Backend and Database

This section will detail the backend's internal structure: the OTLP receiver endpoint, the ingestion pipeline that normalizes spans into AgentScope's domain model, and the database schema optimized for time-series trace data. It will cover supported database backends (SQLite for local development, PostgreSQL for production), the retention policy configuration, and how indexes are structured to keep trace queries fast even at scale.

## OpenTelemetry Integration

This section will explain how AgentScope is a standards-compliant OpenTelemetry backend. Any application instrumented with the OpenTelemetry .NET SDK — including those using `MonadicSharp.Framework.Telemetry`, Semantic Kernel, or `Microsoft.Extensions.AI` — can send data to AgentScope without any vendor-specific SDK. The section will cover the OTLP endpoint URL, supported signal types (traces, metrics), and authentication options.

---

[← Back to home](/)
