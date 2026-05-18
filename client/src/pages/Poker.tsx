import { useEffect, useState } from "react"
import { useHubConnection } from "../hooks/useHubConnection"
import { usePlayer } from "../context/PlayerContext"

const TABLE_ID = "table-1"

export default function Poker() {
  const { player } = usePlayer()
  const connection = useHubConnection("/hubs/poker")
  const [players, setPlayers] = useState<string[]>([])
  const [joined, setJoined] = useState(false)
  const [log, setLog] = useState<string[]>([])

  function addLog(msg: string) {
    setLog((prev) => [`[${new Date().toLocaleTimeString()}] ${msg}`, ...prev.slice(0, 19)])
  }

  useEffect(() => {
    if (!connection) return

    connection.on("PlayerJoined", (name: string) => {
      setPlayers((prev) => prev.includes(name) ? prev : [...prev, name])
      addLog(`${name} joined the table`)
    })
    connection.on("PlayerLeft", (name: string) => {
      setPlayers((prev) => prev.filter((n) => n !== name))
      addLog(`${name} left the table`)
    })

    return () => {
      connection.off("PlayerJoined")
      connection.off("PlayerLeft")
    }
  }, [connection])

  async function joinTable() {
    if (!connection) return
    try {
      await connection.invoke("JoinTable", TABLE_ID)
      setJoined(true)
    } catch (e) { addLog(`Error: ${e}`) }
  }

  async function leaveTable() {
    if (!connection) return
    try {
      await connection.invoke("LeaveTable", TABLE_ID)
      setJoined(false)
      setPlayers([])
    } catch (e) { addLog(`Error: ${e}`) }
  }

  return (
    <div>
      <h1>🃏 Poker</h1>
      <div className="card">
        <p><strong>Table:</strong> {TABLE_ID} &nbsp;|&nbsp; <strong>You:</strong> {player?.name}</p>
        <div style={{ marginTop: "0.75rem" }}>
          {!joined
            ? <button className="btn-primary" onClick={joinTable} disabled={!connection}>Join Table</button>
            : <button className="btn-secondary" onClick={leaveTable}>Leave Table</button>
          }
        </div>
      </div>

      {joined && (
        <div className="card">
          <h3>Players at table</h3>
          <div className="player-list">
            {players.length === 0
              ? <p style={{ opacity: 0.5 }}>Waiting for players…</p>
              : players.map((p) => <div key={p} className="player-item">{p}</div>)
            }
          </div>
        </div>
      )}

      <div className="card">
        <h3>Event Log</h3>
        {log.length === 0
          ? <p style={{ opacity: 0.5 }}>No events yet.</p>
          : log.map((l, i) => <div key={i} style={{ fontSize: "0.85rem", opacity: 0.8 }}>{l}</div>)
        }
      </div>

      {/* TODO: Add Poker game controls (bet, fold, raise) once engine is implemented */}
    </div>
  )
}
