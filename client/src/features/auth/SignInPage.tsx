import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router'
import { api } from '../../api/client'
import { useAuth } from '../../app/AuthContext'
import { Button } from '../../components/ui/button'
import { HeroCard } from '../../components/ui/card'

const fakeMode = import.meta.env.VITE_AUTH_FAKE_MODE === 'true'
const googleClientId = import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined

// Deterministic fake credential the API's FakeGoogleTokenValidator accepts
// when Auth:FakeMode is enabled server-side too (Playwright's E2E run only).
const fakeCredential = JSON.stringify({
  subject: 'e2e-test-user',
  email: 'e2e@example.com',
  name: 'E2E Test User',
  picture: null,
})

export function SignInPage() {
  const { refresh } = useAuth()
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)
  const googleButtonRef = useRef<HTMLDivElement>(null)

  async function completeSignIn(idToken: string) {
    setError(null)
    try {
      await api.signInGoogle(idToken)
      await refresh()
      navigate('/topics')
    } catch {
      setError('Could not sign in. Please try again.')
    }
  }

  useEffect(() => {
    if (fakeMode || !googleClientId || !googleButtonRef.current) {
      return
    }

    const script = document.createElement('script')
    script.src = 'https://accounts.google.com/gsi/client'
    script.async = true
    script.onload = () => {
      const google = (window as unknown as { google?: GoogleIdentityServices }).google
      if (!google || !googleButtonRef.current) {
        return
      }

      google.accounts.id.initialize({
        client_id: googleClientId,
        callback: (response) => completeSignIn(response.credential),
      })
      google.accounts.id.renderButton(googleButtonRef.current, { type: 'standard' })
    }
    document.body.appendChild(script)

    return () => {
      document.body.removeChild(script)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return (
    <div className="flex min-h-svh flex-col items-center justify-center gap-4 bg-navy-50 p-4 dark:bg-navy-950">
      <HeroCard className="flex w-full max-w-sm flex-col items-center gap-4 text-center">
        <span className="size-3 rounded-full bg-mint-500" />
        <h1 className="text-xl font-bold">Sign in</h1>
        {error && (
          <p role="alert" className="rounded-2xl bg-rose-500/20 px-4 py-2 text-sm font-medium text-rose-300">
            {error}
          </p>
        )}

        {fakeMode ? (
          <Button onClick={() => completeSignIn(fakeCredential)}>Continue as test user</Button>
        ) : googleClientId ? (
          <div ref={googleButtonRef} />
        ) : (
          <Button disabled>Sign in with Google (not configured)</Button>
        )}
      </HeroCard>
    </div>
  )
}

interface GoogleIdentityServices {
  accounts: {
    id: {
      initialize: (config: { client_id: string; callback: (response: { credential: string }) => void }) => void
      renderButton: (parent: HTMLElement, options: { type: string }) => void
    }
  }
}
