---
layout: home

hero:
  name: "MonadicSharp.Azure"
  text: "Railway-Oriented Programming for Azure"
  tagline: "Every SDK call wrapped in Result<T> or Option<T>. No scattered try/catch, no null checks — just composable pipelines."
  actions:
    - theme: brand
      text: Get Started
      link: /azure/getting-started
    - theme: alt
      text: Packages
      link: /azure/packages/core
    - theme: alt
      text: GitHub
      link: https://github.com/Danny4897/MonadicSharp.Azure

features:
  - icon: ⚡
    title: Azure Functions
    details: v4 Isolated Worker integration. Handlers return Result<IActionResult> — HTTP status codes map automatically from your typed errors. No try/catch at the function boundary.
    link: /azure/packages/functions
    linkText: Functions docs

  - icon: 🌌
    title: CosmosDb
    details: Container operation extensions — read, upsert, delete, query. NotFound becomes Option<T>, conflicts become typed errors. Cosmos exceptions never escape your service layer.
    link: /azure/packages/cosmosdb
    linkText: CosmosDb docs

  - icon: 📨
    title: Service Bus
    details: Sender and receiver wrappers that return Result<T>. Dead-letter and poison message handling as first-class typed errors — no unhandled MessageLockLostException.
    link: /azure/packages/messaging
    linkText: Messaging docs

  - icon: 📦
    title: Blob Storage
    details: Upload, download, and delete as Result<T>. Blob not found returns Option<T>. Quota and access errors are typed — catch them exactly where they matter.
    link: /azure/packages/storage
    linkText: Storage docs

  - icon: 🔑
    title: Key Vault
    details: Secret access as Result<string>. Missing secrets become typed SecretError, not KeyVaultErrorException. Compose secret retrieval directly in your initialization pipelines.
    link: /azure/packages/keyvault
    linkText: KeyVault docs

  - icon: 🤖
    title: Azure OpenAI
    details: Chat completions and embeddings as Result<T>. Rate limits, token exhaustion, and content filtering all surface as typed AzureAiError — composable with MonadicSharp.AI.
    link: /azure/packages/openai
    linkText: OpenAI docs
---
