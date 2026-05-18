import { createContext, useContext, useState, useEffect, useCallback, type ReactNode } from "react"
import { createPlayer } from "../api/players"

export interface PlayerInfo {
  id: string
  name: string
  token: string
}

interface PlayerContextValue {
  player: PlayerInfo | null
  signIn: (name: string) => Promise<void>
  signOut: () => void
}

const STORAGE_KEY = "casino.player"

const PlayerContext = createContext<PlayerContextValue | null>(null)

export function PlayerProvider({ children }: { children: ReactNode }) {
  const [player, setPlayer] = useState<PlayerInfo | null>(() => {
    try {
      const raw = localStorage.getItem(STORAGE_KEY)
      return raw ? (JSON.parse(raw) as PlayerInfo) : null
    } catch {
      return null
    }
  })

  // Keep localStorage in sync whenever player changes
  useEffect(() => {
    if (player) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(player))
    } else {
      localStorage.removeItem(STORAGE_KEY)
    }
  }, [player])

  const signIn = useCallback(async (name: string) => {
    const info = await createPlayer(name)
    setPlayer(info)
  }, [])

  const signOut = useCallback(() => {
    setPlayer(null)
    // Navigation happens in the component via protected route redirect
  }, [])

  return (
    <PlayerContext.Provider value={{ player, signIn, signOut }}>
      {children}
    </PlayerContext.Provider>
  )
}

export function usePlayer(): PlayerContextValue {
  const ctx = useContext(PlayerContext)
  if (!ctx) throw new Error("usePlayer must be used within PlayerProvider")
  return ctx
}
