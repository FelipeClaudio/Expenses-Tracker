import { useState } from 'react'
import { ApiError, api } from '../../api/client'
import type { Expense } from '../../api/types'

interface Member {
  id: string
  displayName: string
}

interface AddExpenseFormProps {
  topicId: string
  members: Member[]
  onExpenseLogged?: (expense: Expense) => void
}

function today() {
  return new Date().toISOString().slice(0, 10)
}

export function AddExpenseForm({ topicId, members, onExpenseLogged }: AddExpenseFormProps) {
  const [description, setDescription] = useState('')
  const [amount, setAmount] = useState('')
  const [paidByUserId, setPaidByUserId] = useState(members[0]?.id ?? '')
  const [expenseDate, setExpenseDate] = useState(today())
  const [participantUserIds, setParticipantUserIds] = useState<string[]>(members.map((m) => m.id))
  const [error, setError] = useState<string | null>(null)

  function toggleParticipant(userId: string) {
    setParticipantUserIds((current) =>
      current.includes(userId) ? current.filter((id) => id !== userId) : [...current, userId],
    )
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    setError(null)

    if (participantUserIds.length === 0) {
      setError('Select at least one participant.')
      return
    }

    try {
      const expense = await api.logExpense(topicId, {
        description,
        amount: Number(amount),
        paidByUserId,
        expenseDate: new Date(expenseDate).toISOString(),
        participantUserIds,
      })
      onExpenseLogged?.(expense)
      setDescription('')
      setAmount('')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not log the expense.')
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      {error && <p role="alert">{error}</p>}

      <label htmlFor="expense-description">Description</label>
      <input
        id="expense-description"
        value={description}
        onChange={(event) => setDescription(event.target.value)}
        required
      />

      <label htmlFor="expense-amount">Amount</label>
      <input
        id="expense-amount"
        type="number"
        step="0.01"
        value={amount}
        onChange={(event) => setAmount(event.target.value)}
        required
      />

      <label htmlFor="expense-paid-by">Paid by</label>
      <select id="expense-paid-by" value={paidByUserId} onChange={(event) => setPaidByUserId(event.target.value)}>
        {members.map((member) => (
          <option key={member.id} value={member.id}>
            {member.displayName}
          </option>
        ))}
      </select>

      <label htmlFor="expense-date">Date</label>
      <input
        id="expense-date"
        type="date"
        value={expenseDate}
        onChange={(event) => setExpenseDate(event.target.value)}
      />

      <fieldset>
        <legend>Participants</legend>
        {members.map((member) => (
          <label key={member.id} htmlFor={`participant-${member.id}`}>
            <input
              id={`participant-${member.id}`}
              type="checkbox"
              checked={participantUserIds.includes(member.id)}
              onChange={() => toggleParticipant(member.id)}
            />
            {member.displayName}
          </label>
        ))}
      </fieldset>

      <button type="submit">Log expense</button>
    </form>
  )
}
