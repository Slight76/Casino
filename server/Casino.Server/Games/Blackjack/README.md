# Blackjack Engine

ASP.NET Core 8 game engine for multi-seat blackjack delivered over SignalR (`/hubs/blackjack`).

## Rule defaults (Las Vegas Strip-ish)

Defined in `BlackjackRules` and **tweakable — confirm with product before launch**.

| Rule | Default |
|------|---------|
| `DeckCount` | 6 |
| `DealerHitsSoft17` | true |
| `BlackjackPayout` | 1.5 (3:2) |
| `AllowDouble` | true (any first two cards) |
| `AllowSplit` | true (one split per round in v1) |
| `TurnTimeoutSeconds` | 30 (auto-stand) |
| `StartingChips` | 1000 |
| `MinBet` / `MaxBet` | 10 / 500 |

## Flow

`Betting → Dealing → PlayerTurn → DealerTurn → Settling → Betting`

Engine methods on `BlackjackEngine` are pure with respect to a `BlackjackTable` (mutate state, return `List<RoundEvent>`). The hub wraps each call in a `lock(table)` and broadcasts `TableState` to the `blackjack:{tableId}` group.

## v1 limitations / TODOs

- Only one split per round (no resplit).
- Split aces may still be hit (no restriction).
- Split blackjack pays full 3:2 (industry typically pays 1:1).
- Insurance, surrender, side bets: not implemented.
- Penetration cut card at 75% — shoe rebuilds & reshuffles on the next draw past that threshold.
