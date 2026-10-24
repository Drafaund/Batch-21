import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { EMOTE_COOLDOWN_MS, EMOTE_VISIBLE_MS, EMOTES } from '../domain/emotes'
import type { IncomingEmote } from '../hooks/useGame'
import { EmotePanel } from './EmotePanel'

function setup(incoming: IncomingEmote | null = null) {
  const onSend = vi.fn()
  const utils = render(<EmotePanel incoming={incoming} opponentName="Bob" onSend={onSend} />)
  const again = (next: IncomingEmote | null) => utils.rerender(<EmotePanel incoming={next} opponentName="Bob" onSend={onSend} />)
  return { onSend, again, ...utils }
}

describe('EmotePanel', () => {
  beforeEach(() => {
    window.localStorage.clear()
  })

  it('has one button per emote, named for a screen reader', () => {
    setup()

    expect(screen.getAllByRole('button')).toHaveLength(EMOTES.length)
    expect(screen.getByRole('button', { name: 'Thumbs up' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Applause' })).toBeInTheDocument()
  })

  it('sends the code of the emote that was clicked', async () => {
    const { onSend } = setup()

    await userEvent.click(screen.getByRole('button', { name: 'Bullseye' }))

    expect(onSend).toHaveBeenCalledExactlyOnceWith('target')
  })

  it('shows the emote of the opponent with their name', () => {
    setup({ id: 1, emote: 'laugh' })

    expect(screen.getByRole('status')).toHaveTextContent('Bob: Laughing')
  })

  it('shows nothing before an emote has come in', () => {
    setup()

    expect(screen.getByRole('status')).toBeEmptyDOMElement()
  })

  it('lets the player hide the emotes of the opponent and remembers it', async () => {
    const { unmount } = setup({ id: 1, emote: 'laugh' })

    await userEvent.click(screen.getByRole('checkbox', { name: 'Hide emotes from Bob' }))

    expect(screen.getByRole('status')).toBeEmptyDOMElement()
    expect(window.localStorage.getItem('battleship.hideEmotes')).toBe('true')

    unmount()
    setup({ id: 2, emote: 'wow' })
    expect(screen.getByRole('checkbox', { name: 'Hide emotes from Bob' })).toBeChecked()
    expect(screen.getByRole('status')).toBeEmptyDOMElement()
  })

  it('still lets a player who hides emotes send them', async () => {
    const { onSend } = setup()
    await userEvent.click(screen.getByRole('checkbox', { name: 'Hide emotes from Bob' }))

    await userEvent.click(screen.getByRole('button', { name: 'Wave' }))

    expect(onSend).toHaveBeenCalledExactlyOnceWith('wave')
  })

  describe('with a clock', () => {
    beforeEach(() => {
      vi.useFakeTimers({ shouldAdvanceTime: true })
    })

    afterEach(() => {
      vi.useRealTimers()
    })

    it('takes the emote off the screen after a few seconds', () => {
      setup({ id: 1, emote: 'laugh' })
      expect(screen.getByRole('status')).toHaveTextContent('Laughing')

      act(() => {
        vi.advanceTimersByTime(EMOTE_VISIBLE_MS + 50)
      })

      expect(screen.getByRole('status')).toBeEmptyDOMElement()
    })

    it('shows a new emote even when an earlier one has already gone', () => {
      const { again } = setup({ id: 1, emote: 'laugh' })
      act(() => {
        vi.advanceTimersByTime(EMOTE_VISIBLE_MS + 50)
      })

      again({ id: 2, emote: 'laugh' })

      expect(screen.getByRole('status')).toHaveTextContent('Laughing')
    })

    it('disables the buttons for the cooldown of the server, then enables them again', async () => {
      const { onSend } = setup()
      const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime })

      await user.click(screen.getByRole('button', { name: 'Applause' }))
      expect(onSend).toHaveBeenCalledTimes(1)
      screen.getAllByRole('button').forEach((button) => expect(button).toBeDisabled())

      act(() => {
        vi.advanceTimersByTime(EMOTE_COOLDOWN_MS + 50)
      })

      screen.getAllByRole('button').forEach((button) => expect(button).toBeEnabled())
    })
  })
})
