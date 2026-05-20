const BASE = "http://localhost:5000"

export interface BlackjackTableSummary {
  tableId: string
  playerCount: number
  phase: string
}

export async function createBlackjackTable(): Promise<{ tableId: string }> {
  const res = await fetch(`${BASE}/api/tables/blackjack`, { method: "POST" })
  if (!res.ok) throw new Error(`Create table failed: ${await res.text()}`)
  return res.json() as Promise<{ tableId: string }>
}

export async function listBlackjackTables(): Promise<BlackjackTableSummary[]> {
  const res = await fetch(`${BASE}/api/tables/blackjack`)
  if (!res.ok) throw new Error(`List tables failed: ${await res.text()}`)
  return res.json() as Promise<BlackjackTableSummary[]>
}
