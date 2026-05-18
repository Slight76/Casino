# Casino

A multiplayer browser-based casino app featuring **Blackjack**, **Poker**, and **Roulette**. Players choose a display name on first visit; identity persists across games via `localStorage` and a server-issued token.

---

## Architecture

```
Casino/
├── client/          # React + Vite + TypeScript SPA
└── server/
    └── Casino.Server/  # .NET 8 Web API + SignalR
```

**Client → Server communication:**
- REST (`/api/players`) for registration
- SignalR WebSockets for real-time game events

**Identity flow:**
1. Player submits name → `POST /api/players` → receives `{ id, name, token }`
2. `PlayerContext` persists to `localStorage["casino.player"]`
3. SignalR connections pass `token` as `access_token` query param
4. Hubs resolve player from `IPlayerStore` singleton

---

## Prerequisites

| Tool | Version |
|------|---------|
| Node.js | 18+ |
| npm | 9+ |
| .NET SDK | 8.x |

---

## Run the Server

```powershell
cd server/Casino.Server
dotnet run
# Listens on https://localhost:7xxx / http://localhost:5xxx (see console)
```

## Run the Client

```powershell
cd client
npm run dev
# Vite dev server: http://localhost:5173
```

Open `http://localhost:5173` in your browser. Enter a display name to get started.

---

## Project Layout

```
Casino/
  .gitignore
  .editorconfig
  README.md
  Casino.sln
  client/
    src/
      context/PlayerContext.tsx   # Player identity (signIn / signOut)
      api/players.ts              # REST client for /api/players
      hooks/useHubConnection.ts   # SignalR HubConnection factory
      pages/
        NamePrompt.tsx            # Name entry screen
        Lobby.tsx                 # Game lobby
        Blackjack.tsx
        Poker.tsx
        Roulette.tsx
      router.tsx                  # Protected routes
  server/Casino.Server/
    Controllers/PlayersController.cs
    Models/Player.cs
    Services/{IPlayerStore,PlayerStore,ITableStore,TableStore}.cs
    Hubs/{LobbyHub,BlackjackHub,PokerHub,RouletteHub}.cs
    Games/{Blackjack,Poker,Roulette}/  # stub folders for future engines
```

---

## Roadmap

- [ ] **Blackjack engine** — deck, deal, hit, stand, bust detection
- [ ] **Poker engine** — Texas Hold'em hand evaluation, betting rounds
- [ ] **Roulette engine** — spin, bet types, payout calculation
- [ ] Persistent player balances (database)
- [ ] Authentication hardening (JWT bearer tokens)
- [ ] Spectator mode
