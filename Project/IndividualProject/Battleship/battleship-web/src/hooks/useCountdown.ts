import { useEffect, useState } from 'react'

const TICK_MS = 250

/**
 * Counts down from `seconds`, which the server measured when it built a view. It starts again whenever `resetKey`
 * changes, so a newer view restarts the count. Returns null when there is no clock.
 */
export function useCountdown(seconds: number | null | undefined, resetKey: unknown): number | null {
  const [state, setState] = useState({ seconds, resetKey, left: seconds ?? null })

  // A new view restarts the count at once. Setting state while rendering is how to react to changed props without an extra effect.
  if (state.seconds !== seconds || state.resetKey !== resetKey) {
    setState({ seconds, resetKey, left: seconds ?? null })
  }

  useEffect(() => {
    if (seconds === null || seconds === undefined) {
      return
    }
    const end = Date.now() + seconds * 1000
    const timer = setInterval(() => {
      setState((previous) => ({ ...previous, left: Math.max(0, Math.ceil((end - Date.now()) / 1000)) }))
    }, TICK_MS)
    return () => clearInterval(timer)
  }, [seconds, resetKey])

  return seconds === null || seconds === undefined ? null : state.left
}
