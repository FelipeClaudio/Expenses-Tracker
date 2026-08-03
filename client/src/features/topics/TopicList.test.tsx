import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it, vi } from 'vitest'
import { server } from '../../test/server'
import { TopicList } from './TopicList'

describe('TopicList', () => {
  it('renders the root topics returned by the API', async () => {
    server.use(
      http.get('/api/topics', () =>
        HttpResponse.json([
          { id: 't-1', parentTopicId: null, rootTopicId: 't-1', name: 'Portugal Trip', description: null, inviteCode: 'abc', createdByUserId: 'user-1', createdAt: '2026-01-01T00:00:00Z' },
          { id: 't-2', parentTopicId: null, rootTopicId: 't-2', name: 'Flatmates', description: null, inviteCode: 'def', createdByUserId: 'user-1', createdAt: '2026-01-01T00:00:00Z' },
        ]),
      ),
    )

    render(<TopicList />)

    expect(await screen.findByText('Portugal Trip')).toBeInTheDocument()
    expect(screen.getByText('Flatmates')).toBeInTheDocument()
  })

  it('shows an empty state when the user has no topics yet', async () => {
    server.use(http.get('/api/topics', () => HttpResponse.json([])))

    render(<TopicList />)

    expect(await screen.findByText(/no topics yet/i)).toBeInTheDocument()
  })

  it('creates a new topic and adds it to the list', async () => {
    server.use(
      http.get('/api/topics', () => HttpResponse.json([])),
      http.post('/api/topics', async ({ request }) => {
        const body = (await request.json()) as { name: string }
        return HttpResponse.json({
          id: 't-new',
          parentTopicId: null,
          rootTopicId: 't-new',
          name: body.name,
          description: null,
          inviteCode: 'xyz',
          createdByUserId: 'user-1',
          createdAt: '2026-01-01T00:00:00Z',
        })
      }),
    )

    render(<TopicList />)
    await screen.findByText(/no topics yet/i)

    const user = userEvent.setup()
    await user.type(screen.getByLabelText(/topic name/i), 'Ski Trip')
    await user.click(screen.getByRole('button', { name: /create topic/i }))

    expect(await screen.findByText('Ski Trip')).toBeInTheDocument()
  })

  it('calls onSelectTopic when a topic is chosen', async () => {
    server.use(
      http.get('/api/topics', () =>
        HttpResponse.json([
          { id: 't-1', parentTopicId: null, rootTopicId: 't-1', name: 'Portugal Trip', description: null, inviteCode: 'abc', createdByUserId: 'user-1', createdAt: '2026-01-01T00:00:00Z' },
        ]),
      ),
    )
    const onSelectTopic = vi.fn()

    render(<TopicList onSelectTopic={onSelectTopic} />)

    const user = userEvent.setup()
    await user.click(await screen.findByText('Portugal Trip'))

    await waitFor(() => expect(onSelectTopic).toHaveBeenCalledWith('t-1'))
  })
})
