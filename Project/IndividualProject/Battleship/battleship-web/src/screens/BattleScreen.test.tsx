import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { GameView } from '../api/types'
import type { LogEntry } from '../hooks/useGame'
import { fleetView, withEnemyCell } from '../test/fixtures'
import { BattleScreen } from './BattleScreen'

interface Options {
  view?: GameView
  busy?: boolean
  log?: LogEntry[]
}

function setup({ view = fleetView(), busy = false, log = [] }: Options = {}) {
  const onFire = vi.fn()
  const onPlayAgain = vi.fn()
  const utils = render(
    <BattleScreen
      view={view}
      busy={busy}
      log={log}
      lastPlayerShot={null}
      lastBotShots={[]}
      onFire={onFire}
      onPlayAgain={onPlayAgain}
    />,
  )
  return { onFire, onPlayAgain, ...utils }
}

describe('BattleScreen', () => {
  it('tells the player to fire and lets any untouched target cell be clicked', async () => {
    const { onFire } = setup()

    expect(screen.getByRole('status')).toHaveTextContent('Your turn')
    await userEvent.click(screen.getByRole('button', { name: 'Target board B-3: not fired at yet' }))

    expect(onFire).toHaveBeenCalledExactlyOnceWith({ x: 2, y: 1 })
  })

  it('does not let cells that were fired at be clicked', () => {
    const view = withEnemyCell(withEnemyCell(fleetView(), 0, 0, 'Hit'), 1, 0, 'Miss')
    setup({ view })

    expect(screen.getAllByRole('button')).toHaveLength(98)
    expect(screen.getByRole('img', { name: 'Target board A-1: hit' })).toBeInTheDocument()
  })

  it('has no clickable cell when it is not the player turn', () => {
    setup({ view: fleetView({ yourTurn: false }) })

    expect(screen.queryAllByRole('button')).toHaveLength(0)
    expect(screen.getByRole('status')).toHaveTextContent('Waiting')
  })

  it('has no clickable cell while a shot is on its way', () => {
    setup({ busy: true })

    expect(screen.queryAllByRole('button')).toHaveLength(0)
    expect(screen.getByRole('status')).toHaveTextContent('Firing')
  })

  it('shows both fleets on their own board and the enemy count', () => {
    setup({ view: fleetView({ enemyShipsRemaining: 3 }) })

    expect(screen.getByRole('img', { name: 'Your fleet A-1: ship' })).toBeInTheDocument()
    expect(screen.getByText(/Enemy ships left/)).toHaveTextContent('Enemy ships left: 3')
    expect(screen.getByText(/Your ships afloat/)).toHaveTextContent('Your ships afloat: 5')
  })

  it('shows the message log', () => {
    setup({ log: [{ id: 1, kind: 'you', text: 'You fired at A-1: Miss.' }] })

    expect(screen.getByText('You fired at A-1: Miss.')).toBeInTheDocument()
  })

  it('marks the last shots on the right boards', () => {
    render(
      <BattleScreen
        view={withEnemyCell(fleetView(), 4, 4, 'Miss')}
        busy={false}
        log={[]}
        lastPlayerShot={{ x: 4, y: 4 }}
        lastBotShots={[{ x: 0, y: 0 }]}
        onFire={() => {}}
        onPlayAgain={() => {}}
      />,
    )

    expect(screen.getByRole('img', { name: 'Target board E-5: miss' })).toHaveClass('last-shot')
    expect(screen.getByRole('img', { name: 'Your fleet A-1: ship' })).toHaveClass('last-shot')
  })

  it('announces a win and offers a new game', async () => {
    const { onPlayAgain } = setup({ view: fleetView({ phase: 'Finished', yourTurn: false, winner: 'you', enemyShipsRemaining: 0 }) })

    expect(screen.getByRole('status')).toHaveTextContent('You win!')
    expect(screen.queryAllByRole('button', { name: /^Target board/ })).toHaveLength(0)
    await userEvent.click(screen.getByRole('button', { name: 'Play again' }))

    expect(onPlayAgain).toHaveBeenCalledOnce()
  })

  it('announces a defeat with the name of the winner', () => {
    setup({ view: fleetView({ phase: 'Finished', yourTurn: false, winner: 'opponent', opponent: { name: 'Bot Hunter', connected: true } }) })

    expect(screen.getByRole('status')).toHaveTextContent('Bot Hunter wins.')
  })
})
