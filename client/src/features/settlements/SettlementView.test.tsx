import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { server } from '../../test/server'
import { SettlementView } from './SettlementView'

const members = [
  { id: 'user-1', displayName: 'Owner' },
  { id: 'user-2', displayName: 'Member' },
]

describe('SettlementView', () => {
  it('renders each member\'s balance and the suggested transfers', async () => {
    server.use(
      http.get('/api/topics/t-1/balances', () =>
        HttpResponse.json([
          { userId: 'user-1', netBalance: 25 },
          { userId: 'user-2', netBalance: -25 },
        ]),
      ),
      http.get('/api/topics/t-1/settlements', () =>
        HttpResponse.json([{ fromUserId: 'user-2', toUserId: 'user-1', amount: 25 }]),
      ),
    )

    render(<SettlementView topicId="t-1" members={members} />)

    expect(await screen.findByText(/is owed 25\.00/i)).toBeInTheDocument()
    expect(screen.getByText(/owes 25\.00/i)).toBeInTheDocument()
    expect(screen.getByText(/member.*owes.*owner.*25\.00/i)).toBeInTheDocument()
  })

  it('shows an all-settled message when there are no suggested transfers', async () => {
    server.use(
      http.get('/api/topics/t-1/balances', () => HttpResponse.json([])),
      http.get('/api/topics/t-1/settlements', () => HttpResponse.json([])),
    )

    render(<SettlementView topicId="t-1" members={members} />)

    expect(await screen.findByText(/all settled up/i)).toBeInTheDocument()
  })

  it('marks a transfer as paid and removes it from the list', async () => {
    // Models the real backend's behavior: once marked paid, the transfer is
    // excluded from every subsequent settlements fetch (FR-16).
    let pendingTransfers = [{ fromUserId: 'user-2', toUserId: 'user-1', amount: 25 }]
    server.use(
      http.get('/api/topics/t-1/balances', () =>
        HttpResponse.json([
          { userId: 'user-1', netBalance: 25 },
          { userId: 'user-2', netBalance: -25 },
        ]),
      ),
      http.get('/api/topics/t-1/settlements', () => HttpResponse.json(pendingTransfers)),
      http.post('/api/topics/t-1/settlements/mark-paid', async ({ request }) => {
        const body = (await request.json()) as { fromUserId: string; toUserId: string; amount: number }
        pendingTransfers = pendingTransfers.filter(
          (t) => !(t.fromUserId === body.fromUserId && t.toUserId === body.toUserId),
        )
        return HttpResponse.json({
          id: 's-1',
          fromUserId: body.fromUserId,
          toUserId: body.toUserId,
          amount: body.amount,
          recordedAt: '2026-01-01T00:00:00Z',
        })
      }),
    )

    render(<SettlementView topicId="t-1" members={members} />)

    const user = userEvent.setup()
    await user.click(await screen.findByRole('button', { name: /mark as paid/i }))

    await waitFor(() => expect(screen.queryByRole('button', { name: /mark as paid/i })).not.toBeInTheDocument())
    expect(await screen.findByText(/all settled up/i)).toBeInTheDocument()
  })
})
