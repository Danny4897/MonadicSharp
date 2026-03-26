# Deployment

This page covers how to deploy AgentScope to various environments — from a local Docker setup to a production cloud deployment.

::: warning Work in Progress
This page is under construction. AgentScope is in active development.
:::

## Docker Compose (Local)

When this page is complete, it will provide a ready-to-use `docker-compose.yml` that starts the AgentScope backend, the Blazor frontend, and a PostgreSQL instance with a single command. It will explain volume mounts for data persistence, how to access the dashboard in a browser after startup, and the environment variables required to wire the containers together.

## Cloud Deployment Options

This section will cover three production-grade deployment targets:

- **Railway** — the simplest path for solo developers and small teams. Includes a one-click deploy button and guidance on setting up the Railway PostgreSQL plugin as the AgentScope database.
- **Azure Container Apps** — the recommended path for enterprise or high-volume deployments. Covers creating the Container App environment, configuring scaling rules based on OTLP ingestion load, and connecting to Azure Database for PostgreSQL.
- **Self-hosted** — for teams running on-premises or with strict data residency requirements. Covers deploying the Docker images to any host with Docker Engine.

## Environment Variables

This section will serve as a complete reference for every environment variable that AgentScope reads at startup — database connection strings, OTLP listener ports, authentication secrets, retention policy settings, and feature flags — along with their defaults and valid value ranges.

---

[← Back to home](/)
