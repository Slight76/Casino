import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { usePlayer } from "../context/PlayerContext"
import { useHubConnection } from "../hooks/useHubConnection"

interface TableInfo {
  game: string
  tableId: string
  playerCount: number
}

export default function Lobby() {
  const { player, signOut } = usePlayer()
  const connection = useHubConnection("/hubs/lobby")
  const [tables, setTables] = useState<TableInfo[]>([])

  useEffect(() => {
    if (!connection) return

    connection.invoke<TableInfo[]>("ListTables")
      .then(setTables)
      .catch((err) => console.error("[Lobby] ListTables error:", err))

    // Re-fetch if the server broadcasts an update
    connection.on("TablesUpdated", (updated: TableInfo[]) => setTables(updated))

    return () => { connection.off("TablesUpdated") }
  }, [connection])

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
          </div>
        ))}
      </div>
    </div>
  )
}
