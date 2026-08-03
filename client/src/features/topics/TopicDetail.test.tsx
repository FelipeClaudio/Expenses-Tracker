import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it, vi } from 'vitest'
import { server } from '../../test/server'
import { TopicDetail } from './TopicDetail'

const topic = {
  id: 't-1',
  parentTopicId: null,
  rootTopicId: 't-1',
  name: 'Portugal Trip',
  description: null,
  inviteCode: 'abc123',
  createdByUserId: 'user-1',
  createdAt: '2026-01-01T00:00:00Z',
}

function mockTopicRoutes() {
  server.use(
    http.get('/api/topics/t-1', () => HttpResponse.json(topic)),
    http.get('/api/topics/t-1/subtopics', () =>
      HttpResponse.json([
        { id: 't-2', parentTopicId: 't-1', rootTopicId: 't-1', name: 'Fuel', description: null, inviteCode: null, createdByUserId: 'user-1', createdAt: '2026-01-01T00:00:00Z' },
      ]),
    ),
    http.get('/api/topics/t-1/expenses', () =>
      HttpResponse.json([
        {
          id: 'e-1',
          topicId: 't-1',
          description: 'Dinner',
          amount: 50,
          paidByUserId: 'user-1',
          expenseDate: '2026-01-02T00:00:00Z',
          createdByUserId: 'user-1',
          createdAt: '2026-01-02T00:00:00Z',
          participants: [{ userId: 'user-1', shareAmount: 25 }, { userId: 'user-2', shareAmount: 25 }],
        },
      ]),
    ),
  )
}

describe('TopicDetail', () => {
  it('renders the topic name, its subtopics, and its expense history', async () => {
    mockTopicRoutes()

    render(<TopicDetail topicId="t-1" />)

    expect(await screen.findByRole('heading', { name: 'Portugal Trip' })).toBeInTheDocument()
    expect(screen.getByText('Fuel')).toBeInTheDocument()
    expect(screen.getByText('Dinner')).toBeInTheDocument()
    expect(screen.getByText(/50/)).toBeInTheDocument()
  })

  it('shows the invite code for a root topic', async () => {
    mockTopicRoutes()

    render(<TopicDetail topicId="t-1" />)

    expect(await screen.findByText(/abc123/)).toBeInTheDocument()
  })

  it('creates a new subtopic and adds it to the list', async () => {
    mockTopicRoutes()
    server.use(
      http.post('/api/topics/t-1/subtopics', async ({ request }) => {
        const body = (await request.json()) as { name: string }
        return HttpResponse.json({
          id: 't-3',
          parentTopicId: 't-1',
          rootTopicId: 't-1',
          name: body.name,
          description: null,
          inviteCode: null,
          createdByUserId: 'user-1',
          createdAt: '2026-01-01T00:00:00Z',
        })
      }),
    )

    render(<TopicDetail topicId="t-1" />)
    await screen.findByText('Fuel')

    const user = userEvent.setup()
    await user.type(screen.getByLabelText(/subtopic name/i), 'Lodging')
    await user.click(screen.getByRole('button', { name: /add subtopic/i }))

    expect(await screen.findByText('Lodging')).toBeInTheDocument()
  })

  it('calls onOpenSubtopic when a subtopic is selected', async () => {
    mockTopicRoutes()
    const onOpenSubtopic = vi.fn()

    render(<TopicDetail topicId="t-1" onOpenSubtopic={onOpenSubtopic} />)

    const user = userEvent.setup()
    await user.click(await screen.findByText('Fuel'))

    await waitFor(() => expect(onOpenSubtopic).toHaveBeenCalledWith('t-2'))
  })
})
