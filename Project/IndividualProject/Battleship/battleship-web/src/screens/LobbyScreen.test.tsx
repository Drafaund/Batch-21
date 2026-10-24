import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { makeView } from '../test/fixtures'
import { LobbyScreen } from './LobbyScreen'

describe('LobbyScreen', () => {
  it('shows the code to share and says it waits for an opponent', () => {
    render(<LobbyScreen view={makeView({ phase: 'Lobby', code: 'K7QF2', opponent: null })} onLeave={() => {}} />)

    expect(screen.getByRole('heading', { name: 'Waiting for an opponent' })).toBeInTheDocument()
    expect(screen.getByText('K7QF2')).toBeInTheDocument()
  })

  it('lets the player leave the room', async () => {
    const onLeave = vi.fn()
    render(<LobbyScreen view={makeView({ phase: 'Lobby', opponent: null })} onLeave={onLeave} />)

    await userEvent.click(screen.getByRole('button', { name: 'Leave room' }))

    expect(onLeave).toHaveBeenCalledOnce()
  })
})
