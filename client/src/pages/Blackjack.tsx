import { useEffect, useMemo, useState } from "react"
import { useParams, Link } from "react-router-dom"
import { useHubConnection } from "../hooks/useHubConnection"
import { usePlayer } from "../context/PlayerContext"

interface CardDto {
  rank: string
  suit: string
  suitSymbol: string
  display: string
  isHidden: boolean
}

interface HandDto {
  cards: CardDto[]
  total: number
  isBlackjack: boolean
  isBust: boolean
  isStood: boolean
  isDone: boolean
  isSoft: boolean
  canSplit: boolean
  canDouble: boolean
  result: string | null
  bet: number
  winnings: number
}

interface SeatDto {
  playerId: string
  playerName: string
  chips: number
  hands: HandDto[]
  currentHandIndex: number
  bet: number
  hasActed: boolean
  isCurrentSeat: boolean
}

interface DealerHandDto { cards: CardDto[]; total: number }
interface RoundEventDto { type: string; playerId: string | null; playerName: string | null; message: string | null }
interface TableState {
  tableId: string
  phase: string
  dealerHand: DealerHandDto
  seats: SeatDto[]
  currentSeatIndex: number
  turnDeadline: string | null
  lastEvents: RoundEventDto[]
}

const RED_SUITS = new Set(["♥", "♦"])
const CHIP_VALUES = [10, 25, 50, 100]

function CardView({ card }: { card: CardDto }) {
  const red = RED_SUITS.has(card.suitSymbol)
  return (
    <span style={{
      display: "inline-block",
      minWidth: "2.6rem",
      padding: "0.4rem 0.6rem",
      margin: "0.15rem",
      borderRadius: "0.4rem",
      background: card.isHidden ? "#2c3e50" : "#fff",
      color: card.isHidden ? "#ecf0f1" : (red ? "#c0392b" : "#1c1c1c"),
      fontWeight: 700,
      fontFamily: "monospace",
      textAlign: "center",
      boxShadow: "0 1px 3px rgba(0,0,0,0.3)",
    }}>{card.display}</span>
  )
}

function HandView({ hand, label }: { hand: HandDto; label?: string }) {
  return (
    <div style={{ marginBottom: "0.4rem" }}>
      {label && <div style={{ fontSize: "0.8rem", opacity: 0.7 }}>{label}</div>}
      <div>{hand.cards.map((c, i) => <CardView key={i} card={c} />)}</div>
      <div style={{ fontSize: "0.85rem", marginTop: "0.2rem" }}>
        Total: <strong>{hand.total}</strong>
        {hand.isSoft && " (soft)"}
        {hand.isBlackjack && " — Blackjack!"}
        {hand.isBust && " — Bust"}
        {hand.isStood && !hand.isBust && !hand.isBlackjack && " — Stood"}
        {hand.bet > 0 && <span style={{ opacity: 0.75 }}> · Bet {hand.bet}</span>}
        {hand.result && <span style={{ marginLeft: "0.5rem" }}>→ {hand.result} ({hand.winnings >= 0 ? "+" : ""}{hand.winnings})</span>}
      </div>
    </div>
  )
}

export default function Blackjack() {
  const { tableId = "" } = useParams<{ tableId: string }>()
  const { player } = usePlayer()
  const connection = useHubConnection("/hubs/blackjack")
  const [state, setState] = useState<TableState | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [pendingBet, setPendingBet] = useState(0)
  const [now, setNow] = useState(Date.now())

  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 500)
    return () => clearInterval(id)
  }, [])

  useEffect(() => {
    if (!connection || !tableId) return
    connection.on("TableState", (s: TableState) => setState(s))
    connection.invoke("JoinTable", tableId).catch((e) => setError(String(e)))
    return () => {
      connection.off("TableState")
      connection.invoke("LeaveTable", tableId).catch(() => {})
    }
  }, [connection, tableId])

  const mySeat = useMemo(
    () => state?.seats.find((s) => s.playerId === player?.id) ?? null,
    [state, player?.id]
  )
  const isMyTurn = state?.phase === "PlayerTurn" && mySeat?.isCurrentSeat === true
  const myCurrentHand = mySeat?.hands[mySeat.currentHandIndex] ?? null

  const secondsLeft = useMemo(() => {
    if (!state?.turnDeadline) return null
    const ms = new Date(state.turnDeadline).getTime() - now
    return Math.max(0, Math.ceil(ms / 1000))
  }, [state?.turnDeadline, now])

  async function invoke(method: string, ...args: unknown[]) {
    if (!connection) return
    setError(null)
    try { await connection.invoke(method, tableId, ...args) }
    catch (e) { setError(String(e)) }
  }

  return (
    <div>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <h1>♠️ Blackjack — Table {tableId}</h1>
        <Link to="/lobby"><button className="btn-secondary">Back to Lobby</button></Link>
      </div>

      {error && (
        <div className="card" style={{ background: "#3c1f1f", color: "#ffcccc" }}>
          {error}
        </div>
      )}

      {!state ? (
        <div className="card"><p>Connecting…</p></div>
      ) : (
        <>
          {/* Dealer */}
          <div className="card" style={{ background: "#1e3a2e" }}>
            <h2 style={{ marginTop: 0 }}>Dealer</h2>
            <div>{state.dealerHand.cards.map((c, i) => <CardView key={i} card={c} />)}</div>
            <div style={{ marginTop: "0.3rem" }}>Total: <strong>{state.dealerHand.total}{state.dealerHand.cards.some(c => c.isHidden) ? "+?" : ""}</strong></div>
            <div style={{ marginTop: "0.5rem", fontSize: "0.85rem", opacity: 0.8 }}>
              Phase: <strong>{state.phase}</strong>
              {isMyTurn && secondsLeft !== null && (
                <span style={{ marginLeft: "1rem" }}>⏱ {secondsLeft}s</span>
              )}
            </div>
          </div>

          {/* Seats */}
          <div className="card">
            <h3>Seats</h3>
            {state.seats.length === 0 && <p style={{ opacity: 0.5 }}>No seats yet.</p>}
            {state.seats.map((seat) => {
              const isMe = seat.playerId === player?.id
              return (
                <div key={seat.playerId} style={{
                  padding: "0.6rem",
                  marginBottom: "0.4rem",
                  borderRadius: "0.4rem",
                  background: seat.isCurrentSeat ? "#2a4a7a" : "#222",
                  border: isMe ? "1px solid #6ab04c" : "1px solid transparent",
                }}>
                  <div style={{ display: "flex", justifyContent: "space-between" }}>
                    <strong>{seat.playerName}{isMe && " (you)"}</strong>
                    <span>Chips: {seat.chips} · Bet: {seat.bet}{seat.isCurrentSeat && " ← turn"}</span>
                  </div>
                  {seat.hands.map((h, i) => (
                    <HandView
                      key={i}
                      hand={h}
                      label={seat.hands.length > 1 ? `Hand ${i + 1}${i === seat.currentHandIndex && seat.isCurrentSeat ? " ← active" : ""}` : undefined}
                    />
                  ))}
                </div>
              )
            })}
          </div>

          {/* Betting bar */}
          {state.phase === "Betting" && mySeat && (
            <div className="card">
              <h3>Place your bet</h3>
              <div style={{ marginBottom: "0.5rem" }}>
                Pending bet: <strong>{pendingBet}</strong>
                <button className="btn-secondary" style={{ marginLeft: "0.5rem" }} onClick={() => setPendingBet(0)}>Clear</button>
              </div>
              <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
                {CHIP_VALUES.map((v) => (
                  <button key={v} className="btn-secondary"
                    onClick={() => setPendingBet((p) => p + v)}
                    disabled={pendingBet + v > mySeat.chips}>
                    +{v}
                  </button>
                ))}
                <button className="btn-primary"
                  disabled={pendingBet <= 0}
                  onClick={async () => { await invoke("PlaceBet", pendingBet); setPendingBet(0) }}>
                  Place Bet ({pendingBet})
                </button>
              </div>
              {mySeat.bet > 0 && <p style={{ marginTop: "0.5rem", opacity: 0.8 }}>Current bet: {mySeat.bet} (waiting for others)</p>}
            </div>
          )}

          {/* Action bar */}
          {isMyTurn && myCurrentHand && (
            <div className="card">
              <h3>Your action</h3>
              <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
                <button className="btn-primary" onClick={() => invoke("Hit")}>Hit</button>
                <button className="btn-primary" onClick={() => invoke("Stand")}>Stand</button>
                {myCurrentHand.canDouble && mySeat && mySeat.chips >= myCurrentHand.bet && (
                  <button className="btn-secondary" onClick={() => invoke("Double")}>Double</button>
                )}
                {myCurrentHand.canSplit && mySeat?.hands.length === 1 && mySeat.chips >= myCurrentHand.bet && (
                  <button className="btn-secondary" onClick={() => invoke("Split")}>Split</button>
                )}
              </div>
            </div>
          )}

          {/* Result banner */}
          {state.phase === "Settling" && mySeat && (
            <div className="card" style={{ background: "#2c3e50" }}>
              <h3>Round result</h3>
              {mySeat.hands.map((h, i) => (
                <div key={i}>
                  Hand {i + 1}: {h.result ?? "—"} ({h.winnings >= 0 ? "+" : ""}{h.winnings})
                </div>
              ))}
              <p style={{ opacity: 0.7, fontSize: "0.85rem", marginTop: "0.5rem" }}>Next round in a few seconds…</p>
            </div>
          )}

          {/* Event log */}
          <div className="card">
            <h3>Last events</h3>
            {state.lastEvents.length === 0
              ? <p style={{ opacity: 0.5 }}>—</p>
              : state.lastEvents.map((e, i) => (
                <div key={i} style={{ fontSize: "0.85rem", opacity: 0.85 }}>
                  {e.type}{e.playerName ? ` · ${e.playerName}` : ""}{e.message ? `: ${e.message}` : ""}
                </div>
              ))
            }
          </div>
        </>
      )}
    </div>
  )
}
