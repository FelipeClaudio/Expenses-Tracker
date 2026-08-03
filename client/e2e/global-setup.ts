import { execSync } from 'node:child_process'

// Only starts Postgres (not api/client) so it never clashes with the ports
// Playwright's own webServer entries use for the API and client dev servers.
// Requires Docker running locally - the same prerequisite the
// Testcontainers-backed Api.IntegrationTests suite already has.
export default function globalSetup() {
  execSync('docker compose up -d postgres', { cwd: '..', stdio: 'inherit' })
}
