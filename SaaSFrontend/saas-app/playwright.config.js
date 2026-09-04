import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  timeout: 30000,
  expect: {
    timeout: 5000
  },
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:5183',
    storageState: {
      cookies: [],
      origins: [
        {
          origin: 'http://localhost:5183',
          localStorage: [
            { name: 'PARAGLIDE_LOCALE', value: 'ru' }
          ]
        }
      ]
    },
    trace: 'on-first-retry',
    video: 'off',
    screenshot: 'only-on-failure'
  },
  webServer: {
    command: 'npm run dev -- --port 5183',
    port: 5183,
    reuseExistingServer: false,
    timeout: 30000
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
