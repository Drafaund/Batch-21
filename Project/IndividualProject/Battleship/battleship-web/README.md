# battleship-web

The React front end of Battleship: play against a bot in the browser, or play online against a friend in a room with a code.
It talks to `Battleship.Api` (see `../WEB_DESIGN.md` for the endpoints and the shape of the data): commands go over REST, and a SignalR connection delivers the events of an online game.

Vite, React 19, and TypeScript. SignalR client: `@microsoft/signalr`. Tests: Vitest with Testing Library, and Playwright for the browser.

## Run it

You need the .NET 8 SDK and Node.js 22.

```
# terminal 1, from the Battleship folder
dotnet run --project Battleship.Api

# terminal 2, from this folder
npm install
npm run dev
```

Open http://localhost:5173. The dev server sends every `/api` request, and the SignalR connection under `/hubs`, to the API on http://localhost:5080.

To try an online game alone, open the page in two browser windows (the second one private, so it has its own saved session): create a room in one, and join it with the code in the other.

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
├── api/          the server contract: types, fetch client, SignalR connection, saved session
├── domain/       rules that do not need React: coordinates, ship cells, placement preview, emotes
├── hooks/        useGame: the state of the game and the actions that change it; useCountdown: the turn timer
├── components/   boards, fleet status, message log, emote panel, error banner
├── screens/      start, lobby, placement, and battle screens
└── App.tsx       picks the screen from the phase of the game
e2e/              Playwright tests against the real API
```

- **The server decides.** The browser sends "fire at D-4" and draws what it gets back. The placement preview only helps the player; the server still checks every move.
- **No enemy ships in the browser.** The enemy board type has no value for a ship, so it cannot hold one. A ship that has sunk is shown on the enemy board with a skull.
- **Screens.** *Start* (name, opponent, create or join a room), *Lobby* (waiting for the second player, with the room code), *Placement*, and *Battle*. The lobby only appears in online games.
- **Events replace state.** Every online event except an emote carries the full view of this player, and the view has a version number that grows with every change, so the browser ignores a view older than the one it has.
- **Reconnecting.** After a dropped connection the SignalR client keeps trying for two minutes, and after each reconnect the app fetches the current game from the server. The opponent sees when a player is away and when they are back.
- **Turn countdown.** In an online game the battle screen counts down the 30 seconds a player has to fire.
- **Resuming.** The game code and token are kept in `localStorage`, so a reload goes back to the same game. If the server no longer knows the game, the app returns to the start screen.
- **Cells.** Rows are letters A-J and columns are numbers 1-10, like `D-4` in the console game. Boards are indexed `cells[y][x]`.
