/** The game this browser is playing: enough to come back to it after a reload. */
export interface Session {
  code: string
  token: string
  /** True in a room with another human: only then does the game listen for real-time events. */
  online?: boolean
}

const KEY = 'battleship.session'

export function loadSession(): Session | null {
  try {
    const raw = window.localStorage.getItem(KEY)
    if (!raw) {
      return null
    }
    const value = JSON.parse(raw) as Partial<Session>
    if (typeof value.code !== 'string' || typeof value.token !== 'string') {
      return null
    }
    return value.online === true ? { code: value.code, token: value.token, online: true } : { code: value.code, token: value.token }
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
