import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'MonadicSharp',
  description: 'Railway-Oriented Programming for C# — composable, explicit error handling without exceptions.',
  base: '/MonadicSharp/',
  cleanUrls: true,

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
        ],
        activeMatch: '/getting-started|/pipelines|/why'
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
        activeMatch: '/core/'
      },
      {
        text: 'Ecosystem',
        items: [
          { text: 'Overview', link: '/ecosystem/' },
          { text: 'MonadicSharp.AI', link: '/ai' },
          { text: 'Templates', link: '/templates' },
        ],
        activeMatch: '/ecosystem/|/templates|/ai'
      },
      {
        text: 'v1.5',
        items: [
          { text: 'Changelog', link: 'https://github.com/Danny4897/MonadicSharp/blob/main/CHANGELOG.md' },
          { text: 'NuGet', link: 'https://www.nuget.org/packages/MonadicSharp/' },
          { text: 'Contributing', link: 'https://github.com/Danny4897/MonadicSharp/blob/main/CONTRIBUTING.md' },
        ]
      }
    ],

    sidebar: [
      {
        text: 'Guide',
        collapsed: false,
        items: [
          { text: 'Getting Started', link: '/getting-started' },
          { text: 'Async Pipelines', link: '/pipelines' },
          { text: 'Why MonadicSharp', link: '/why' },
        ]
      },
      {
        text: 'Core Types',
        collapsed: false,
        items: [
          { text: 'Result<T>', link: '/core/result' },
          { text: 'Option<T>', link: '/core/option' },
          { text: 'Either<L,R>', link: '/core/either' },
          { text: 'Error', link: '/core/error' },
          { text: 'Try', link: '/core/try' },
        ]
      },
      {
        text: 'Ecosystem',
        collapsed: false,
        items: [
          { text: 'Overview', link: '/ecosystem/' },
          { text: 'MonadicSharp.AI', link: '/ai' },
          { text: 'Templates', link: '/templates' },
        ]
      },
    ],

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
