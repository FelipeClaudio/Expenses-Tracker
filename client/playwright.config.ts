import { defineConfig, devices } from '@playwright/test'

const API_PORT = 5179
const CLIENT_PORT = 5173

const apiConnectionString =
  'Host=localhost;Port=5432;Database=expenses_tracker;Username=expenses_tracker;Password=expenses_tracker'

export default defineConfig({
  testDir: './e2e',
  globalSetup: './e2e/global-setup.ts',
  fullyParallel: false,
  // CI additionally gets an HTML report on disk (uploaded as a build
  // artifact on failure - see ci.yml) since there's no terminal to read
  // the list reporter's output from after the run finishes.
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: `http://localhost:${CLIENT_PORT}`,
    trace: 'on-first-retry',
    // The API runs over HTTPS with the local ASP.NET Core dev certificate
    // (needed for Secure cookies - see NFR-11 - to actually be stored by a
    // real browser), which browsers don't trust by default.
    ignoreHTTPSErrors: true,
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
    { name: 'webkit', use: { ...devices['Desktop Safari'] } },
  ],
  webServer: [
    {
      command: 'dotnet run --no-launch-profile',
      cwd: '../src/Api',
      port: API_PORT,
      timeout: 180_000,
      reuseExistingServer: !process.env.CI,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: `https://localhost:${API_PORT}`,
        Auth__FakeMode: 'true',
        Cors__AllowedOrigins__0: `http://localhost:${CLIENT_PORT}`,
        ConnectionStrings__Default: apiConnectionString,
      },
    },
    {
      command: `npm run dev -- --port ${CLIENT_PORT} --strictPort`,
      port: CLIENT_PORT,
      timeout: 60_000,
      reuseExistingServer: !process.env.CI,
      env: {
        VITE_API_BASE_URL: `https://localhost:${API_PORT}`,
        VITE_AUTH_FAKE_MODE: 'true',
      },
    },
  ],
})
