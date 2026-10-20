import { defineConfig, devices } from '@playwright/test'

// The end-to-end tests drive a real browser against the real API.
// Both servers are started here unless they are already running (reuseExistingServer).
export default defineConfig({
  testDir: './e2e',
  timeout: 90_000,
  workers: 1,
  reporter: [['list']],
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: 'dotnet run --project ../Battleship.Api --no-launch-profile --urls http://localhost:5080',
      url: 'http://localhost:5080/swagger/index.html',
      env: { ASPNETCORE_ENVIRONMENT: 'Development' },
      reuseExistingServer: !process.env.CI,
      timeout: 180_000,
    },
    {
      command: 'npm run dev -- --port 5173 --strictPort',
      url: 'http://localhost:5173',
      reuseExistingServer: !process.env.CI,
      timeout: 60_000,
    },
  ],
})
