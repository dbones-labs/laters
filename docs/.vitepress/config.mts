import { defineConfig } from 'vitepress'

// https://vitepress.dev/reference/site-config
export default defineConfig({
  title: "laters",
  base: "/laters/",
  description: "doing work... laters!",
  lastUpdated: false,
  themeConfig: {
    // https://vitepress.dev/reference/default-theme-config
    logo: 'https://raw.githubusercontent.com/dbones-labs/laters/main/docs/logo.png',
    nav: [
      { text: 'Home', link: '/' },
      { text: 'Quick Start', link: '/overview/quick-start' }
    ],
    
    search: {
      provider: 'local'
    },
    
    footer: {
      message: 'Apache2 Licensed',
      copyright: 'Copyright © dbones labs'
    },
    
    
    editLink: {
      pattern: 'https://github.com/dbones-labs/laters/edit/main/docs/:path',
      text: 'Suggest changes to this page'
    },
    
    sidebar: [
      {
        text: 'Overview',
        items: [
          { text: 'What is Laters', link: '/overview/what-is-laters' },
          { text: 'Architecture', link: '/overview/architecture' },
          { text: 'Quick start', link: '/overview/quick-start' },
          { text: 'Model', link: '/overview/model' }
        ]
      },
      {
        text: 'Configuration',
        items: [
          { text: 'program.cs', link: '/configuration/programcs' },
          { text: 'Telemetry', link: '/telemetry/open-telemtery' }
        ]
      },
      {
        text: 'Processing',
        items: [
          { text: 'Client Pipeline', link: '/processing/client-pipeline' },
          {
            text: 'Handler',
            items: [
              { text: 'Job Handler', link: '/processing/job-handler' },
              { text: 'Minimal Api', link: '/processing/minimal-api' }
            ]
          },
          { text: 'Custom Actions', link: '/processing/custom-actions' }
        ]
      },
      {
        text: 'Scheduling',
        items: [
          { text: 'Queueing', link: '/scheduling/queueing' },
          { text: 'For-Later (fire-forget)', link: '/scheduling/for-later' },
          {
            text: 'Cron',
            items: [
              { text: 'Many-For-Later (Cron)', link: '/scheduling/many-for-later' },
              { text: 'Many-For-Later (Global Cron)', link: '/scheduling/global-many-for-later' }
            ]
          }
        ]
      },
      {
        text: 'Storage',
        items: [
          { text: 'Marten', link: '/storage/marten' },
          { text: 'EntityFramework', link: '/storage/entity-framework' },
          { text: 'custom', link: '/storage/custom' }
        ]
      }
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/dbones-labs/laters' }
    ]
  }
})
