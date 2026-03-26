import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'MonadicSharp',
  description: 'Railway-Oriented Programming for C# — composable, explicit error handling without exceptions.',
  base: '/MonadicSharp/',
  cleanUrls: true,
  ignoreDeadLinks: true,

  head: [
    ['meta', { name: 'theme-color', content: '#7c3aed' }],
    ['meta', { property: 'og:type', content: 'website' }],
    ['meta', { property: 'og:title', content: 'MonadicSharp' }],
    ['meta', { property: 'og:description', content: 'Railway-Oriented Programming for C#. Replace exception-driven control flow with composable, explicit error handling.' }],
    ['meta', { property: 'og:image', content: 'https://danny4897.github.io/MonadicSharp/logo.svg' }],
    ['meta', { name: 'twitter:card', content: 'summary' }],
  ],

  themeConfig: {
    logo: '/logo.svg',
    siteTitle: 'MonadicSharp',

    nav: [
      {
        text: 'Guide',
        items: [
          { text: 'Getting Started', link: '/getting-started' },
          { text: 'Async Pipelines', link: '/pipelines' },
          { text: 'Why MonadicSharp', link: '/why' },
          { text: 'Try it ▶', link: '/try-it' },
        ],
        activeMatch: '^/(getting-started|pipelines|why|try-it)'
      },
      {
        text: 'Core',
        items: [
          { text: 'Result<T>', link: '/core/result' },
          { text: 'Option<T>', link: '/core/option' },
          { text: 'Either<L,R>', link: '/core/either' },
          { text: 'Error', link: '/core/error' },
          { text: 'Try', link: '/core/try' },
        ],
        activeMatch: '^/core/'
      },
      {
        text: 'Packages',
        items: [
          {
            text: 'Production',
            items: [
              { text: 'MonadicSharp.Framework', link: '/framework/' },
              { text: 'MonadicSharp.AI',        link: '/ai/' },
              { text: 'MonadicSharp.Recovery',  link: '/recovery/' },
              { text: 'MonadicSharp.Azure',     link: '/azure/' },
              { text: 'MonadicSharp.DI',        link: '/di/' },
            ],
          },
          {
            text: 'Tooling',
            items: [
              { text: 'MonadicLeaf',            link: '/leaf/' },
              { text: 'MonadicSharp × OpenCode',link: '/opencode/' },
              { text: 'AgentScope',             link: '/agentscope/' },
            ],
          },
        ],
        activeMatch: '^/(framework|ai|recovery|azure|di|leaf|opencode|agentscope)/'
      },
      {
        text: 'v1.5',
        items: [
          { text: 'Changelog',    link: 'https://github.com/Danny4897/MonadicSharp/blob/main/CHANGELOG.md' },
          { text: 'NuGet',        link: 'https://www.nuget.org/packages/MonadicSharp/' },
          { text: 'Contributing', link: 'https://github.com/Danny4897/MonadicSharp/blob/main/CONTRIBUTING.md' },
        ]
      }
    ],

    sidebar: {
      // ── Main docs ──────────────────────────────────────────────────────────
      '/getting-started': [{ text: 'Guide', items: [
        { text: 'Getting Started', link: '/getting-started' },
        { text: 'Async Pipelines', link: '/pipelines' },
        { text: 'Why MonadicSharp', link: '/why' },
        { text: 'Try it ▶', link: '/try-it' },
      ]}],
      '/core/': [
        { text: 'Guide', collapsed: false, items: [
          { text: 'Getting Started', link: '/getting-started' },
          { text: 'Async Pipelines', link: '/pipelines' },
          { text: 'Try it ▶', link: '/try-it' },
        ]},
        { text: 'Core Types', collapsed: false, items: [
          { text: 'Result<T>', link: '/core/result' },
          { text: 'Option<T>', link: '/core/option' },
          { text: 'Either<L,R>', link: '/core/either' },
          { text: 'Error', link: '/core/error' },
          { text: 'Try', link: '/core/try' },
        ]},
      ],

      // ── MonadicSharp.DI ────────────────────────────────────────────────────
      '/di/': [
        { text: 'MonadicSharp.DI', collapsed: false, items: [
          { text: 'Overview',          link: '/di/' },
          { text: 'Getting Started',   link: '/di/getting-started' },
          { text: 'CQRS Pattern',      link: '/di/cqrs' },
          { text: 'Pipeline Behaviors',link: '/di/pipeline-behaviors' },
        ]},
        { text: 'API Reference', collapsed: false, items: [
          { text: 'IQueryHandler',     link: '/di/api/query-handler' },
          { text: 'ICommandHandler',   link: '/di/api/command-handler' },
          { text: 'IPipelineBehavior', link: '/di/api/pipeline-behavior' },
          { text: 'INotification',     link: '/di/api/notification' },
        ]},
      ],

      // ── MonadicSharp.AI ───────────────────────────────────────────────────
      '/ai/': [
        { text: 'MonadicSharp.AI', collapsed: false, items: [
          { text: 'Overview',        link: '/ai/' },
          { text: 'Getting Started', link: '/ai/getting-started' },
          { text: 'Why AI errors?',  link: '/ai/why' },
        ]},
        { text: 'API Reference', collapsed: false, items: [
          { text: 'AiError',         link: '/ai/api/ai-error' },
          { text: 'RetryResult',     link: '/ai/api/retry-result' },
          { text: 'ValidatedResult', link: '/ai/api/validated-result' },
          { text: 'AgentResult',     link: '/ai/api/agent-result' },
          { text: 'StreamResult',    link: '/ai/api/stream-result' },
        ]},
      ],

      // ── MonadicSharp.Recovery ─────────────────────────────────────────────
      '/recovery/': [
        { text: 'MonadicSharp.Recovery', collapsed: false, items: [
          { text: 'Overview',          link: '/recovery/' },
          { text: 'Getting Started',   link: '/recovery/getting-started' },
          { text: 'Three Lanes',       link: '/recovery/three-lanes' },
        ]},
        { text: 'API Reference', collapsed: false, items: [
          { text: 'RescueAsync',       link: '/recovery/api/rescue-async' },
          { text: 'StartFixBranch',    link: '/recovery/api/start-fix-branch-async' },
          { text: 'Telemetry',         link: '/recovery/api/telemetry' },
        ]},
      ],

      // ── MonadicSharp.Azure ────────────────────────────────────────────────
      '/azure/': [
        { text: 'MonadicSharp.Azure', collapsed: false, items: [
          { text: 'Overview',          link: '/azure/' },
          { text: 'Getting Started',   link: '/azure/getting-started' },
          { text: 'Error Mapping',     link: '/azure/error-mapping' },
        ]},
        { text: 'Packages', collapsed: false, items: [
          { text: 'Core',              link: '/azure/packages/core' },
          { text: 'Functions',         link: '/azure/packages/functions' },
          { text: 'CosmosDB',          link: '/azure/packages/cosmosdb' },
          { text: 'Service Bus',       link: '/azure/packages/messaging' },
          { text: 'Blob Storage',      link: '/azure/packages/storage' },
          { text: 'Key Vault',         link: '/azure/packages/keyvault' },
          { text: 'Azure OpenAI',      link: '/azure/packages/openai' },
        ]},
      ],

      // ── MonadicSharp.Framework ────────────────────────────────────────────
      '/framework/': [
        { text: 'MonadicSharp.Framework', collapsed: false, items: [
          { text: 'Overview',          link: '/framework/' },
          { text: 'Getting Started',   link: '/framework/getting-started' },
          { text: 'Architecture',      link: '/framework/architecture' },
          { text: 'Why Framework?',    link: '/framework/why' },
        ]},
        { text: 'Packages', collapsed: false, items: [
          { text: 'Agents',            link: '/framework/packages/agents' },
          { text: 'Security',          link: '/framework/packages/security' },
          { text: 'Telemetry',         link: '/framework/packages/telemetry' },
          { text: 'Http',              link: '/framework/packages/http' },
          { text: 'Persistence',       link: '/framework/packages/persistence' },
          { text: 'Caching',           link: '/framework/packages/caching' },
        ]},
      ],

      // ── MonadicLeaf ───────────────────────────────────────────────────────
      '/leaf/': [
        { text: 'MonadicLeaf', collapsed: false, items: [
          { text: 'Overview',          link: '/leaf/' },
          { text: 'Getting Started',   link: '/leaf/getting-started' },
          { text: 'How it works',      link: '/leaf/how-it-works' },
          { text: 'Green Score',       link: '/leaf/green-score' },
          { text: 'CLI',               link: '/leaf/cli' },
          { text: 'CI Integration',    link: '/leaf/ci' },
        ]},
        { text: 'Rules', collapsed: false, items: [
          { text: 'All Rules',         link: '/leaf/rules/' },
          { text: 'GC001 — No throw',  link: '/leaf/rules/gc001' },
          { text: 'GC002 — No catch',  link: '/leaf/rules/gc002' },
          { text: 'GC003 — No null',   link: '/leaf/rules/gc003' },
          { text: 'GC004 — No bool',   link: '/leaf/rules/gc004' },
        ]},
      ],

      // ── MonadicSharp × OpenCode ───────────────────────────────────────────
      '/opencode/': [
        { text: 'MonadicSharp × OpenCode', collapsed: false, items: [
          { text: 'Overview',            link: '/opencode/' },
          { text: 'Getting Started',     link: '/opencode/getting-started' },
          { text: 'How it works',        link: '/opencode/how-it-works' },
          { text: 'Green Code',          link: '/opencode/green-code' },
          { text: 'ROP Patterns',        link: '/opencode/rol-patterns' },
        ]},
        { text: 'Commands', collapsed: false, items: [
          { text: '/forge-analyze',      link: '/opencode/commands/forge-analyze' },
          { text: '/green-check',        link: '/opencode/commands/green-check' },
          { text: '/migrate',            link: '/opencode/commands/migrate' },
        ]},
      ],

      // ── AgentScope ────────────────────────────────────────────────────────
      '/agentscope/': [
        { text: 'AgentScope', collapsed: false, items: [
          { text: 'Overview',            link: '/agentscope/' },
          { text: 'Getting Started',     link: '/agentscope/getting-started' },
          { text: 'Architecture',        link: '/agentscope/architecture' },
          { text: 'Deploy',              link: '/agentscope/deploy' },
        ]},
        { text: 'Features', collapsed: false, items: [
          { text: 'Tracing',             link: '/agentscope/features/tracing' },
          { text: 'Metrics',             link: '/agentscope/features/metrics' },
          { text: 'Circuit Breaker',     link: '/agentscope/features/circuit-breaker' },
          { text: 'Alerts',              link: '/agentscope/features/alerts' },
        ]},
        { text: 'Integrations', collapsed: false, items: [
          { text: 'Extensions.AI',       link: '/agentscope/integrations/extensions-ai' },
          { text: 'Semantic Kernel',     link: '/agentscope/integrations/semantic-kernel' },
          { text: 'Telemetry',           link: '/agentscope/integrations/telemetry' },
        ]},
      ],
    },

    socialLinks: [
      { icon: 'github', link: 'https://github.com/Danny4897/MonadicSharp' },
    ],

    footer: {
      message: 'Released under the MIT License.',
      copyright: 'Copyright © 2024–2026 Danny4897'
    },

    editLink: {
      pattern: 'https://github.com/Danny4897/MonadicSharp/edit/main/docs/:path',
      text: 'Edit this page on GitHub'
    },

    search: {
      provider: 'local',
      options: {
        detailedView: true
      }
    },

    outline: {
      level: [2, 3],
      label: 'On this page'
    }
  },

  markdown: {
    theme: {
      light: 'github-light',
      dark: 'one-dark-pro'
    },
    lineNumbers: false
  }
})
