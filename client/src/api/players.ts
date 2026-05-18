import type { PlayerInfo } from "../context/PlayerContext"

const BASE = "http://localhost:5000"

export async function createPlayer(name: string): Promise<PlayerInfo> {
  const res = await fetch(`${BASE}/api/players`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ name }),
  })
  if (!res.ok) {
    const text = await res.text()
    throw new Error(`Failed to create player: ${text}`)
  }
  return res.json() as Promise<PlayerInfo>
}

export async function getPlayer(id: string): Promise<PlayerInfo> {
  const res = await fetch(`${BASE}/api/players/${id}`)
  if (!res.ok) throw new Error("Player not found")
  return res.json() as Promise<PlayerInfo>
}
