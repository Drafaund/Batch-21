import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useCountdown } from './useCountdown'

describe('useCountdown', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('has no clock when the server sends none', () => {
    const { result } = renderHook(() => useCountdown(null, 1))

    expect(result.current).toBeNull()
  })

  it('has no clock while the value is missing', () => {
    const { result } = renderHook(() => useCountdown(undefined, 1))

    expect(result.current).toBeNull()
  })

  it('starts at the seconds the server reported and counts down', () => {
    const { result } = renderHook(() => useCountdown(30, 1))
    expect(result.current).toBe(30)

    act(() => {
      vi.advanceTimersByTime(5000)
    })

    expect(result.current).toBe(25)
  })

  it('stops at zero', () => {
    const { result } = renderHook(() => useCountdown(2, 1))

    act(() => {
      vi.advanceTimersByTime(10_000)
    })

    expect(result.current).toBe(0)
  })

  it('starts again when a newer view arrives', () => {
    const { result, rerender } = renderHook(({ seconds, key }) => useCountdown(seconds, key), { initialProps: { seconds: 30, key: 1 } })
    act(() => {
      vi.advanceTimersByTime(10_000)
    })
    expect(result.current).toBe(20)

    rerender({ seconds: 30, key: 2 })

    expect(result.current).toBe(30)
    act(() => {
      vi.advanceTimersByTime(3000)
    })
    expect(result.current).toBe(27)
  })

  it('follows a different number of seconds in the same view', () => {
    const { result, rerender } = renderHook(({ seconds }) => useCountdown(seconds, 1), { initialProps: { seconds: 30 } })

    rerender({ seconds: 12 })

    expect(result.current).toBe(12)
  })

  it('drops the clock when the seconds go away, for example when the game ends', () => {
    const { result, rerender } = renderHook(({ seconds }) => useCountdown(seconds, 1), { initialProps: { seconds: 30 as number | null } })

    rerender({ seconds: null })

    expect(result.current).toBeNull()
  })
})
