import { defineConfig, configDefaults, coverageConfigDefaults } from 'vitest/config'
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
    // Extends (rather than replaces) Vitest's own default exclusions.
    exclude: [...configDefaults.exclude, 'e2e/**'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'html', 'json-summary', 'json'],
      // Without `all`, v8 only reports files a test actually imported -
      // untested files (e.g. AppShell, SignInPage) would silently vanish
      // from the report instead of counting as 0%, overstating the total.
      all: true,
      include: ['src/**/*.{ts,tsx}'],
      exclude: [
        ...coverageConfigDefaults.exclude,
        'e2e/**',
        'src/main.tsx',
        'src/test/**',
      ],
    },
  },
})
