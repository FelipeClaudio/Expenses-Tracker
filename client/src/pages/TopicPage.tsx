import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { AppShell } from '../app/AppShell'
import { useAuth } from '../app/AuthContext'
import { AddExpenseForm } from '../features/expenses/AddExpenseForm'
import { TopicDetail } from '../features/topics/TopicDetail'
import { buttonVariants } from '../components/ui/button'

export function TopicPage() {
  const { topicId } = useParams<{ topicId: string }>()
  const navigate = useNavigate()
  const { user } = useAuth()
  const [refreshKey, setRefreshKey] = useState(0)

  if (!topicId || !user) {
    return null
  }

  const members = [{ id: user.id, displayName: user.displayName }]

  return (
    <AppShell>
      <div className="flex flex-col gap-6">
        <TopicDetail key={refreshKey} topicId={topicId} onOpenSubtopic={(id) => navigate(`/topics/${id}`)} />

        <section>
          <h2 className="mb-2 text-lg font-semibold text-navy-900 dark:text-white">Add an expense</h2>
          <AddExpenseForm topicId={topicId} members={members} onExpenseLogged={() => setRefreshKey((key) => key + 1)} />
        </section>

        <Link to={`/topics/${topicId}/settle`} className={buttonVariants({ variant: 'outline', className: 'self-start' })}>
          Settle up
        </Link>
      </div>
    </AppShell>
  )
}
