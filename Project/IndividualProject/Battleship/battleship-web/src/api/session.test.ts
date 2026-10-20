import { afterEach, describe, expect, it, vi } from 'vitest'
import { clearSession, loadSession, saveSession } from './session'

afterEach(() => {
  vi.restoreAllMocks()
})

describe('session', () => {
  it('returns null when nothing was saved', () => {
    expect(loadSession()).toBeNull()
  })

  it('saves and loads a session', () => {
    saveSession({ code: 'K7QF2', token: 'secret' })

    expect(loadSession()).toEqual({ code: 'K7QF2', token: 'secret' })
  })

  it('forgets a session', () => {
    saveSession({ code: 'K7QF2', token: 'secret' })

    clearSession()

    expect(loadSession()).toBeNull()
  })

  it('ignores stored text that is not a session', () => {
    window.localStorage.setItem('battleship.session', '{ not json')
    expect(loadSession()).toBeNull()

    window.localStorage.setItem('battleship.session', JSON.stringify({ code: 5 }))
    expect(loadSession()).toBeNull()
  })

  it('does not fail when the storage is blocked', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('blocked')
    })
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('blocked')
    })
    vi.spyOn(Storage.prototype, 'removeItem').mockImplementation(() => {
      throw new Error('blocked')
    })

    expect(loadSession()).toBeNull()
    expect(() => saveSession({ code: 'K7QF2', token: 'secret' })).not.toThrow()
    expect(() => clearSession()).not.toThrow()
  })
})
