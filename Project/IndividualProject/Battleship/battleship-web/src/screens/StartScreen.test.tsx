import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { StartScreen } from './StartScreen'

describe('StartScreen', () => {
  it('starts a game against the random bot by default', async () => {
    const onStart = vi.fn()
    render(<StartScreen busy={false} onStart={onStart} />)

    await userEvent.type(screen.getByLabelText('Your name'), 'Ann')
    await userEvent.click(screen.getByRole('button', { name: 'Start game' }))

    expect(onStart).toHaveBeenCalledExactlyOnceWith('Ann', 'Random')
  })

  it('lets the player choose the Hunt-Target bot', async () => {
    const onStart = vi.fn()
    render(<StartScreen busy={false} onStart={onStart} />)

    await userEvent.type(screen.getByLabelText('Your name'), 'Ann')
    await userEvent.click(screen.getByRole('radio', { name: /Hunt-Target AI/ }))
    await userEvent.click(screen.getByRole('button', { name: 'Start game' }))

    expect(onStart).toHaveBeenCalledExactlyOnceWith('Ann', 'HuntTarget')
  })

  it('offers exactly two opponents with the random one selected', () => {
    render(<StartScreen busy={false} onStart={() => {}} />)

    const radios = screen.getAllByRole('radio')
    expect(radios).toHaveLength(2)
    expect(screen.getByRole('radio', { name: /Random AI/ })).toBeChecked()
    expect(screen.getByRole('radio', { name: /Hunt-Target AI/ })).not.toBeChecked()
  })

  it('trims the name', async () => {
    const onStart = vi.fn()
    render(<StartScreen busy={false} onStart={onStart} />)

    await userEvent.type(screen.getByLabelText('Your name'), '  Ann  ')
    await userEvent.click(screen.getByRole('button', { name: 'Start game' }))

    expect(onStart).toHaveBeenCalledWith('Ann', 'Random')
  })

  it('cannot start without a name', async () => {
    const onStart = vi.fn()
    render(<StartScreen busy={false} onStart={onStart} />)
    const button = screen.getByRole('button', { name: 'Start game' })

    expect(button).toBeDisabled()
    await userEvent.type(screen.getByLabelText('Your name'), '   ')
    expect(button).toBeDisabled()
    await userEvent.click(button)

    expect(onStart).not.toHaveBeenCalled()
  })

  it('starts by pressing Enter in the name field', async () => {
    const onStart = vi.fn()
    render(<StartScreen busy={false} onStart={onStart} />)

    await userEvent.type(screen.getByLabelText('Your name'), 'Ann{Enter}')

    expect(onStart).toHaveBeenCalledOnce()
  })

  it('limits the name to twenty characters', () => {
    render(<StartScreen busy={false} onStart={() => {}} />)

    expect(screen.getByLabelText('Your name')).toHaveAttribute('maxlength', '20')
  })

  it('is disabled while a game is being created', async () => {
    render(<StartScreen busy onStart={() => {}} />)
    await userEvent.type(screen.getByLabelText('Your name'), 'Ann')

    expect(screen.getByRole('button', { name: 'Starting…' })).toBeDisabled()
  })
})
