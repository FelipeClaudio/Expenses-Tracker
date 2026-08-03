import { useNavigate } from 'react-router'
import { AppShell } from '../app/AppShell'
import { TopicList } from '../features/topics/TopicList'

export function TopicsPage() {
  const navigate = useNavigate()

  return (
    <AppShell>
      <TopicList onSelectTopic={(topicId) => navigate(`/topics/${topicId}`)} />
    </AppShell>
  )
}
