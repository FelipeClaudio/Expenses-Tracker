import { http, HttpResponse } from 'msw'

// Default happy-path handlers used across component tests. Individual tests
// override specific routes via `server.use(...)` for their own scenarios.
export const handlers = [
  http.get('/api/users/me', () =>
    HttpResponse.json({
      id: 'user-1',
      email: 'owner@example.com',
      displayName: 'Owner',
      avatarUrl: null,
    }),
  ),

  http.get('/api/topics', () => HttpResponse.json([])),
]

export const defaultUser = {
  id: 'user-1',
  email: 'owner@example.com',
  displayName: 'Owner',
  avatarUrl: null,
}
