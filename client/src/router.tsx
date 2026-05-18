import { createBrowserRouter, Navigate } from "react-router-dom"
import App from "./App"
import NamePrompt from "./pages/NamePrompt"
import Lobby from "./pages/Lobby"
import Blackjack from "./pages/Blackjack"
import Poker from "./pages/Poker"
import Roulette from "./pages/Roulette"
import { usePlayer } from "./context/PlayerContext"
import type { ReactNode } from "react"

function RequirePlayer({ children }: { children: ReactNode }) {
  const { player } = usePlayer()
  if (!player) return <Navigate to="/" replace />
  return <>{children}</>
}

export const router = createBrowserRouter([
  {
    path: "/",
    element: <App />,
    children: [
      { index: true, element: <NamePrompt /> },
      {
        path: "lobby",
        element: <RequirePlayer><Lobby /></RequirePlayer>,
      },
      {
        path: "blackjack",
        element: <RequirePlayer><Blackjack /></RequirePlayer>,
      },
      {
        path: "poker",
        element: <RequirePlayer><Poker /></RequirePlayer>,
      },
      {
        path: "roulette",
        element: <RequirePlayer><Roulette /></RequirePlayer>,
      },
    ],
  },
])
