import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { Expense, Topic } from '../../api/types'
import { Card, CardTitle } from '../../components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table'
import { Button } from '../../components/ui/button'
import { useIsDesktop } from '../../lib/useIsDesktop'

interface TopicDetailProps {
  topicId: string
  onOpenSubtopic?: (topicId: string) => void
}

export function TopicDetail({ topicId, onOpenSubtopic }: TopicDetailProps) {
  const [topic, setTopic] = useState<Topic | null>(null)
  const [subtopics, setSubtopics] = useState<Topic[]>([])
  const [expenses, setExpenses] = useState<Expense[]>([])
  const [newSubtopicName, setNewSubtopicName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const isDesktop = useIsDesktop()

  useEffect(() => {
    Promise.all([api.getTopic(topicId), api.getSubtopics(topicId), api.getExpenses(topicId)])
      .then(([topicResult, subtopicsResult, expensesResult]) => {
        setTopic(topicResult)
        setSubtopics(subtopicsResult)
        setExpenses(expensesResult)
      })
      .catch(() => setError('Could not load this topic.'))
  }, [topicId])

  async function handleAddSubtopic(event: React.FormEvent) {
    event.preventDefault()
    if (!newSubtopicName.trim()) {
      return
    }

    try {
      const created = await api.createSubtopic(topicId, { name: newSubtopicName.trim() })
      setSubtopics((current) => [...current, created])
      setNewSubtopicName('')
    } catch {
      setError('Could not create the subtopic.')
    }
  }

  if (error) {
    return <p role="alert">{error}</p>
  }

  if (topic === null) {
    return <p>Loading topic…</p>
  }

  return (
    <div>
      <h1 className="mb-1 text-2xl font-semibold">{topic.name}</h1>
      {topic.inviteCode && <p className="mb-4 text-sm text-neutral-500">Invite code: {topic.inviteCode}</p>}

      <section className="mb-6">
        <h2 className="mb-2 text-lg font-medium">Subtopics</h2>
        {subtopics.length === 0 ? (
          <p>No subtopics yet.</p>
        ) : (
          <ul className="flex flex-col gap-2">
            {subtopics.map((subtopic) => (
              <li key={subtopic.id}>
                <button
                  type="button"
                  className="min-h-11 text-left"
                  onClick={() => onOpenSubtopic?.(subtopic.id)}
                >
                  {subtopic.name}
                </button>
              </li>
            ))}
          </ul>
        )}

        <form onSubmit={handleAddSubtopic} className="mt-4 flex items-end gap-2">
          <div>
            <label htmlFor="new-subtopic-name" className="block text-sm font-medium">
              Subtopic name
            </label>
            <input
              id="new-subtopic-name"
              value={newSubtopicName}
              onChange={(event) => setNewSubtopicName(event.target.value)}
              className="min-h-11 rounded-md border border-neutral-300 px-3 dark:border-neutral-700 dark:bg-neutral-900"
            />
          </div>
          <Button type="submit">Add subtopic</Button>
        </form>
      </section>

      <section>
        <h2 className="mb-2 text-lg font-medium">Expenses</h2>
        {expenses.length === 0 ? (
          <p>No expenses logged yet.</p>
        ) : isDesktop ? (
          <Table data-testid="expense-table">
            <TableHeader>
              <TableRow>
                <TableHead>Description</TableHead>
                <TableHead>Amount</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {expenses.map((expense) => (
                <TableRow key={expense.id}>
                  <TableCell>{expense.description}</TableCell>
                  <TableCell>{expense.amount.toFixed(2)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        ) : (
          <div data-testid="expense-cards" className="grid grid-cols-1 gap-3">
            {expenses.map((expense) => (
              <Card key={expense.id}>
                <CardTitle>{expense.description}</CardTitle>
                <span>{expense.amount.toFixed(2)}</span>
              </Card>
            ))}
          </div>
        )}
      </section>
    </div>
  )
}
