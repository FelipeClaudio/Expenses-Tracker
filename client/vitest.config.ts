import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    globals: false,
    // e2e/ holds Playwright specs (playwright.config.ts owns those), not
    // Vitest ones - without this exclude, Vitest's default include glob
    // picks up *.spec.ts anywhere and tries to run them as unit tests.
    exclude: ['**/node_modules/**', '**/dist/**', 'e2e/**'],
  },
})
