# battleship-web

The React front end of Battleship: play one game against a bot in the browser.
It talks to `Battleship.Api` (see `../WEB_DESIGN.md` for the endpoints and the shape of the data).

Vite, React 19, and TypeScript. Tests: Vitest with Testing Library, and Playwright for the browser.

## Run it

You need the .NET 8 SDK and Node.js 22.

```
# terminal 1, from the Battleship folder
dotnet run --project Battleship.Api

# terminal 2, from this folder
npm install
npm run dev
```

Open http://localhost:5173. The dev server sends every `/api` request to the API on http://localhost:5080.

## Scripts

| Command | What it does |
|---|---|
| `npm run dev` | Dev server with hot reload |
| `npm run build` | Type-check and build to `dist/` |
| `npm test` | Component and unit tests (Vitest) |
| `npm run e2e` | Browser tests (Playwright); starts the API and the dev server if they are not running |
| `npm run lint` | Lint with oxlint |

The first time you run the browser tests, install the browser with `npx playwright install chromium`.

## How the code is organised

```
src/
├── api/          the server contract: types, fetch client, saved session
├── domain/       rules that do not need React: coordinates, ship cells, placement preview
├── hooks/        useGame: the state of the game and the actions that change it
├── components/   boards, fleet status, message log, error banner
├── screens/      start, placement, and battle screens
└── App.tsx       picks the screen from the phase of the game
e2e/              Playwright tests against the real API
```

- **The server decides.** The browser sends "fire at D-4" and draws what it gets back. The placement preview only helps the player; the server still checks every move.
- **No enemy ships in the browser.** The enemy board type has no value for a ship, so it cannot hold one.
- **Resuming.** The game code and token are kept in `localStorage`, so a reload goes back to the same game. If the server no longer knows the game, the app returns to the start screen.
- **Cells.** Rows are letters A-J and columns are numbers 1-10, like `D-4` in the console game. Boards are indexed `cells[y][x]`.
