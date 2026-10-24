import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// The API address can be overridden, which the end-to-end tests use to talk to a second copy of the API.
const apiUrl = process.env.E2E_API_URL ?? 'http://localhost:5080'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // In development the React app is served by Vite and the API runs on its own port.
    proxy: {
      '/api': apiUrl,
      // SignalR: the hub also needs WebSocket upgrades to pass through.
      '/hubs': { target: apiUrl, ws: true },
    },
  },
  test: {
    environment: 'jsdom',
    include: ['src/**/*.test.{ts,tsx}'],
    setupFiles: ['./src/test/setup.ts'],
    css: false,
  },
})
