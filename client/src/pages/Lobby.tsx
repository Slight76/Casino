import { useEffect, useState } from "react"
import { Link, useNavigate } from "react-router-dom"
import { usePlayer } from "../context/PlayerContext"
import { useHubConnection } from "../hooks/useHubConnection"
import { createBlackjackTable, listBlackjackTables, type BlackjackTableSummary } from "../api/tables"

interface TableInfo {
  game: string
  tableId: string
  playerCount: number
}

export default function Lobby() {
  const { player, signOut } = usePlayer()
  const connection = useHubConnection("/hubs/lobby")
  const [tables, setTables] = useState<TableInfo[]>([])
  const [bjTables, setBjTables] = useState<BlackjackTableSummary[]>([])
  const [creating, setCreating] = useState(false)
  const navigate = useNavigate()

  useEffect(() => {
    if (!connection) return

    connection.invoke<TableInfo[]>("ListTables")
      .then(setTables)
      .catch((err) => console.error("[Lobby] ListTables error:", err))

    connection.on("TablesUpdated", (updated: TableInfo[]) => setTables(updated))

    return () => { connection.off("TablesUpdated") }
  }, [connection])

  useEffect(() => {
    let cancelled = false
    async function refresh() {
      try {
        const list = await listBlackjackTables()
        if (!cancelled) setBjTables(list)
      } catch (e) { console.error("[Lobby] listBlackjackTables:", e) }
    }
    refresh()
    const id = setInterval(refresh, 3000)
    return () => { cancelled = true; clearInterval(id) }
  }, [])

  async function newBlackjackTable() {
    setCreating(true)
    try {
      const { tableId } = await createBlackjackTable()
      navigate(`/blackjack/${tableId}`)
    } catch (e) {
      console.error("[Lobby] create blackjack table:", e)
    } finally {
      setCreating(false)
    }
  }

  return (
    <div>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <h1>🃏 Lobby</h1>
        <button className="btn-danger" onClick={signOut}>Sign Off</button>
      </div>
      <p style={{ marginBottom: "1.5rem", opacity: 0.75 }}>Welcome, {player?.name}!</p>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(3, 1fr)", gap: "1rem" }}>
        {(["blackjack", "poker", "roulette"] as const).map((game) => (
          <div key={game} className="card">
            <h2 style={{ textTransform: "capitalize" }}>{game}</h2>
            {game === "blackjack" ? (
              <>
                <button className="btn-primary"
                  style={{ width: "100%", marginBottom: "0.75rem" }}
                  onClick={newBlackjackTable}
                  disabled={creating}>
                  {creating ? "Creating…" : "New Blackjack Table"}
                </button>
                <ul className="table-list">
                  {bjTables.length === 0 && (
                    <li style={{ opacity: 0.5 }}>No active tables</li>
                  )}
                  {bjTables.map((t) => (
                    <li key={t.tableId} className="table-item">
                      <Link to={`/blackjack/${t.tableId}`}>Table {t.tableId}</Link>
                      <span>{t.playerCount} player{t.playerCount !== 1 ? "s" : ""} · {t.phase}</span>
                    </li>
                  ))}
                </ul>
              </>
            ) : (
              <>
                <Link to={`/${game}`}>
                  <button className="btn-primary" style={{ width: "100%", marginBottom: "0.75rem" }}>
                    Play
                  </button>
                </Link>
                <ul className="table-list">
                  {tables.filter((t) => t.game === game).map((t) => (
                    <li key={t.tableId} className="table-item">
                      <span>Table {t.tableId}</span>
                      <span>{t.playerCount} player{t.playerCount !== 1 ? "s" : ""}</span>
                    </li>
                  ))}
                  {tables.filter((t) => t.game === game).length === 0 && (
                    <li style={{ opacity: 0.5 }}>No active tables</li>
                  )}
                </ul>
              </>
            )}
          </div>
        ))}
      </div>
    </div>
  )
}
