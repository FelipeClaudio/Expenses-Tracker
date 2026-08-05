import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { Topic } from '../../api/types'
import { useAuth } from '../../app/AuthContext'
import { Card, HeroCard } from '../../components/ui/card'
import { IconBadge } from '../../components/ui/icon-badge'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table'
import { Button } from '../../components/ui/button'
import { useIsDesktop } from '../../lib/useIsDesktop'

interface TopicListProps {
  onSelectTopic?: (topicId: string) => void
}

export function TopicList({ onSelectTopic }: TopicListProps) {
  const { user } = useAuth()
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
      <HeroCard className="mb-6">
        <p className="text-sm text-white/70">Hi, {user?.displayName ?? 'there'}!</p>
        <h1 className="mt-1 text-2xl font-bold">Your topics</h1>
        <span className="mt-4 inline-flex items-center rounded-full bg-mint-500/20 px-3 py-1 text-xs font-semibold text-mint-300">
          {topics.length} {topics.length === 1 ? 'topic' : 'topics'}
        </span>
      </HeroCard>

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
                  <button
                    type="button"
                    className="min-h-11 font-medium hover:text-mint-600"
                    onClick={() => onSelectTopic?.(topic.id)}
                  >
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
            <Card key={topic.id} className="p-0">
              <button
                type="button"
                className="flex min-h-11 w-full items-center gap-3 rounded-2xl px-4 py-3 text-left"
                onClick={() => onSelectTopic?.(topic.id)}
              >
                <IconBadge variant="neutral" className="font-semibold" aria-hidden="true">
                  {topic.name.charAt(0).toUpperCase()}
                </IconBadge>
                {topic.name}
              </button>
            </Card>
          ))}
        </div>
      )}

      <form onSubmit={handleCreate} className="mt-6 flex items-end gap-2">
        <div>
          <label htmlFor="new-topic-name" className="mb-1 block text-sm font-medium text-navy-400 dark:text-navy-300">
            Topic name
          </label>
          <input
            id="new-topic-name"
            value={newTopicName}
            onChange={(event) => setNewTopicName(event.target.value)}
            className="min-h-11 rounded-2xl border border-navy-100 px-4 focus:border-mint-400 focus:ring-2 focus:ring-mint-400 focus:outline-none dark:border-navy-700 dark:bg-navy-800"
          />
        </div>
        <Button type="submit">Create topic</Button>
      </form>
    </div>
  )
}
