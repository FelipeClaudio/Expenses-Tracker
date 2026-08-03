import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { Topic } from '../../api/types'
import { Card, CardTitle } from '../../components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table'
import { Button } from '../../components/ui/button'
import { useIsDesktop } from '../../lib/useIsDesktop'

interface TopicListProps {
  onSelectTopic?: (topicId: string) => void
}

export function TopicList({ onSelectTopic }: TopicListProps) {
  const [topics, setTopics] = useState<Topic[] | null>(null)
  const [newTopicName, setNewTopicName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const isDesktop = useIsDesktop()

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
      <h1 className="mb-4 text-2xl font-semibold">Your topics</h1>

      {topics.length === 0 ? (
        <p>No topics yet — create one to get started.</p>
      ) : isDesktop ? (
        <Table data-testid="topic-table">
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {topics.map((topic) => (
              <TableRow key={topic.id}>
                <TableCell>
                  <button type="button" onClick={() => onSelectTopic?.(topic.id)}>
                    {topic.name}
                  </button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      ) : (
        <div data-testid="topic-cards" className="grid grid-cols-1 gap-3">
          {topics.map((topic) => (
            <Card key={topic.id}>
              <CardTitle>
                <button type="button" className="min-h-11 w-full text-left" onClick={() => onSelectTopic?.(topic.id)}>
                  {topic.name}
                </button>
              </CardTitle>
            </Card>
          ))}
        </div>
      )}

      <form onSubmit={handleCreate} className="mt-4 flex items-end gap-2">
        <div>
          <label htmlFor="new-topic-name" className="block text-sm font-medium">
            Topic name
          </label>
          <input
            id="new-topic-name"
            value={newTopicName}
            onChange={(event) => setNewTopicName(event.target.value)}
            className="min-h-11 rounded-md border border-neutral-300 px-3 dark:border-neutral-700 dark:bg-neutral-900"
          />
        </div>
        <Button type="submit">Create topic</Button>
      </form>
    </div>
  )
}
