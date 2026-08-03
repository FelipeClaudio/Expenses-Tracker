import { Navigate, Outlet } from 'react-router'
import { useAuth } from './AuthContext'

export function RequireAuth() {
  const { user, loading } = useAuth()

  if (loading) {
    return <p className="p-4">Loading…</p>
  }

  if (!user) {
    return <Navigate to="/signin" replace />
  }

  return <Outlet />
}
