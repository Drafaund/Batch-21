import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { StartScreen } from './StartScreen'

describe('StartScreen', () => {
  it('starts a game against the random bot by default', async () => {
    const onStart = vi.fn()
    render(<StartScreen busy={false} onStart={onStart} onJoin={() => {}} />)

    await userEvent.type(screen.getByLabelText('Your name'), 'Ann')
    await userEvent.click(screen.getByRole('button', { name: 'Start game' }))

    expect(onStart).toHaveBeenCalledExactlyOnceWith('Ann', 'Random')
  })

  it('lets the player choose the Hunt-Target bot', async () => {
    const onStart = vi.fn()
    render(<StartScreen busy={false} onStart={onStart} onJoin={() => {}} />)

    await userEvent.type(screen.getByLabelText('Your name'), 'Ann')
    await userEvent.click(screen.getByRole('radio', { name: /Hunt-Target AI/ }))
    await userEvent.click(screen.getByRole('button', { name: 'Start game' }))

    expect(onStart).toHaveBeenCalledExactlyOnceWith('Ann', 'HuntTarget')
  })

  it('offers exactly two opponents with the random one selected', () => {
    render(<StartScreen busy={false} onStart={() => {}} onJoin={() => {}} />)

    const radios = screen.getAllByRole('radio')
    expect(radios).toHaveLength(2)
    expect(screen.getByRole('radio', { name: /Random AI/ })).toBeChecked()
    expect(screen.getByRole('radio', { name: /Hunt-Target AI/ })).not.toBeChecked()
  })

  it('trims the name', async () => {
    const onStart = vi.fn()
    render(<StartScreen busy={false} onStart={onStart} onJoin={() => {}} />)

    await userEvent.type(screen.getByLabelText('Your name'), '  Ann  ')
    await userEvent.click(screen.getByRole('button', { name: 'Start game' }))

    expect(onStart).toHaveBeenCalledWith('Ann', 'Random')
  })

  it('cannot start without a name', async () => {
    const onStart = vi.fn()
    render(<StartScreen busy={false} onStart={onStart} onJoin={() => {}} />)
    const button = screen.getByRole('button', { name: 'Start game' })

    expect(button).toBeDisabled()
    await userEvent.type(screen.getByLabelText('Your name'), '   ')
    expect(button).toBeDisabled()
    await userEvent.click(button)

    expect(onStart).not.toHaveBeenCalled()
  })

  it('starts by pressing Enter in the name field', async () => {
    const onStart = vi.fn()
    render(<StartScreen busy={false} onStart={onStart} onJoin={() => {}} />)

    await userEvent.type(screen.getByLabelText('Your name'), 'Ann{Enter}')

    expect(onStart).toHaveBeenCalledOnce()
  })

  it('limits the name to twenty characters', () => {
    render(<StartScreen busy={false} onStart={() => {}} onJoin={() => {}} />)

    expect(screen.getByLabelText('Your name')).toHaveAttribute('maxlength', '20')
  })

  it('is disabled while a game is being created', async () => {
    render(<StartScreen busy onStart={() => {}} onJoin={() => {}} />)
    await userEvent.type(screen.getByLabelText('Your name'), 'Ann')

    expect(screen.getByRole('button', { name: 'Starting…' })).toBeDisabled()
  })
})

describe('StartScreen online room', () => {
  it('opens a room for a friend with the name typed', async () => {
    const onStart = vi.fn()
    render(<StartScreen busy={false} onStart={onStart} onJoin={() => {}} />)

    await userEvent.type(screen.getByLabelText('Your name'), 'Ann')
    await userEvent.click(screen.getByRole('button', { name: 'Create a room' }))

    expect(onStart).toHaveBeenCalledExactlyOnceWith('Ann', 'Human')
  })

  it('joins the room of a friend with the name and the code', async () => {
    const onJoin = vi.fn()
    render(<StartScreen busy={false} onStart={() => {}} onJoin={onJoin} />)

    await userEvent.type(screen.getByLabelText('Your name'), ' Bob ')
    await userEvent.type(screen.getByLabelText('Room code'), 'k7qf2')
    await userEvent.click(screen.getByRole('button', { name: 'Join room' }))

    expect(onJoin).toHaveBeenCalledExactlyOnceWith('Bob', 'k7qf2')
  })

  it('joins by pressing Enter in the code field and does not start a game against a bot', async () => {
    const onStart = vi.fn()
    const onJoin = vi.fn()
    render(<StartScreen busy={false} onStart={onStart} onJoin={onJoin} />)

    await userEvent.type(screen.getByLabelText('Your name'), 'Bob')
    await userEvent.type(screen.getByLabelText('Room code'), 'K7QF2{Enter}')

    expect(onJoin).toHaveBeenCalledExactlyOnceWith('Bob', 'K7QF2')
    expect(onStart).not.toHaveBeenCalled()
  })

  it('needs a name for both online buttons and a code to join', async () => {
    render(<StartScreen busy={false} onStart={() => {}} onJoin={() => {}} />)
    const create = screen.getByRole('button', { name: 'Create a room' })
    const join = screen.getByRole('button', { name: 'Join room' })
    expect(create).toBeDisabled()
    expect(join).toBeDisabled()

    await userEvent.type(screen.getByLabelText('Your name'), 'Bob')
    expect(create).toBeEnabled()
    expect(join).toBeDisabled()

    await userEvent.type(screen.getByLabelText('Room code'), 'K7QF2')
    expect(join).toBeEnabled()
  })

  it('limits the code to five characters', () => {
    render(<StartScreen busy={false} onStart={() => {}} onJoin={() => {}} />)

    expect(screen.getByLabelText('Room code')).toHaveAttribute('maxlength', '5')
  })
})
