import { useState } from 'react'
import { ApiError, api } from '../../api/client'
import type { Expense } from '../../api/types'
import { Button } from '../../components/ui/button'
import { Card } from '../../components/ui/card'
import { cn } from '../../lib/cn'

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

const fieldClassName =
  'min-h-11 w-full rounded-2xl border border-navy-100 px-4 focus:border-mint-400 focus:ring-2 focus:ring-mint-400 focus:outline-none dark:border-navy-700 dark:bg-navy-800'
const labelClassName = 'mb-1 block text-sm font-medium text-navy-400 dark:text-navy-300'

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
    <Card>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {error && (
          <p
            role="alert"
            className="rounded-2xl bg-rose-50 px-4 py-2 text-sm font-medium text-rose-700 dark:bg-rose-500/20 dark:text-rose-300"
          >
            {error}
          </p>
        )}

        <div>
          <label htmlFor="expense-description" className={labelClassName}>
            Description
          </label>
          <input
            id="expense-description"
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            required
            className={fieldClassName}
          />
        </div>

        <div>
          <label htmlFor="expense-amount" className={labelClassName}>
            Amount
          </label>
          <input
            id="expense-amount"
            type="number"
            step="0.01"
            value={amount}
            onChange={(event) => setAmount(event.target.value)}
            required
            className={fieldClassName}
          />
        </div>

        <div>
          <label htmlFor="expense-paid-by" className={labelClassName}>
            Paid by
          </label>
          <select
            id="expense-paid-by"
            value={paidByUserId}
            onChange={(event) => setPaidByUserId(event.target.value)}
            className={fieldClassName}
          >
            {members.map((member) => (
              <option key={member.id} value={member.id}>
                {member.displayName}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label htmlFor="expense-date" className={labelClassName}>
            Date
          </label>
          <input
            id="expense-date"
            type="date"
            value={expenseDate}
            onChange={(event) => setExpenseDate(event.target.value)}
            className={fieldClassName}
          />
        </div>

        <fieldset>
          <legend className={labelClassName}>Participants</legend>
          <div className="flex flex-wrap gap-2">
            {members.map((member) => (
              <label
                key={member.id}
                htmlFor={`participant-${member.id}`}
                className={cn(
                  'flex min-h-11 cursor-pointer items-center gap-2 rounded-full border border-navy-100 px-4 text-sm font-medium text-navy-700 transition-colors',
                  'has-[:checked]:border-mint-500 has-[:checked]:bg-mint-100 has-[:checked]:text-navy-900',
                  'has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-mint-400 has-[:focus-visible]:ring-offset-2',
                  'dark:border-navy-700 dark:text-navy-100 dark:has-[:checked]:border-mint-400 dark:has-[:checked]:bg-mint-500/20 dark:has-[:checked]:text-mint-300',
                  'dark:has-[:focus-visible]:ring-offset-navy-800',
                )}
              >
                <input
                  id={`participant-${member.id}`}
                  type="checkbox"
                  checked={participantUserIds.includes(member.id)}
                  onChange={() => toggleParticipant(member.id)}
                  className="sr-only"
                />
                {member.displayName}
              </label>
            ))}
          </div>
        </fieldset>

        <Button type="submit" className="self-start">
          Log expense
        </Button>
      </form>
    </Card>
  )
}
