import { expect, test, type Page } from '@playwright/test'

const ROWS = 'ABCDEFGHIJ'

async function startGame(page: Page, name: string, opponent: 'Random AI' | 'Hunt-Target AI') {
  await page.goto('/')
  await page.getByLabel('Your name').fill(name)
  await page.getByRole('radio', { name: new RegExp(opponent) }).check()
  await page.getByRole('button', { name: 'Start game' }).click()
  await expect(page.getByRole('heading', { name: 'Place your ships' })).toBeVisible()
}

/** Fires at the cells row by row until the game is over. */
async function fireUntilFinished(page: Page) {
  const playAgain = page.getByRole('button', { name: 'Play again' })

  for (let y = 0; y < 10; y++) {
    for (let x = 0; x < 10; x++) {
      const cell = page.getByRole('button', { name: `Target board ${ROWS[y]}-${x + 1}: not fired at yet` })
      // Either the next cell can be clicked or the game ended.
      await expect(playAgain.or(cell)).toBeVisible()
      if (await playAgain.isVisible()) {
        return
      }
      await cell.click()
    }
  }
}

test.describe('a game against a bot', () => {
  for (const opponent of ['Random AI', 'Hunt-Target AI'] as const) {
    test(`is played to the end against the ${opponent}`, async ({ page }) => {
      await startGame(page, 'Eve', opponent)
      await page.getByRole('button', { name: 'Place randomly' }).click()
      await expect(page.getByText(/Your turn/)).toBeVisible()

      await fireUntilFinished(page)

      await expect(page.getByRole('status').filter({ hasText: /You win!|wins\./ })).toBeVisible()
      await expect(page.getByRole('button', { name: 'Play again' })).toBeVisible()
      // The enemy board only ever showed hits and misses.
      await expect(page.getByRole('img', { name: /Target board .*ship/i })).toHaveCount(0)
    })
  }

  test('lets the player place the ships by hand and fire the first shot', async ({ page }) => {
    await startGame(page, 'Eve', 'Hunt-Target AI')

    // A carrier at the right edge does not fit: the preview turns red and a click is refused.
    await page.getByRole('button', { name: 'Your fleet A-9: water' }).hover()
    await expect(page.locator('.preview-bad')).toHaveCount(2)
    await page.getByRole('button', { name: 'Your fleet A-9: water' }).click()
    await expect(page.getByText('does not fit there')).toBeVisible()

    // Five ships, one per row, all horizontal. The ship to place advances by itself.
    for (const row of 'ABCDE') {
      await page.getByRole('button', { name: `Your fleet ${row}-1: water` }).hover()
      await expect(page.locator('.preview-ok').first()).toBeVisible()
      await page.getByRole('button', { name: `Your fleet ${row}-1: water` }).click()
      // The cells are buttons while placing, and plain boxes once the fifth ship starts the battle.
      const placed = page.getByRole('button', { name: `Your fleet ${row}-1: ship` }).or(page.getByRole('img', { name: `Your fleet ${row}-1: ship` }))
      await expect(placed).toBeVisible()
    }

    await expect(page.getByText(/Your turn/)).toBeVisible()
    await page.getByRole('button', { name: 'Target board A-1: not fired at yet' }).click()

    await expect(page.getByText(/You fired at A-1:/)).toBeVisible()
    await expect(page.getByText(/Bot Hunter fired at/)).toBeVisible()
  })

  test('rotates a ship with the R key', async ({ page }) => {
    await startGame(page, 'Eve', 'Random AI')

    await page.keyboard.press('r')
    await expect(page.getByRole('button', { name: /Direction: Vertical/ })).toBeVisible()
    await page.getByRole('button', { name: 'Your fleet A-1: water' }).click()

    for (const row of 'ABCDE') {
      // While placing, every cell of the board is a button.
      await expect(page.getByRole('button', { name: `Your fleet ${row}-1: ship` })).toBeVisible()
    }
    await expect(page.getByRole('button', { name: 'Your fleet F-1: water' })).toBeVisible()
  })

  test('comes back to the same game after a reload', async ({ page }) => {
    await startGame(page, 'Eve', 'Random AI')
    await page.getByRole('button', { name: 'Place randomly' }).click()
    await page.getByRole('button', { name: 'Target board C-3: not fired at yet' }).click()
    await expect(page.getByText(/You fired at C-3:/)).toBeVisible()

    await page.reload()

    await expect(page.getByText(/Your turn/)).toBeVisible()
    await expect(page.getByRole('img', { name: /Target board C-3: (hit|miss)/ })).toBeVisible()
  })

  test('starts over with Play again', async ({ page }) => {
    await startGame(page, 'Eve', 'Random AI')
    await page.getByRole('button', { name: 'Place randomly' }).click()
    await fireUntilFinished(page)

    await page.getByRole('button', { name: 'Play again' }).click()

    await expect(page.getByLabel('Your name')).toBeVisible()
  })
})
