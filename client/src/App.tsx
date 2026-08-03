import { Navigate, Route, Routes } from 'react-router'
import { AuthProvider } from './app/AuthContext'
import { RequireAuth } from './app/RequireAuth'
import { SignInPage } from './features/auth/SignInPage'
import { TopicsPage } from './pages/TopicsPage'
import { TopicPage } from './pages/TopicPage'
import { SettlementPage } from './pages/SettlementPage'

function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/signin" element={<SignInPage />} />
        <Route element={<RequireAuth />}>
          <Route path="/" element={<Navigate to="/topics" replace />} />
          <Route path="/topics" element={<TopicsPage />} />
          <Route path="/topics/:topicId" element={<TopicPage />} />
          <Route path="/topics/:topicId/settle" element={<SettlementPage />} />
        </Route>
      </Routes>
    </AuthProvider>
  )
}

export default App
