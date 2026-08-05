import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { Expense, Topic } from '../../api/types'
import { Card, CardTitle } from '../../components/ui/card'
import { IconBadge } from '../../components/ui/icon-badge'
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
      <div className="mb-6 flex flex-wrap items-center gap-3">
        <h1 className="text-2xl font-bold text-navy-900 dark:text-white">{topic.name}</h1>
        {topic.inviteCode && (
          <span className="rounded-full bg-mint-100 px-3 py-1 text-xs font-semibold text-mint-700 dark:bg-mint-500/20 dark:text-mint-300">
            Invite code: {topic.inviteCode}
          </span>
        )}
      </div>

      <section className="mb-6">
        <h2 className="mb-2 text-lg font-semibold text-navy-900 dark:text-white">Subtopics</h2>
        {subtopics.length === 0 ? (
          <p className="text-navy-400 dark:text-navy-300">No subtopics yet.</p>
        ) : (
          <Card className="p-2">
            <ul className="flex flex-col">
              {subtopics.map((subtopic) => (
                <li key={subtopic.id}>
                  <button
                    type="button"
                    className="flex min-h-11 w-full items-center gap-3 rounded-2xl px-2 py-2 text-left"
                    onClick={() => onOpenSubtopic?.(subtopic.id)}
                  >
                    <IconBadge variant="neutral" className="text-sm font-semibold" aria-hidden="true">
                      {subtopic.name.charAt(0).toUpperCase()}
                    </IconBadge>
                    {subtopic.name}
                  </button>
                </li>
              ))}
            </ul>
          </Card>
        )}

        <form onSubmit={handleAddSubtopic} className="mt-4 flex items-end gap-2">
          <div>
            <label htmlFor="new-subtopic-name" className="mb-1 block text-sm font-medium text-navy-400 dark:text-navy-300">
              Subtopic name
            </label>
            <input
              id="new-subtopic-name"
              value={newSubtopicName}
              onChange={(event) => setNewSubtopicName(event.target.value)}
              className="min-h-11 rounded-2xl border border-navy-100 px-4 focus:border-mint-400 focus:ring-2 focus:ring-mint-400 focus:outline-none dark:border-navy-700 dark:bg-navy-800"
            />
          </div>
          <Button type="submit">Add subtopic</Button>
        </form>
      </section>

      <section>
        <h2 className="mb-2 text-lg font-semibold text-navy-900 dark:text-white">Expenses</h2>
        {expenses.length === 0 ? (
          <p className="text-navy-400 dark:text-navy-300">No expenses logged yet.</p>
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
              <Card key={expense.id} className="flex items-center gap-3">
                <IconBadge variant="neutral" aria-hidden="true">
                  {expense.description.charAt(0).toUpperCase()}
                </IconBadge>
                <CardTitle className="flex-1">{expense.description}</CardTitle>
                <span className="font-semibold text-navy-900 dark:text-white">{expense.amount.toFixed(2)}</span>
              </Card>
            ))}
          </div>
        )}
      </section>
    </div>
  )
}
