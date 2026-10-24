import { expect, test } from '@playwright/test'

// The server gives a player 30 seconds per turn, which is too long to wait for in a test. Start the API with a short
// limit and run only this file, for example:
//   Game__TurnTimeout=00:00:06 E2E_TURN_TIMEOUT=1 npx playwright test e2e/turn-timeout.spec.ts
// The other online tests need the normal limit, so this file skips itself unless E2E_TURN_TIMEOUT is set.
test.skip(!process.env.E2E_TURN_TIMEOUT, 'needs the API to run with a short Game__TurnTimeout')

test('a player who does nothing for the whole turn loses, and the other one wins', async ({ browser }) => {
  test.setTimeout(60_000)
  const annContext = await browser.newContext()
  const bobContext = await browser.newContext()
  const ann = await annContext.newPage()
  const bob = await bobContext.newPage()

  await ann.goto('/')
  await ann.getByLabel('Your name').fill('Ann')
  await ann.getByRole('button', { name: 'Create a room' }).click()
  const code = (await ann.locator('.room-code').textContent())?.trim() ?? ''

  await bob.goto('/')
  await bob.getByLabel('Your name').fill('Bob')
  await bob.getByLabel('Room code').fill(code)
  await bob.getByRole('button', { name: 'Join room' }).click()

  await ann.getByRole('button', { name: 'Place randomly' }).click()
  await bob.getByRole('button', { name: 'Place randomly' }).click()

  // Ann fires first and has a clock; Bob waits and sees the same clock.
  await expect(ann.getByText(/Your turn/)).toBeVisible()
  await expect(ann.getByLabel(/seconds left/)).toBeVisible()
  await expect(bob.getByLabel(/seconds left/)).toBeVisible()

  // Ann does nothing.
  await expect(ann.getByText('You ran out of time. You lose.')).toBeVisible({ timeout: 30_000 })
  await expect(bob.locator('strong', { hasText: 'Ann ran out of time. You win!' })).toBeVisible()
  await expect(ann.getByLabel(/seconds left/)).toHaveCount(0)

  await annContext.close()
  await bobContext.close()
})
