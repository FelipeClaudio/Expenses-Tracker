import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { Expense, Topic } from '../../api/types'

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
      <h1>{topic.name}</h1>
      {topic.inviteCode && <p>Invite code: {topic.inviteCode}</p>}

      <section>
        <h2>Subtopics</h2>
        {subtopics.length === 0 ? (
          <p>No subtopics yet.</p>
        ) : (
          <ul>
            {subtopics.map((subtopic) => (
              <li key={subtopic.id}>
                <button type="button" onClick={() => onOpenSubtopic?.(subtopic.id)}>
                  {subtopic.name}
                </button>
              </li>
            ))}
          </ul>
        )}

        <form onSubmit={handleAddSubtopic}>
          <label htmlFor="new-subtopic-name">Subtopic name</label>
          <input
            id="new-subtopic-name"
            value={newSubtopicName}
            onChange={(event) => setNewSubtopicName(event.target.value)}
          />
          <button type="submit">Add subtopic</button>
        </form>
      </section>

      <section>
        <h2>Expenses</h2>
        {expenses.length === 0 ? (
          <p>No expenses logged yet.</p>
        ) : (
          <ul>
            {expenses.map((expense) => (
              <li key={expense.id}>
                <span>{expense.description}</span> <span>{expense.amount.toFixed(2)}</span>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}
