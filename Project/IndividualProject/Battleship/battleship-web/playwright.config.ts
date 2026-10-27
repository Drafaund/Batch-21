import { defineConfig, devices } from '@playwright/test'

const apiUrl = process.env.E2E_API_URL ?? 'http://localhost:5080'
const webPort = process.env.E2E_WEB_PORT ?? '5173'
const apiCommand = process.env.E2E_API_COMMAND ?? `dotnet run --project ../Battleship.Api --no-launch-profile --urls ${apiUrl}`

export default defineConfig({
  testDir: './e2e',
  timeout: 90_000,
  workers: 1,
  reporter: [['list']],
  use: {
    baseURL: `http://localhost:${webPort}`,
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: apiCommand,
      url: `${apiUrl}/swagger/index.html`,
      env: { ASPNETCORE_ENVIRONMENT: 'Development' },
      reuseExistingServer: !process.env.CI,
      timeout: 180_000,
    },
    {
      command: `npm run dev -- --port ${webPort} --strictPort`,
      url: `http://localhost:${webPort}`,
      env: { E2E_API_URL: apiUrl },
      reuseExistingServer: !process.env.CI,
      timeout: 60_000,
    },
  ],
})
