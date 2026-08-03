import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { Topic } from '../../api/types'

interface TopicListProps {
  onSelectTopic?: (topicId: string) => void
}

export function TopicList({ onSelectTopic }: TopicListProps) {
  const [topics, setTopics] = useState<Topic[] | null>(null)
  const [newTopicName, setNewTopicName] = useState('')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api.getMyTopics().then(setTopics).catch(() => setError('Could not load your topics.'))
  }, [])

  async function handleCreate(event: React.FormEvent) {
    event.preventDefault()
    if (!newTopicName.trim()) {
      return
    }

    try {
      const created = await api.createTopic({ name: newTopicName.trim() })
      setTopics((current) => [...(current ?? []), created])
      setNewTopicName('')
    } catch {
      setError('Could not create the topic.')
    }
  }

  if (error) {
    return <p role="alert">{error}</p>
  }

  if (topics === null) {
    return <p>Loading topics…</p>
  }

  return (
    <div>
      <h1>Your topics</h1>

      {topics.length === 0 ? (
        <p>No topics yet — create one to get started.</p>
      ) : (
        <ul>
          {topics.map((topic) => (
            <li key={topic.id}>
              <button type="button" onClick={() => onSelectTopic?.(topic.id)}>
                {topic.name}
              </button>
            </li>
          ))}
        </ul>
      )}

      <form onSubmit={handleCreate}>
        <label htmlFor="new-topic-name">Topic name</label>
        <input
          id="new-topic-name"
          value={newTopicName}
          onChange={(event) => setNewTopicName(event.target.value)}
        />
        <button type="submit">Create topic</button>
      </form>
    </div>
  )
}
