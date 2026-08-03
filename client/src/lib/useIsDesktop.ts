import { useEffect, useState } from 'react'

const DESKTOP_QUERY = '(min-width: 768px)'

function currentlyDesktop() {
  return typeof window !== 'undefined' && typeof window.matchMedia === 'function' && window.matchMedia(DESKTOP_QUERY).matches
}

// Drives the card-vs-table list layout switch (NFR-7/8/9). A single
// conditional render (rather than rendering both and hiding one with CSS)
// so component tests querying by text don't hit duplicate matches; jsdom has
// no real viewport, so matchMedia is either absent or always non-matching
// there, which correctly defaults tests to the mobile/card rendering.
export function useIsDesktop() {
  const [isDesktop, setIsDesktop] = useState(currentlyDesktop)

  useEffect(() => {
    if (typeof window.matchMedia !== 'function') {
      return
    }

    const mediaQueryList = window.matchMedia(DESKTOP_QUERY)
    const handleChange = () => setIsDesktop(mediaQueryList.matches)
    mediaQueryList.addEventListener('change', handleChange)
    return () => mediaQueryList.removeEventListener('change', handleChange)
  }, [])

  return isDesktop
}
