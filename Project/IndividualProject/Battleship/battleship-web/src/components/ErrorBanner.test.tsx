import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ErrorBanner } from './ErrorBanner'

describe('ErrorBanner', () => {
  it('announces the message as an alert', () => {
    render(<ErrorBanner message="It is not your turn." onDismiss={() => {}} />)

    expect(screen.getByRole('alert')).toHaveTextContent('It is not your turn.')
  })

  it('can be dismissed', async () => {
    const onDismiss = vi.fn()
    render(<ErrorBanner message="Oops" onDismiss={onDismiss} />)

    await userEvent.click(screen.getByRole('button', { name: 'Dismiss the message' }))

    expect(onDismiss).toHaveBeenCalledOnce()
  })
})
