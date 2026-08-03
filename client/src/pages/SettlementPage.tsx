import { useParams } from 'react-router'
import { AppShell } from '../app/AppShell'
import { useAuth } from '../app/AuthContext'
import { SettlementView } from '../features/settlements/SettlementView'

export function SettlementPage() {
  const { topicId } = useParams<{ topicId: string }>()
  const { user } = useAuth()

  if (!topicId || !user) {
    return null
  }

  const members = [{ id: user.id, displayName: user.displayName }]

  return (
    <AppShell>
      <SettlementView topicId={topicId} members={members} />
    </AppShell>
  )
}
