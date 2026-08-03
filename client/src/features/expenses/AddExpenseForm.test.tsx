import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it, vi } from 'vitest'
import { server } from '../../test/server'
import { AddExpenseForm } from './AddExpenseForm'

const members = [
  { id: 'user-1', displayName: 'Owner' },
  { id: 'user-2', displayName: 'Member' },
]

describe('AddExpenseForm', () => {
  it('logs an expense with the selected payer and participants', async () => {
    let capturedBody: unknown
    server.use(
      http.post('/api/topics/t-1/expenses', async ({ request }) => {
        capturedBody = await request.json()
        return HttpResponse.json({
          id: 'e-1',
          topicId: 't-1',
          description: 'Dinner',
          amount: 40,
          paidByUserId: 'user-1',
          expenseDate: '2026-01-02T00:00:00.000Z',
          createdByUserId: 'user-1',
          createdAt: '2026-01-02T00:00:00Z',
          participants: [{ userId: 'user-1', shareAmount: 20 }, { userId: 'user-2', shareAmount: 20 }],
        })
      }),
    )
    const onExpenseLogged = vi.fn()

    render(<AddExpenseForm topicId="t-1" members={members} onExpenseLogged={onExpenseLogged} />)

    const user = userEvent.setup()
    await user.type(screen.getByLabelText(/description/i), 'Dinner')
    await user.clear(screen.getByLabelText(/amount/i))
    await user.type(screen.getByLabelText(/amount/i), '40')
    await user.selectOptions(screen.getByLabelText(/paid by/i), 'user-1')
    // Both participants are checked by default; leave as-is and submit.
    await user.click(screen.getByRole('button', { name: /log expense/i }))

    await waitFor(() => expect(onExpenseLogged).toHaveBeenCalled())
    expect(capturedBody).toMatchObject({
      description: 'Dinner',
      amount: 40,
      paidByUserId: 'user-1',
      participantUserIds: expect.arrayContaining(['user-1', 'user-2']),
    })
  })

  it('requires at least one participant', async () => {
    render(<AddExpenseForm topicId="t-1" members={members} />)

    const user = userEvent.setup()
    await user.type(screen.getByLabelText(/description/i), 'Dinner')
    await user.clear(screen.getByLabelText(/amount/i))
    await user.type(screen.getByLabelText(/amount/i), '40')
    await user.click(screen.getByLabelText('Owner'))
    await user.click(screen.getByLabelText('Member'))
    await user.click(screen.getByRole('button', { name: /log expense/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/participant/i)
  })

  it('displays the server-returned error message when submission is rejected', async () => {
    server.use(
      http.post('/api/topics/t-1/expenses', () => new HttpResponse('Amount must be positive.', { status: 400 })),
    )

    render(<AddExpenseForm topicId="t-1" members={members} />)

    const user = userEvent.setup()
    await user.type(screen.getByLabelText(/description/i), 'Dinner')
    await user.clear(screen.getByLabelText(/amount/i))
    await user.type(screen.getByLabelText(/amount/i), '40')
    await user.click(screen.getByRole('button', { name: /log expense/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/amount must be positive/i)
  })
})
