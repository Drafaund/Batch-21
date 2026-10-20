/** The game this browser is playing: enough to come back to it after a reload. */
export interface Session {
  code: string
  token: string
}

const KEY = 'battleship.session'

export function loadSession(): Session | null {
  try {
    const raw = window.localStorage.getItem(KEY)
    if (!raw) {
      return null
    }
    const value = JSON.parse(raw) as Partial<Session>
    return typeof value.code === 'string' && typeof value.token === 'string' ? { code: value.code, token: value.token } : null
  } catch {
    return null
  }
}

export function saveSession(session: Session): void {
  try {
    window.localStorage.setItem(KEY, JSON.stringify(session))
  } catch {
    // Storage can be blocked; the game still works, it just cannot be resumed after a reload.
  }
}

export function clearSession(): void {
  try {
    window.localStorage.removeItem(KEY)
  } catch {
    // Nothing to clear.
  }
}
