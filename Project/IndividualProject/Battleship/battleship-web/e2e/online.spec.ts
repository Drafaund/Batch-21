import { expect, test, type Browser, type BrowserContext, type Page } from '@playwright/test'

const ROWS = 'ABCDEFGHIJ'

interface Player {
  context: BrowserContext
  page: Page
  name: string
}

async function newPlayer(browser: Browser, name: string): Promise<Player> {
  const context = await browser.newContext()
  return { context, page: await context.newPage(), name }
}

async function fillName(page: Page, name: string) {
  await page.goto('/')
  await page.getByLabel('Your name').fill(name)
}

async function createRoom(player: Player): Promise<string> {
  await fillName(player.page, player.name)
  await player.page.getByRole('button', { name: 'Create a room' }).click()
  await expect(player.page.getByRole('heading', { name: 'Waiting for an opponent' })).toBeVisible()
  const code = (await player.page.locator('.room-code').textContent())?.trim() ?? ''
  expect(code).toMatch(/^[A-Z0-9]{5}$/)
  return code
}

async function joinRoom(player: Player, code: string) {
  await fillName(player.page, player.name)
  await player.page.getByLabel('Room code').fill(code)
  await player.page.getByRole('button', { name: 'Join room' }).click()
}

async function startBattle(browser: Browser) {
  const ann = await newPlayer(browser, 'Ann')
  const bob = await newPlayer(browser, 'Bob')
  const code = await createRoom(ann)
  await joinRoom(bob, code)

  await expect(ann.page.getByRole('heading', { name: 'Place your ships' })).toBeVisible()
  await expect(bob.page.getByRole('heading', { name: 'Place your ships' })).toBeVisible()

  await ann.page.getByRole('button', { name: 'Place randomly' }).click()
  await expect(ann.page.getByText(/Waiting for Bob to place theirs/)).toBeVisible()
  await bob.page.getByRole('button', { name: 'Place randomly' }).click()

  await expect(ann.page.getByText(/Your turn/)).toBeVisible()
  await expect(bob.page.getByText('Waiting for Ann…')).toBeVisible()
  return { ann, bob, code }
}

async function fireUntilFinished(page: Page) {
  const playAgain = page.getByRole('button', { name: 'Play again' })

  for (let y = 0; y < 10; y++) {
    for (let x = 0; x < 10; x++) {
      const cell = page.getByRole('button', { name: `Target board ${ROWS[y]}-${x + 1}: not fired at yet` })
      await expect(playAgain.or(cell)).toBeVisible({ timeout: 30_000 })
      if (await playAgain.isVisible()) {
        return
      }
      await cell.click()
    }
  }
}

test.describe('two players online', () => {
  test.setTimeout(180_000)

  test('play a whole game, each in their own browser', async ({ browser }) => {
    const { ann, bob } = await startBattle(browser)

    await Promise.all([fireUntilFinished(ann.page), fireUntilFinished(bob.page)])

    const verdicts = await Promise.all(
      [ann, bob].map((player) => player.page.getByRole('status').filter({ hasText: /You win!|wins\./ }).textContent()),
    )
    expect(verdicts.filter((text) => text?.includes('You win!'))).toHaveLength(1)
    expect(verdicts.filter((text) => text?.includes('wins.'))).toHaveLength(1)
    await expect(ann.page.getByRole('button', { name: 'Play again' })).toBeVisible()
    await expect(bob.page.getByRole('button', { name: 'Play again' })).toBeVisible()
    await expect(ann.page.getByRole('img', { name: /Target board .*ship/i })).toHaveCount(0)
    await expect(bob.page.getByRole('img', { name: /Target board .*ship/i })).toHaveCount(0)

    await ann.context.close()
    await bob.context.close()
  })

  test('the shot of one player shows on the screen of the other at once', async ({ browser }) => {
    const { ann, bob } = await startBattle(browser)

    await ann.page.getByRole('button', { name: 'Target board A-1: not fired at yet' }).click()

    await expect(ann.page.getByText(/You fired at A-1:/)).toBeVisible()
    await expect(bob.page.getByText(/Ann fired at A-1:/)).toBeVisible()
    await expect(bob.page.getByText(/Your turn/)).toBeVisible()
    await expect(bob.page.getByRole('img', { name: /Your fleet A-1: (hit|miss)/ })).toBeVisible()
    await expect(ann.page.getByText('Waiting for Bob…')).toBeVisible()

    await ann.context.close()
    await bob.context.close()
  })

  test('a player who reloads the page comes back to the battle and keeps receiving shots', async ({ browser }) => {
    const { ann, bob } = await startBattle(browser)
    await ann.page.getByRole('button', { name: 'Target board A-1: not fired at yet' }).click()
    await expect(bob.page.getByText(/Ann fired at A-1:/)).toBeVisible()

    await bob.page.reload()
    await expect(bob.page.getByText(/Your turn/)).toBeVisible()
    await bob.page.getByRole('button', { name: 'Target board B-2: not fired at yet' }).click()

    await expect(bob.page.getByText(/You fired at B-2:/)).toBeVisible()
    await expect(ann.page.getByText(/Bob fired at B-2:/)).toBeVisible()
    await expect(ann.page.getByText(/Your turn/)).toBeVisible()

    await ann.context.close()
    await bob.context.close()
  })

  test('the opponent sees when a player drops out and when they are back', async ({ browser }) => {
    const { ann, bob } = await startBattle(browser)

    await bob.page.close()
    await expect(ann.page.getByText(/Bob lost the connection\. The game waits/)).toBeVisible()

    const returned = await bob.context.newPage()
    await returned.goto('/')
    await expect(returned.getByText('Waiting for Ann…')).toBeVisible()
    await expect(ann.page.getByText('Bob connected.').last()).toBeVisible()
    await expect(ann.page.getByText(/Bob lost the connection\. The game waits/)).toHaveCount(0)

    await ann.context.close()
    await bob.context.close()
  })

  test('refuses a code nobody opened and a room that is already full', async ({ browser }) => {
    const { ann, bob, code } = await startBattle(browser)
    const eve = await newPlayer(browser, 'Eve')

    await joinRoom(eve, 'ZZZZZ')
    await expect(eve.page.getByText('There is no game with this code.')).toBeVisible()

    await eve.page.getByLabel('Room code').fill(code)
    await eve.page.getByRole('button', { name: 'Join room' }).click()
    await expect(eve.page.getByText('This room is already full.')).toBeVisible()
    await expect(eve.page.getByRole('button', { name: 'Start game' })).toBeVisible()

    await ann.context.close()
    await bob.context.close()
    await eve.context.close()
  })

  test('a player can leave the lobby and start over', async ({ browser }) => {
    const ann = await newPlayer(browser, 'Ann')
    await createRoom(ann)

    await ann.page.getByRole('button', { name: 'Leave room' }).click()

    await expect(ann.page.getByRole('button', { name: 'Start game' })).toBeVisible()
    await ann.context.close()
  })
})
